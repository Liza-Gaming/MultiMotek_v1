using System.Collections;
using UnityEngine;

public class SugarBlinkers : MonoBehaviour
{
    
    public static SugarBlinkers Instance;
    
    [Header("Targets")]
    public GameObject upArrow;
    public GameObject downArrow;

    [Header("Warning (sugar thresholds)")]
    public GameObject warningIcon;
    public float lowThreshold  = 60f;
    public float highThreshold = 190f;
    public float warningBlinkInterval = 0.25f;

    [Header("Arrows blinking")]
    public float blinkInterval = 0.25f;

    [Header("Time")]
    [Tooltip("Use unscaled time so blinking continues during pause/popups")]
    public bool useUnscaledTime = false;

    private Coroutine   routine;
    private CanvasGroup upCg, downCg, warnCg;

    private float upUntil    = -1f;
    private float downUntil  = -1f;
    private float suppressUntil = -1f;

    private float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

    void Awake()
    {
        Instance = this;
        upCg   = GetOrAddCG(upArrow);
        downCg = GetOrAddCG(downArrow);
        warnCg = GetOrAddCG(warningIcon);
        HideAll();
    }

    void OnEnable()  { TrySubscribe(); }
    void OnDisable() { TryUnsubscribe(); HideImmediate(); }

    void Update()
    {
        // אם אין חיצים פעילים אבל צריך להתריע – נדאג שהקורוטינה תרוץ
        if (Now >= suppressUntil && ShouldWarn())
            EnsureRoutine();
    }

    void TrySubscribe()
    {
        var sm = SugarMeter.Instance ? SugarMeter.Instance : FindObjectOfType<SugarMeter>();
        if (sm)
        {
            sm.TimedChangeStarted += OnTimedChangeBegan;
            sm.TimedChangeEnded += OnTimedChangeEnded;
        }
    }

    void TryUnsubscribe()
    {
        var sm = SugarMeter.Instance;
        if (sm)
        {
            sm.TimedChangeStarted -= OnTimedChangeBegan;
            sm.TimedChangeEnded -= OnTimedChangeEnded;
        }
    }

    CanvasGroup GetOrAddCG(GameObject go)
    {
        if (!go) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    public void HideImmediate()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        HideAll();
        upUntil = downUntil = -1f;
    }

    public void HideBothArrows()
    {
        if (upCg)   upCg.alpha   = 0f;
        if (downCg) downCg.alpha = 0f;
    }

    private void HideAll()
    {
        HideBothArrows();
        if (warnCg) warnCg.alpha = 0f;
    }

    public void ShowUp  (float duration = -1f)               => ManualBump(true,  duration, exclusive:true);
    public void ShowDown(float duration = -1f)               => ManualBump(false, duration, exclusive:true);

// חדש: גרסאות transient שלא מכבות חץ קיים
    public void ShowUpTransient  (float duration = -1f)      => ManualBump(true,  duration, exclusive:false);
    public void ShowDownTransient(float duration = -1f)      => ManualBump(false, duration, exclusive:false);


    public void SuppressForSeconds(float seconds)
    {
        suppressUntil = Mathf.Max(suppressUntil, Now + Mathf.Max(0f, seconds));
        HideImmediate();
    }

    private void ManualBump(bool isUp, float duration, bool exclusive)
    {
        if (Now < suppressUntil) return;

        float dur   = (duration > 0f) ? duration : 2f;
        float until = Now + dur;

        if (exclusive)
        {
            // התנהגות קיימת – חץ יחיד בלבד
            if (isUp)
            {
                downUntil = -1f;
                if (downCg) downCg.alpha = 0f;
                upUntil = Mathf.Max(upUntil, until);
            }
            else
            {
                upUntil = -1f;
                if (upCg) upCg.alpha = 0f;
                downUntil = Mathf.Max(downUntil, until);
            }
        }
        else
        {
            // transient: לא מכבים את החץ הנגדי
            // אם כבר יש חץ בכיוון ההפוך פעיל – נתעלם מהבקשה (כדי לא להציג שני חצים)
            bool oppositeActive = isUp ? (Now < downUntil) : (Now < upUntil);
            if (oppositeActive)
            {
                // מתעלמים – לא לשבור את חץ המגמה
            }
            else
            {
                if (isUp)   upUntil   = Mathf.Max(upUntil,   until);
                else        downUntil = Mathf.Max(downUntil, until);
            }
        }

        EnsureRoutine();
    }



    private void OnTimedChangeBegan(bool isIncrease, float duration)
    {
        if (Now < suppressUntil) return;

        // תמיד מציגים חץ יחיד: מכבים את הכיוון ההפוך מיידית
        float until = Now + Mathf.Max(0f, duration);
        if (isIncrease)
        {
            downUntil = -1f;                 // מכבה ירידה
            if (downCg) downCg.alpha = 0f;
            upUntil = until;                 // מדליק עלייה
        }
        else
        {
            upUntil = -1f;                   // מכבה עלייה
            if (upCg) upCg.alpha = 0f;
            downUntil = until;               // מדליק ירידה
        }

        EnsureRoutine();
    }

    private void OnTimedChangeEnded(bool isIncrease)
    {
        // כשמגמה מסתיימת — מכבים רק את החץ שלה
        if (isIncrease)
        {
            upUntil = -1f;
            if (upCg) upCg.alpha = 0f;
        }
        else
        {
            downUntil = -1f;
            if (downCg) downCg.alpha = 0f;
        }
        // אם שני החיצים כבויים — ייתכן שצריך לעצור את הקורוטינה בסיבוב הבא
    }


    private bool ShouldWarn()
    {
        var sm = SugarMeter.Instance;
        if (sm == null) return false;
        float s = sm.GetSugarLevel();
        return (s < lowThreshold) || (s > highThreshold);
    }

    private void EnsureRoutine()
    {
        if (routine == null)
            routine = StartCoroutine(BlinkDriver());
    }

    IEnumerator BlinkDriver()
    {
        bool on = true;

        while (true)
        {
            bool suppressed = Now < suppressUntil;
            bool upActive   = !suppressed && (Now < upUntil);
            bool downActive = !suppressed && (Now < downUntil);
            bool warnActive = !suppressed && ShouldWarn();

            if (upCg)   upCg.alpha   = upActive   ? (on ? 1f : 0f) : 0f;
            if (downCg) downCg.alpha = downActive ? (on ? 1f : 0f) : 0f;
            if (warnCg) warnCg.alpha = warnActive ? (on ? 1f : 0f) : 0f;

            // אם אין מה להציג – מפסיקים עד לפעם הבאה ש־EnsureRoutine יופעל
            if (!upActive && !downActive && !warnActive)
            {
                HideAll();
                routine = null;
                yield break;
            }

            float wait = Mathf.Min(blinkInterval, warningBlinkInterval);
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(wait);
            else                 yield return new WaitForSeconds(wait);

            on = !on;
        }
    }
}
