using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SugarMeter : MonoBehaviour
{
    public static SugarMeter Instance;
    
    [SerializeField] private bool simulationPaused = false;

    // ===== Persist basic =====
    private static bool  s_hasSaved;
    private static float s_savedSugar;
    private static int   s_savedHearts;

    [Tooltip("Initial sugar level")] public float startSugar = 100f;

    [Tooltip("Baseline decrease per GAME HOUR (from long-acting insulin)")]
    public float sugarDecreaseRate = 1f;

    public int   maxHearts = 3;
    private int  currentHearts;

    public float minSugar = 70f, maxSugar = 180f;
    public float minSugarClamp = 0f, maxSugarClamp = 250f;
    

    private float timeOutsideSafeRange = 0f, timeInsideSafeRange = 0f;

    public Image[] heartImages;
    public Text    sugarText;
    private static bool s_skipRestoreOnce;
    private float sugarLevel;

    [SerializeField] private bool heartsPaused = false;

    // ===== Superposition Model =====
    private struct Effect
    {
        public float  ratePerGameMin;
        public double startAtGameMin;
        public double endAtGameMin;
        public int    id;
    }
    private readonly List<Effect> _effects = new();
    private int _nextEffectId = 1;

// ===== Transients (momentary deltas outside trends) =====
    private struct Transient
    {
        public float ratePerGameMin;
        public float remainingGameMin; 
    }
    private readonly List<Transient> _transients = new();

    
    private float BaselineRatePerGameMin => -(sugarDecreaseRate / 60f);
    
    private double _absGameMinutes = 0;
    
    private int _currentTrendSign = 0;
    private double _currentTrendEndsAt = -1;

    // ===== Events =====
    public event Action<bool, float> TimedChangeStarted;   // isIncrease, durationSec
    public event Action<bool, float, float> TimedChangeScheduled; // isIncrease, delaySec, durationSec
    public event Action<bool> TimedChangeEnded;     // isIncrease

    // ===== Legacy =====
    [Serializable] private struct TrendsState { public bool has; }
    private static TrendsState s_trends;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        bool launchedStandalone = (typeof(AppFlow) != null && AppFlow.Mode == LaunchMode.Standalone);
        bool shouldRestore = s_hasSaved && !s_skipRestoreOnce && !launchedStandalone;

        if (shouldRestore)
        {
            sugarLevel    = Mathf.Clamp(s_savedSugar,  minSugarClamp, maxSugarClamp);
            currentHearts = Mathf.Clamp(s_savedHearts, 0, maxHearts);
        }
        else
        {
            sugarLevel    = startSugar;
            currentHearts = 0;
            _effects.Clear();
            _currentTrendSign = 0;
            _currentTrendEndsAt = -1;
            s_trends = new TrendsState { has = false };
            s_hasSaved = false;
            _absGameMinutes = 0;
        }

        s_skipRestoreOnce = false;

        UpdateSugarUI();
        UpdateHeartsUI();
    }
    
    public void SetSimulationPaused(bool paused)
    {
        simulationPaused = paused;
        
        SetHeartsPaused(paused, resetProgress: paused);
    }

    void OnDisable()
    {
        s_savedSugar  = sugarLevel;
        s_savedHearts = currentHearts;
        s_hasSaved    = true;
        s_trends = new TrendsState { has = true };
    }

    void Update()
    {
        if (simulationPaused) 
        {
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        
        float gsrs = Timer.Instance ? Timer.Instance.GameSecondsPerRealSecond : 30f;
        float gameMinutesThisFrame = dt * (gsrs / 60f);
        _absGameMinutes += gameMinutesThisFrame;
        double nowGM = _absGameMinutes;
        
        if (_transients.Count > 0)
        {
            float gmStep = gameMinutesThisFrame;
            for (int i = _transients.Count - 1; i >= 0; i--)
            {
                var tr = _transients[i];

                if (tr.remainingGameMin <= 0.0001f)
                {
                    sugarLevel = Mathf.Clamp(sugarLevel + tr.ratePerGameMin * 0f /* no time */ + tr.ratePerGameMin * tr.remainingGameMin, minSugarClamp, maxSugarClamp);
                    _transients.RemoveAt(i);
                    continue;
                }

                float step = Mathf.Min((float)tr.remainingGameMin, gmStep);
                float delta = tr.ratePerGameMin * step;
                sugarLevel = Mathf.Clamp(sugarLevel + delta, minSugarClamp, maxSugarClamp);
                tr.remainingGameMin -= step;

                if (tr.remainingGameMin <= 0.0001f) _transients.RemoveAt(i);
                else                                _transients[i] = tr;
            }
        }
        
        for (int i = _effects.Count - 1; i >= 0; i--)
            if (nowGM >= _effects[i].endAtGameMin) _effects.RemoveAt(i);
        
        float effectsRate = CalculateEffectsRate(nowGM);
        
        int sign = GetSign(effectsRate);
        if (sign != 0)
        {
            double flipAt = ComputeNextSignFlipTime(nowGM);
            TrimOppositeEffectsUntil(sign, nowGM, flipAt);
            effectsRate = CalculateEffectsRate(nowGM);
            _currentTrendEndsAt = flipAt;
        }
        else
        {
            _currentTrendEndsAt = -1;
        }


        float totalRate = (sign == 0) ? BaselineRatePerGameMin : effectsRate;
        
        sugarLevel = Mathf.Clamp(
            sugarLevel + totalRate * gameMinutesThisFrame,
            minSugarClamp, maxSugarClamp
        );
        
        UpdateTrendArrows_AfterTrim(sign, nowGM);

        UpdateSugarUI();
        //UpdateHeartsLogic();
    }

    public int GetCurrentTrendSign()
    {
        double gm = _absGameMinutes;
        float totalRate = 0f;
        
        foreach (var effect in _effects)
        {
            if (gm >= effect.startAtGameMin && gm < effect.endAtGameMin)
                totalRate += effect.ratePerGameMin;
        }

        if (Mathf.Abs(totalRate) < 1e-6f) return 0;
        return totalRate > 0 ? 1 : -1;
    }

    
    private float EffectsRateAt(double gm)
    {
        float sum = 0f;
        for (int i = 0; i < _effects.Count; i++)
        {
            var e = _effects[i];
            if (gm >= e.startAtGameMin && gm < e.endAtGameMin)
                sum += e.ratePerGameMin;
        }
        return sum;
    }
    
    private double ComputeNextSignFlipTime(double nowGM)
    {
        int signNow = GetSign(EffectsRateAt(nowGM));
        // אוספים נקודות מבנה קדימה
        var events = new List<double>();
        foreach (var e in _effects)
        {
            if (e.startAtGameMin > nowGM) events.Add(e.startAtGameMin);
            if (e.endAtGameMin   > nowGM) events.Add(e.endAtGameMin);
        }
        events.Sort();

        for (int i = 0; i < events.Count; i++)
        {
            double t = events[i];
            int signAfter = GetSign(EffectsRateAt(t + 1e-5));
            if (signAfter != signNow) return t;
        }
        return nowGM + 24 * 60;
    }
    
    private void TrimOppositeEffectsUntil(int currentSign, double nowGM, double trimAt)
    {
        if (trimAt <= nowGM) return;
        for (int i = 0; i < _effects.Count; i++)
        {
            var e = _effects[i];

            bool activeNow = (nowGM >= e.startAtGameMin && nowGM < e.endAtGameMin);
            if (!activeNow) continue;

            bool opposite = (currentSign > 0 && e.ratePerGameMin < 0f) ||
                            (currentSign < 0 && e.ratePerGameMin > 0f);
            if (!opposite) continue;
            
            if (e.startAtGameMin < trimAt && e.endAtGameMin > trimAt)
            {
                e.endAtGameMin = trimAt;
                _effects[i] = e;
            }
        }
    }

    
    private float CalculateEffectsRate(double gameMinutes)
    {
        float totalRate = 0f;
        foreach (var effect in _effects)
        {
            if (gameMinutes >= effect.startAtGameMin && gameMinutes < effect.endAtGameMin)
            {
                totalRate += effect.ratePerGameMin;
            }
        }
        return totalRate;
    }
    
    public void AddTransientGame(float amountSigned, float durationGameMin = 0f)
    {
        if (Mathf.Approximately(durationGameMin, 0f))
        {
            sugarLevel = Mathf.Clamp(sugarLevel + amountSigned, minSugarClamp, maxSugarClamp);
            UpdateSugarUI();
            return;
        }

        float rate = amountSigned / Mathf.Max(0.0001f, durationGameMin);
        _transients.Add(new Transient {
            ratePerGameMin    = rate,
            remainingGameMin  = durationGameMin
        });
    }

    public void AddTransientDecreaseGame(float amount, float durationGameMin = 0f)
        => AddTransientGame(-Mathf.Abs(amount), durationGameMin);

    public void AddTransientIncreaseGame(float amount, float durationGameMin = 0f)
        => AddTransientGame(+Mathf.Abs(amount), durationGameMin);


    private void UpdateTrendArrows_AfterTrim(int newSign, double nowGM)
    {
        const double EPS_GAMEMIN = 1e-4;
        const float  MIN_SHOW_SEC = 0.25f;

        if (newSign != _currentTrendSign)
        {
            if (_currentTrendSign != 0)
                TimedChangeEnded?.Invoke(_currentTrendSign > 0);

            if (newSign != 0)
            {
                double endsAt = ComputeNextSignFlipTime(nowGM);
                if (endsAt <= nowGM + EPS_GAMEMIN)
                    endsAt = nowGM + EPS_GAMEMIN;

                float durationSec = GameTime.GameMinutesToRealSeconds((float)(endsAt - nowGM));
                durationSec = Mathf.Max(durationSec, MIN_SHOW_SEC);

                TimedChangeStarted?.Invoke(newSign > 0, durationSec);
                _currentTrendEndsAt = endsAt;
            }
            else
            {
                _currentTrendEndsAt = -1;
            }

            _currentTrendSign = newSign;
        }
        else if (newSign != 0 && _currentTrendEndsAt > 0 && nowGM >= _currentTrendEndsAt - EPS_GAMEMIN)
        {
            double endsAt = ComputeNextSignFlipTime(nowGM);
            if (endsAt <= nowGM + EPS_GAMEMIN)
                endsAt = nowGM + EPS_GAMEMIN;

            float durationSec = GameTime.GameMinutesToRealSeconds((float)(endsAt - nowGM));
            durationSec = Mathf.Max(durationSec, MIN_SHOW_SEC);

            TimedChangeStarted?.Invoke(newSign > 0, durationSec);
            _currentTrendEndsAt = endsAt;
        }
    }


    
    private int GetSign(float rate)
    {
        if (Mathf.Abs(rate) < 1e-6f) return 0;
        return rate > 0 ? 1 : -1;
    }
    
    private (float durationGameMin, double endsAtGameMin) CalculateTrendDuration(double nowGM, int currentSign)
    {
        if (currentSign == 0)
            return (0f, nowGM);
        
        Effect? dominantEffect = null;
        float maxAbsRate = 0f;

        foreach (var effect in _effects)
        {
            if (nowGM >= effect.startAtGameMin && nowGM < effect.endAtGameMin)
            {
                float absRate = Mathf.Abs(effect.ratePerGameMin);
                bool sameDirection = (currentSign > 0 && effect.ratePerGameMin > 0) || 
                                   (currentSign < 0 && effect.ratePerGameMin < 0);
                
                if (sameDirection && absRate > maxAbsRate)
                {
                    maxAbsRate = absRate;
                    dominantEffect = effect;
                }
            }
        }
        
        if (!dominantEffect.HasValue)
            return (0f, nowGM);
        
        double dominantEnd = dominantEffect.Value.endAtGameMin;
        
        var changePoints = new List<double>();
        foreach (var effect in _effects)
        {
            if (effect.startAtGameMin > nowGM && effect.startAtGameMin < dominantEnd)
                changePoints.Add(effect.startAtGameMin);
            if (effect.endAtGameMin > nowGM && effect.endAtGameMin < dominantEnd)
                changePoints.Add(effect.endAtGameMin);
        }
        changePoints.Sort();

        double closestFlip = dominantEnd;
        
        foreach (var point in changePoints)
        {
            float rateAtPoint = CalculateEffectsRate(point + 1e-5);
            int signAtPoint = GetSign(rateAtPoint);
            
            if (signAtPoint != currentSign)
            {
                closestFlip = point;
                break;
            }
        }

        float duration = Mathf.Max(0f, (float)(closestFlip - nowGM));
        return (duration, closestFlip);
    }

    // ===== API ראשי =====
    /// <summary>
    /// מתזמן אפקט: amountSigned יחידות סוכר מפוזרות לינארית על פני (durationGameMin - entryGameMin).
    /// </summary>
    public void ScheduleEffectGame(float amountSigned, float durationGameMin, float entryGameMin)
    {
        durationGameMin = Mathf.Max(0f, durationGameMin);
        entryGameMin = Mathf.Max(0f, entryGameMin);
        
        float effectiveDuration = durationGameMin - entryGameMin;
        if (effectiveDuration <= 0.0001f)
        {
            // השפעה מיידית
            SetSugarInstant(sugarLevel + amountSigned);
            return;
        }

        float ratePerGameMin = amountSigned / effectiveDuration;

        double nowGM = _absGameMinutes;
        var effect = new Effect
        {
            ratePerGameMin = ratePerGameMin,
            startAtGameMin = nowGM + entryGameMin,
            endAtGameMin = nowGM + durationGameMin,
            id = _nextEffectId++
        };
        _effects.Add(effect);

        // אירוע תזמון
        float realDelaySec = GameTime.GameMinutesToRealSeconds(entryGameMin);
        float realDurSec = GameTime.GameMinutesToRealSeconds(effectiveDuration);
        bool isIncrease = amountSigned > 0f;
        TimedChangeScheduled?.Invoke(isIncrease, realDelaySec, realDurSec);
    }

    // ===== תאימות לאחור =====
    public void AddSugarGame(float amount, float durationGameMin = 0f, float delayGameMin = 0f)
    {
        amount = Mathf.Abs(amount);
        if (durationGameMin <= 0f)
            ScheduleEffectGame(+amount, 1f, delayGameMin);
        else
            ScheduleEffectGame(+amount, durationGameMin, delayGameMin);
    }

    public void DecreaseSugarGame(float amount, float durationGameMin = 0f, float delayGameMin = 0f)
    {
        amount = Mathf.Abs(amount);
        if (durationGameMin <= 0f)
            ScheduleEffectGame(-amount, 1f, delayGameMin);
        else
            ScheduleEffectGame(-amount, durationGameMin, delayGameMin);
    }

    public void AddImmediateGame(float amountSigned, float durationGameMin)
    {
        if (Mathf.Approximately(durationGameMin, 0f))
        {
            SetSugarInstant(Mathf.Clamp(sugarLevel + amountSigned, minSugarClamp, maxSugarClamp));
            return;
        }
        ScheduleEffectGame(amountSigned, durationGameMin, 0f);
    }

    public void AddImmediateIncreaseGame(float amount, float durationGameMin)
        => AddImmediateGame(+Mathf.Abs(amount), durationGameMin);

    public void AddImmediateDecreaseGame(float amount, float durationGameMin)
        => AddImmediateGame(-Mathf.Abs(amount), durationGameMin);
    
    void UpdateSugarUI()
    {
        if (sugarText)
            if (sugarLevel > 500)
            {
                sugarText.text = "HIGH";
                sugarText.color = Color.yellow;
            }
            else
            {
                sugarText.color = Color.white;
                sugarText.text = Mathf.RoundToInt(sugarLevel).ToString();
            }
            
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].enabled = i < currentHearts;
    }

    public void SetHeartsPaused(bool paused, bool resetProgress = true)
    {
        heartsPaused = paused;
        if (paused && resetProgress)
        {
            timeInsideSafeRange = 0f;
            timeOutsideSafeRange = 0f;
        }
    }

    public float GetSugarLevel() => sugarLevel;
    public int CurrentHearts => currentHearts;

    public void SetSugarInstant(float value)
    {
        sugarLevel = Mathf.Clamp(value, minSugarClamp, maxSugarClamp);
        UpdateSugarUI();
    }

    public void ForceSetForLevel(float sugar, bool clearTrends = true)
    {
        if (clearTrends)
        {
            _effects.Clear();
            _currentTrendSign = 0;
            _currentTrendEndsAt = -1;
        }
        SetSugarInstant(sugar);
        s_trends = new TrendsState { has = false };
        s_savedSugar = sugar;
        s_savedHearts = currentHearts;
        s_hasSaved = true;
        _absGameMinutes = 0;
    }

    public static void ClearSavedState(bool clearTrends = true)
    {
        s_hasSaved = false;
        s_savedSugar = 0f;
        s_savedHearts = 0;
        if (clearTrends)
            s_trends = new TrendsState { has = false };
    }

    public void ResetHearts(int value)
    {
        currentHearts = Mathf.Clamp(value, 0, maxHearts);
        UpdateHeartsUI();
    }

    public static void RequestSkipRestoreOnNextStart()
    {
        s_skipRestoreOnce = true;
    }
}