using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SugarMeter : MonoBehaviour
{
    public static SugarMeter Instance;

    // Persist
    private static bool  s_hasSaved;
    private static float s_savedSugar;
    private static int   s_savedHearts;

    [Tooltip("Initial sugar level")] public float startSugar = 100f;

    [Tooltip("Sugar decrease per game hour (baseline)")]
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

    private float sugarLevel;
    
    [SerializeField] private bool heartsPaused = false;

    // ====== Phase model ======
    private enum Phase { None, Up, Down }
    private Phase _phase = Phase.None;
    
    [Serializable]
    private struct TrendsState {
        public Phase phase;
        public List<RunningEffect> runUp, runDown;
        public List<PendingSpec>   pendUp, pendDown;

        // חדש: אפקטים מיידיים
        public List<ImmediateEffect> immediate;

        public float sugar;
        public bool  has;
    }
    
    private static TrendsState s_trends;

    private struct RunningEffect
    {
        public float ratePerSec;       // קצב קבוע למנה הזו
        public float remaining;        // כמה יחידות נשאר לבצע (חיובי)
        public bool  suppressBaseline; // לכבות baseline כשהיא פעילה
    }

    private struct PendingSpec
    {
        public float amount;
        public float durationSec;
        public bool  suppressBaseline;
    }
    
    // ==== Immediate effects (always applied, independent of phases) ====
    private struct ImmediateEffect {
        public float deltaPerSec;      // יכול להיות חיובי (עלייה) או שלילי (ירידה)
        public float remainingAbs;     // כמה יחידות מוחלטות נשארו להחיל (abs)
        public bool  suppressBaseline; // האם לכבות baseline בזמן שהאפקט רץ
    }
    private readonly List<ImmediateEffect> _immediate = new();


    // רצים כרגע (רק מגמה אחת תהיה פעילה)
    private readonly List<RunningEffect> _runUp   = new();
    private readonly List<RunningEffect> _runDown = new();

    // ממתינים לפאזה ההפוכה
    private readonly List<PendingSpec> _pendUp   = new();
    private readonly List<PendingSpec> _pendDown = new();

    // Events
    public event Action<bool, float>        TimedChangeStarted;   // isIncrease, durationSec
    public event Action<bool, float, float> TimedChangeScheduled; // isIncrease, delaySec, durationSec
    public event Action<bool, float>        TimedChangeBegan;     // isIncrease, durationSec
    public event Action<bool>               TimedChangeEnded;     // isIncrease

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
        if (s_hasSaved)
        {
            sugarLevel    = Mathf.Clamp(s_savedSugar,  minSugarClamp, maxSugarClamp);
            currentHearts = Mathf.Clamp(s_savedHearts, 0,             maxHearts);
        }
        else
        {
            sugarLevel    = startSugar;
            currentHearts = 0;
        }

        UpdateSugarUI();
        UpdateHeartsUI();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Baseline (לשעת-משחק -> לשנייה אמיתית)
        float realSecondsPerGameHour    = Mathf.Max(0.0001f, GameTime.GameHoursToRealSeconds(1f));
        float baselinePerRealSecondDown = -(sugarDecreaseRate / realSecondsPerGameHour);

        float delta = 0f;
        bool suppressBaselineNow = false;

// === Immediate effects (apply first, always) ===
        for (int i = _immediate.Count - 1; i >= 0; i--)
        {
            var e = _immediate[i];
            float step = Mathf.Min(Mathf.Abs(e.deltaPerSec) * dt, e.remainingAbs);
            float signedStep = Mathf.Sign(e.deltaPerSec) * step;

            e.remainingAbs -= step;
            suppressBaselineNow |= e.suppressBaseline;

            if (e.remainingAbs <= 0f) _immediate.RemoveAt(i);
            else                      _immediate[i] = e;

            delta += signedStep; // מצטבר לדלתא
        }


        // מפעילים רק את רשימת הפאזה הנוכחית
        if (_phase == Phase.Up)
        {
            for (int i = _runUp.Count - 1; i >= 0; i--)
            {
                var e = _runUp[i];
                float apply = Mathf.Min(e.ratePerSec * dt, e.remaining);
                e.remaining -= apply;
                delta += apply;
                suppressBaselineNow |= e.suppressBaseline;

                if (e.remaining <= 0f)
                {
                    _runUp.RemoveAt(i);
                    TimedChangeEnded?.Invoke(true);
                }
                else _runUp[i] = e;
            }

            // אם הסתיימו כל העליות – עוברים לפאזה הבאה (אם יש ממתינים)
            if (_runUp.Count == 0)
            {
                _phase = Phase.None;
                ActivatePendingIfExists(); // ייתכן שיעביר ל-Down
            }
        }
        else if (_phase == Phase.Down)
        {
            for (int i = _runDown.Count - 1; i >= 0; i--)
            {
                var e = _runDown[i];
                float apply = Mathf.Min(e.ratePerSec * dt, e.remaining);
                e.remaining -= apply;
                delta -= apply;
                suppressBaselineNow |= e.suppressBaseline;

                if (e.remaining <= 0f)
                {
                    _runDown.RemoveAt(i);
                    TimedChangeEnded?.Invoke(false);
                }
                else _runDown[i] = e;
            }

            if (_runDown.Count == 0)
            {
                _phase = Phase.None;
                ActivatePendingIfExists(); // ייתכן שיעביר ל-Up
            }
        }
        else // Phase.None – אולי יש משהו ממתין להתחיל עכשיו
        {
            ActivatePendingIfExists();
        }

        if (!suppressBaselineNow)
            delta += baselinePerRealSecondDown * dt;

        sugarLevel = Mathf.Clamp(sugarLevel + delta, minSugarClamp, maxSugarClamp);

        UpdateSugarUI();
        UpdateHeartsLogic();
    }
    
    void OnEnable() {
        if (s_trends.has) {
            _phase = s_trends.phase;
            _runUp.Clear();    if (s_trends.runUp != null)    _runUp.AddRange(s_trends.runUp);
            _runDown.Clear();  if (s_trends.runDown != null)  _runDown.AddRange(s_trends.runDown);
            _pendUp.Clear();   if (s_trends.pendUp != null)   _pendUp.AddRange(s_trends.pendUp);
            _pendDown.Clear(); if (s_trends.pendDown != null) _pendDown.AddRange(s_trends.pendDown);

            _immediate.Clear(); if (s_trends.immediate != null) _immediate.AddRange(s_trends.immediate);
            // אם תרצי גם את ערך הסוכר: SetSugarInstant(s_trends.sugar);
        }
    }

    void OnDisable() {
        s_savedSugar  = sugarLevel;
        s_savedHearts = currentHearts;
        s_hasSaved    = true;

        s_trends = new TrendsState {
            phase     = _phase,
            runUp     = new List<RunningEffect>(_runUp),
            runDown   = new List<RunningEffect>(_runDown),
            pendUp    = new List<PendingSpec>(_pendUp),
            pendDown  = new List<PendingSpec>(_pendDown),
            immediate = new List<ImmediateEffect>(_immediate), // חדש
            sugar     = sugarLevel,
            has       = true
        };
    }
    
    public void AddImmediateGame(float amountSigned, float durationGameMin, bool suppressBaselineDuring = false)
    {
        // amountSigned > 0 → עלייה ; amountSigned < 0 → ירידה
        if (Mathf.Approximately(durationGameMin, 0f)) {
            // אפקט מיידי לגמרי (בלי משך) — נכתוב ישירות
            sugarLevel = Mathf.Clamp(sugarLevel + amountSigned, minSugarClamp, maxSugarClamp);
            UpdateSugarUI();
            return;
        }

        float durationSec = GameTime.GameMinutesToRealSeconds(Mathf.Abs(durationGameMin));
        float rate = amountSigned / Mathf.Max(0.0001f, durationSec);

        _immediate.Add(new ImmediateEffect {
            deltaPerSec     = rate,
            remainingAbs    = Mathf.Abs(amountSigned),
            suppressBaseline = suppressBaselineDuring
        });
    }

// אם נוח לך נגיש גם Helpers ייעודיים:
    public void AddImmediateIncreaseGame(float amount, float durationGameMin, bool suppressBaselineDuring = false)
        => AddImmediateGame(+Mathf.Abs(amount), durationGameMin, suppressBaselineDuring);

    public void AddImmediateDecreaseGame(float amount, float durationGameMin, bool suppressBaselineDuring = false)
        => AddImmediateGame(-Mathf.Abs(amount), durationGameMin, suppressBaselineDuring);


    // ====== Phase helpers ======

    private void ActivatePendingIfExists()
    {
        // אם אין פאזה – נעדיף להמשיך את המגמה "הבאה בתור" אם יש ממתינים
        if (_phase != Phase.None) return;

        if (_pendUp.Count > 0)
        {
            _phase = Phase.Up;
            for (int i = 0; i < _pendUp.Count; i++) ActivateUp(_pendUp[i]);
            _pendUp.Clear();
        }
        else if (_pendDown.Count > 0)
        {
            _phase = Phase.Down;
            for (int i = 0; i < _pendDown.Count; i++) ActivateDown(_pendDown[i]);
            _pendDown.Clear();
        }
    }

    private void ActivateUp(PendingSpec spec)
    {
        float rate = spec.amount / Mathf.Max(0.0001f, spec.durationSec);
        _runUp.Add(new RunningEffect { ratePerSec = rate, remaining = spec.amount, suppressBaseline = spec.suppressBaseline });
        TimedChangeStarted?.Invoke(true, spec.durationSec);
        TimedChangeBegan?.Invoke(true,  spec.durationSec);
    }

    private void ActivateDown(PendingSpec spec)
    {
        float rate = spec.amount / Mathf.Max(0.0001f, spec.durationSec);
        _runDown.Add(new RunningEffect { ratePerSec = rate, remaining = spec.amount, suppressBaseline = spec.suppressBaseline });
        TimedChangeStarted?.Invoke(false, spec.durationSec);
        TimedChangeBegan?.Invoke(false,  spec.durationSec);
    }

    // ====== UI/Hearts ======
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
            timeInsideSafeRange   = 0f;

            if (timeOutsideSafeRange >= timeOutsideRangeToLoseHeart)
            {
                LoseHeart();
                timeOutsideSafeRange = 0f;
            }
        }
    }

    void GainHeart()
    {
        if (currentHearts < maxHearts) { currentHearts++; UpdateHeartsUI(); }
    }

    void LoseHeart()
    {
        if (currentHearts > 0)
        {
            currentHearts--;
            UpdateHeartsUI();
            if (currentHearts == 0) Debug.Log("Game Over!");
        }
    }

    void UpdateSugarUI() { if (sugarText) sugarText.text = Mathf.RoundToInt(sugarLevel).ToString(); }
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
            // לא לצבור "כמעט לב" בזמן פאוז
            timeInsideSafeRange = 0f;
            timeOutsideSafeRange = 0f;
        }
    }

    public float GetSugarLevel() => sugarLevel;
    public int   CurrentHearts  => currentHearts;

    public void SetSugarInstant(float value)
    {
        sugarLevel = Mathf.Clamp(value, minSugarClamp, maxSugarClamp);
        UpdateSugarUI();
    }
    

    public void AddSugarGame(float amount, float durationGameMin = 0f, float delayGameMin = 0f,
                             bool suppressBaselineDuring = false)
    {
        amount = Mathf.Abs(amount);
        float delaySec = GameTime.GameMinutesToRealSeconds(delayGameMin);

        if (durationGameMin <= 0f)
        {
            if (delaySec <= 0f) SetSugarInstant(sugarLevel + amount);
            else                StartCoroutine(ApplyInstantAfterDelay(+amount, delaySec));
            return;
        }

        float durationSec = GameTime.GameMinutesToRealSeconds(durationGameMin);
        TimedChangeScheduled?.Invoke(true, delaySec, durationSec);

        if (delaySec <= 0f) StartEffect(true, amount, durationSec, suppressBaselineDuring);
        else                StartCoroutine(StartEffectAfterDelay(true, amount, durationSec, delaySec, suppressBaselineDuring));
    }

    public void DecreaseSugarGame(float amount, float durationGameMin = 0f, float delayGameMin = 0f, bool suppressBaselineDuring = false)
    {
        amount = Mathf.Abs(amount);
        float delaySec = GameTime.GameMinutesToRealSeconds(delayGameMin);

        if (durationGameMin <= 0f)
        {
            if (delaySec <= 0f) SetSugarInstant(sugarLevel - amount);
            else                StartCoroutine(ApplyInstantAfterDelay(-amount, delaySec));
            return;
        }

        float durationSec = GameTime.GameMinutesToRealSeconds(durationGameMin);
        TimedChangeScheduled?.Invoke(false, delaySec, durationSec);

        if (delaySec <= 0f) StartEffect(false, amount, durationSec, suppressBaselineDuring);
        else                StartCoroutine(StartEffectAfterDelay(false, amount, durationSec, delaySec, suppressBaselineDuring));
    }

    private IEnumerator ApplyInstantAfterDelay(float delta, float delaySec)
    {
        if (delaySec > 0f) yield return new WaitForSeconds(delaySec);
        sugarLevel = Mathf.Clamp(sugarLevel + delta, minSugarClamp, maxSugarClamp);
        UpdateSugarUI();
    }

    private IEnumerator StartEffectAfterDelay(bool isIncrease, float amount, float durationSec, float delaySec, bool suppress)
    {
        if (delaySec > 0f) yield return new WaitForSeconds(delaySec);
        StartEffect(isIncrease, amount, durationSec, suppress);
    }

    private void StartEffect(bool isIncrease, float amount, float durationSec, bool suppress)
    {
        var spec = new PendingSpec { amount = amount, durationSec = durationSec, suppressBaseline = suppress };

        if (_phase == Phase.None)
        {

            _phase = isIncrease ? Phase.Up : Phase.Down;
            if (isIncrease) ActivateUp(spec);
            else            ActivateDown(spec);
        }
        else if ((_phase == Phase.Up  && isIncrease) ||
                 (_phase == Phase.Down && !isIncrease))
        {

            if (isIncrease) ActivateUp(spec);
            else            ActivateDown(spec);
        }
        else
        {

            if (isIncrease) _pendUp.Add(spec);
            else            _pendDown.Add(spec);

        }
    }
}

