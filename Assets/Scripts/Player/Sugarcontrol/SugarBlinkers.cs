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
            sm.TimedChangeBegan += OnTimedChangeBegan;
            sm.TimedChangeEnded += OnTimedChangeEnded;
        }
    }

    void TryUnsubscribe()
    {
        var sm = SugarMeter.Instance;
        if (sm)
        {
            sm.TimedChangeBegan -= OnTimedChangeBegan;
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

    public void ShowUp(float duration = -1f)   => ManualBump(true,  duration);
    public void ShowDown(float duration = -1f) => ManualBump(false, duration);

    public void SuppressForSeconds(float seconds)
    {
        suppressUntil = Mathf.Max(suppressUntil, Now + Mathf.Max(0f, seconds));
        HideImmediate();
    }

    private void ManualBump(bool isUp, float duration)
    {
        if (Now < suppressUntil) return;

        float dur   = (duration > 0f) ? duration : 2f;
        float until = Now + dur;
        if (isUp) upUntil = Mathf.Max(upUntil, until);
        else      downUntil = Mathf.Max(downUntil, until);

        EnsureRoutine();
    }

    private void OnTimedChangeBegan(bool isIncrease, float duration)
    {
        if (Now < suppressUntil) return;

        float until = Now + Mathf.Max(0f, duration);
        if (isIncrease) upUntil = Mathf.Max(upUntil, until);
        else            downUntil = Mathf.Max(downUntil, until);

        EnsureRoutine();
    }

    private void OnTimedChangeEnded(bool isIncrease)
    {
        
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
