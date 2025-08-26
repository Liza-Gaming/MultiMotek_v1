using System.Collections;
using UnityEngine;

public class SugarChangeArrow : MonoBehaviour
{
    [Header("Targets")]
    public GameObject upArrow;
    public GameObject downArrow;

    [Header("Blinking")]
    public float blinkInterval = 0.25f;

    [Header("Coop with Warning")]
    public SugarWarningBlinker warningBlinker;

    private Coroutine routine;
    private CanvasGroup upCg, downCg;
    
    private float upUntil  = -1f;
    private float downUntil= -1f;
    
    private float suppressUntil = -1f;

    void Awake()
    {
        upCg   = GetOrAddCG(upArrow);
        downCg = GetOrAddCG(downArrow);
        HideBoth();
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        TryUnsubscribe();
        HideImmediate();
    }

    void TrySubscribe()
    {
        var sm = SugarMeter.Instance ? SugarMeter.Instance : FindObjectOfType<SugarMeter>();
        if (sm)
        {
            sm.TimedChangeBegan += OnTimedChangeBegan;
            sm.TimedChangeEnded += OnTimedChangeEnded; // אופציונלי, לא חובה ללוגיקה שלנו
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

    public void HideBoth()
    {
        if (upCg)   upCg.alpha   = 0f;
        if (downCg) downCg.alpha = 0f;
    }

    public void HideImmediate()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        HideBoth();
        upUntil = downUntil = -1f;
    }
    
    public void ShowUp(float duration = -1f)   => ManualBump(true,  duration);
    public void ShowDown(float duration = -1f) => ManualBump(false, duration);

    public void SuppressForSeconds(float seconds)
    {
        suppressUntil = Mathf.Max(suppressUntil, Time.time + Mathf.Max(0f, seconds));
        HideImmediate();
    }
    
    private void OnTimedChangeBegan(bool isIncrease, float duration)
    {
        if (Time.time < suppressUntil) return;

        float until = Time.time + Mathf.Max(0f, duration);
        if (isIncrease) upUntil = Mathf.Max(upUntil, until);
        else            downUntil = Mathf.Max(downUntil, until);

        EnsureRoutine();
    }

    private void OnTimedChangeEnded(bool isIncrease)
    {

    }


    private void ManualBump(bool isUp, float duration)
    {
        if (Time.time < suppressUntil) return;

        float dur = (duration > 0f) ? duration : 2f;
        float until = Time.time + dur;
        if (isUp) upUntil = Mathf.Max(upUntil, until);
        else      downUntil = Mathf.Max(downUntil, until);

        EnsureRoutine();
    }

    private void EnsureRoutine()
    {
        float totalDur = Mathf.Max(upUntil, downUntil) - Time.time;
        if (totalDur > 0f && warningBlinker)
            warningBlinker.SuppressForSeconds(totalDur);

        if (routine == null)
            routine = StartCoroutine(BlinkDriver());
    }

    IEnumerator BlinkDriver()
    {
        bool on = true;

        while (Time.time < Mathf.Max(upUntil, downUntil))
        {

            if (upCg)   upCg.alpha   = (Time.time < upUntil)    ? (on ? 1f : 0f) : 0f;
            if (downCg) downCg.alpha = (Time.time < downUntil)  ? (on ? 1f : 0f) : 0f;

            yield return new WaitForSeconds(blinkInterval);
            on = !on;
        }

        HideBoth();
        routine = null;
    }
}
