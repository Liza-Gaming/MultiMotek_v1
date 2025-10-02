using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SugarMeter : MonoBehaviour
{
    public static SugarMeter Instance;

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

    public float timeOutsideRangeToLoseHeart = 20f;
    public float timeInsideRangeToGainHeart  = 20f;

    private float timeOutsideSafeRange = 0f, timeInsideSafeRange = 0f;

    public Image[] heartImages;
    public Text    sugarText;
    private static bool s_skipRestoreOnce;
    private float sugarLevel;

    [SerializeField] private bool heartsPaused = false;

    // ===== Superposition Model =====
    private struct Effect
    {
        public float  ratePerGameMin;    // יחידות סוכר לדקת-משחק
        public double startAtGameMin;    // התחלה (דקות-משחק אבסולוטי)
        public double endAtGameMin;      // סוף (דקות-משחק אבסולוטי)
        public int    id;                // מזהה פנימי
    }
    private readonly List<Effect> _effects = new();
    private int _nextEffectId = 1;

// ===== Transients (momentary deltas outside trends) =====
    private struct Transient
    {
        public float ratePerGameMin;     // יכול להיות חיובי/שלילי
        public float remainingGameMin;   // זמן-משחק שנותר לפריסה (0 => נגמר)
    }
    private readonly List<Transient> _transients = new();


    
    // הבסיס - אינסולין ארוך (פועל תמיד, לא מוצג בחיצים)
    private float BaselineRatePerGameMin => -(sugarDecreaseRate / 60f);

    // זמן אבסולוטי בדקות-משחק
    private double _absGameMinutes = 0;

    // מעקב אחר מגמה נוכחית (בלי הבסיס)
    private int _currentTrendSign = 0; // -1, 0, +1
    private double _currentTrendEndsAt = -1; // מתי המגמה הנוכחית מסתיימת

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

    void OnDisable()
    {
        s_savedSugar  = sugarLevel;
        s_savedHearts = currentHearts;
        s_hasSaved    = true;
        s_trends = new TrendsState { has = true };
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // זמן משחק
        float gsrs = Timer.Instance ? Timer.Instance.GameSecondsPerRealSecond : 30f;
        float gameMinutesThisFrame = dt * (gsrs / 60f);
        _absGameMinutes += gameMinutesThisFrame;
        double nowGM = _absGameMinutes;
        
        // === Apply transients (do NOT affect trends/arrows) ===
        if (_transients.Count > 0)
        {
            float gmStep = gameMinutesThisFrame;
            for (int i = _transients.Count - 1; i >= 0; i--)
            {
                var tr = _transients[i];

                if (tr.remainingGameMin <= 0.0001f)
                {
                    // "מיידי": כל הכמות בפריים הזה
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


        // ניקוי אפקטים שהסתיימו
        for (int i = _effects.Count - 1; i >= 0; i--)
            if (nowGM >= _effects[i].endAtGameMin) _effects.RemoveAt(i);

        // קצב השפעות (ללא בסיס)
        float effectsRate = CalculateEffectsRate(nowGM);

        // --- ניטרול זנב: גוזרים את האפקטים ההפוכים עד ה-flip הבא ---
        int sign = GetSign(effectsRate);
        if (sign != 0)
        {
            double flipAt = ComputeNextSignFlipTime(nowGM);   // איפה הנטו מתהפך
            TrimOppositeEffectsUntil(sign, nowGM, flipAt);    // גזירת הזנב
            effectsRate = CalculateEffectsRate(nowGM);        // חישוב מחדש אחרי הגזירה
            _currentTrendEndsAt = flipAt;                     // נשמור לאינדיקציה/חץ
        }
        else
        {
            _currentTrendEndsAt = -1;
        }

        // קצב כולל: בסיס רק כשאין אפקטים פעילים
        float totalRate = (sign == 0) ? BaselineRatePerGameMin : effectsRate;

        // עדכון רמת הסוכר
        sugarLevel = Mathf.Clamp(
            sugarLevel + totalRate * gameMinutesThisFrame,
            minSugarClamp, maxSugarClamp
        );

        // חיצים לפי האפקטים בלבד
        UpdateTrendArrows_AfterTrim(sign, nowGM);

        UpdateSugarUI();
        UpdateHeartsLogic();
    }

    
    // סכום קצב האפקטים (ללא בסיס) בזמן gm
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

// מוצא את זמן ההיפוך הבא של סימן הנטו (לפי אפקטים בלבד)
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
            if (signAfter != signNow) return t; // כאן מתהפך הסימן
        }
        return nowGM + 24 * 60; // אופק רחוק (יום משחק) אם אין היפוך נראה לעין
    }

// גוזר אפקטים מהכיוון ההפוך כך שלא יישאר "זנב" אחרי המנצח
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

            // אם האפקט חוצה את נקודת ה-trim — נגזור את הסוף שלו
            if (e.startAtGameMin < trimAt && e.endAtGameMin > trimAt)
            {
                e.endAtGameMin = trimAt;
                _effects[i] = e;
            }
            // אם הוא כולו אחרי trimAt – לא נוגעים (הוא יהיה רלוונטי רק אם בעתיד יתחיל כיוון חדש)
        }
    }


    // מחשב את סכום קצבי ההשפעות (ללא בסיס) בזמן נתון
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
    
    // Adds a transient delta that does NOT participate in trends.
// durationGameMin == 0 -> instant; otherwise spreads linearly over duration.
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
        // ללא חיצים/אירועים
    }

    public void AddTransientDecreaseGame(float amount, float durationGameMin = 0f)
        => AddTransientGame(-Mathf.Abs(amount), durationGameMin);

    public void AddTransientIncreaseGame(float amount, float durationGameMin = 0f)
        => AddTransientGame(+Mathf.Abs(amount), durationGameMin);


    private void UpdateTrendArrows_AfterTrim(int newSign, double nowGM)
    {
        const double EPS_GAMEMIN = 1e-4; // ~0.006 דקת-משחק
        const float  MIN_SHOW_SEC = 0.25f;

        if (newSign != _currentTrendSign)
        {
            if (_currentTrendSign != 0)
                TimedChangeEnded?.Invoke(_currentTrendSign > 0);

            if (newSign != 0)
            {
                // תמיד מחשבים אופק טרי (לא משתנים קודמים)
                double endsAt = ComputeNextSignFlipTime(nowGM);
                if (endsAt <= nowGM + EPS_GAMEMIN)
                    endsAt = nowGM + EPS_GAMEMIN; // מבטיחים חיובי

                float durationSec = GameTime.GameMinutesToRealSeconds((float)(endsAt - nowGM));
                durationSec = Mathf.Max(durationSec, MIN_SHOW_SEC); // מינימום חצי-פלאש

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
            // אם עברנו את נקודת הסיום – מחשבים אופק חדש (בדר"כ שינוי מבני)
            double endsAt = ComputeNextSignFlipTime(nowGM);
            if (endsAt <= nowGM + EPS_GAMEMIN)
                endsAt = nowGM + EPS_GAMEMIN;

            float durationSec = GameTime.GameMinutesToRealSeconds((float)(endsAt - nowGM));
            durationSec = Mathf.Max(durationSec, MIN_SHOW_SEC);

            TimedChangeStarted?.Invoke(newSign > 0, durationSec);
            _currentTrendEndsAt = endsAt;
        }
    }



    // מחזיר -1, 0, או 1 לפי הקצב
    private int GetSign(float rate)
    {
        if (Mathf.Abs(rate) < 1e-6f) return 0;
        return rate > 0 ? 1 : -1;
    }

    // מחשב כמה זמן המגמה הנוכחית תימשך
    private (float durationGameMin, double endsAtGameMin) CalculateTrendDuration(double nowGM, int currentSign)
    {
        if (currentSign == 0)
            return (0f, nowGM);

        // מצא את האפקט הדומיננטי = החזק ביותר בכיוון המגמה
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

        // אם אין אפקט דומיננטי, אין מגמה
        if (!dominantEffect.HasValue)
            return (0f, nowGM);

        // משך המגמה = עד שהאפקט הדומיננטי נגמר
        double dominantEnd = dominantEffect.Value.endAtGameMin;

        // עכשיו נבדוק אם לפני שהדומיננטי נגמר, המגמה מתהפכת
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

        // בודק בכל נקודה אם הסימן משתנה
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

    // ===== Hearts =====
    private void UpdateHeartsLogic()
    {
        if (heartsPaused) return;
        
        if (sugarLevel >= minSugar && sugarLevel <= maxSugar)
        {
            timeInsideSafeRange += Time.deltaTime;
            timeOutsideSafeRange = 0f;

            if (timeInsideSafeRange >= timeInsideRangeToGainHeart)
            {
                GainHeart();
                timeInsideSafeRange = 0f;
            }
        }
        else
        {
            timeOutsideSafeRange += Time.deltaTime;
            timeInsideSafeRange = 0f;

            if (timeOutsideSafeRange >= timeOutsideRangeToLoseHeart)
            {
                LoseHeart();
                timeOutsideSafeRange = 0f;
            }
        }
    }

    void GainHeart()
    {
        if (currentHearts < maxHearts)
        {
            currentHearts++;
            UpdateHeartsUI();
        }
    }

    void LoseHeart()
    {
        if (currentHearts > 0)
        {
            currentHearts--;
            UpdateHeartsUI();
            if (currentHearts == 0)
                Debug.Log("Game Over!");
        }
    }

    void UpdateSugarUI()
    {
        if (sugarText)
            sugarText.text = Mathf.RoundToInt(sugarLevel).ToString();
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