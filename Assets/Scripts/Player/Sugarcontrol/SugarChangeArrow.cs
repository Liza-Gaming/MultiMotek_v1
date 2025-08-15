using System.Collections;
using UnityEngine;

public class SugarChangeArrow : MonoBehaviour
{
    [Header("Targets")]
    public GameObject upArrow;
    public GameObject downArrow;

    [Header("Blinking")]
    public float blinkInterval = 0.25f;
    public float showDuration = 2f;

    [Header("Coop with Warning")]
    public SugarWarningBlinker warningBlinker; 

    private Coroutine routine;
    private CanvasGroup upCg, downCg;
    
    private float suppressUntil = -1f;
    
    void Awake()
    {
        upCg   = GetOrAddCG(upArrow);
        downCg = GetOrAddCG(downArrow);
        HideBoth();
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
        if (upCg) upCg.alpha = 0f;
        if (downCg) downCg.alpha = 0f;
    }
    
    public void HideImmediate()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        HideBoth();
    }

    public void ShowUp(float duration = -1f)  => Show(true,  duration);
    public void ShowDown(float duration = -1f)=> Show(false, duration);
    
    public void SuppressForSeconds(float seconds)
    {
        suppressUntil = Mathf.Max(suppressUntil, Time.time + Mathf.Max(0f, seconds));
        HideImmediate();
    }
    void Show(bool isUp, float duration)
    {
        if (Time.time < suppressUntil) return;

        if (routine != null) StopCoroutine(routine);
        float dur = (duration > 0f) ? duration : showDuration;

        if (warningBlinker) warningBlinker.SuppressForSeconds(dur);

        routine = StartCoroutine(BlinkRoutine(isUp, dur));
    }

    IEnumerator BlinkRoutine(bool isUp, float duration)
    {
        HideBoth();
        CanvasGroup cg = isUp ? upCg : downCg;
        if (cg == null) yield break;

        bool on = true;
        float t = 0f;
        cg.alpha = 1f;

        while (t < duration)
        {
            yield return new WaitForSeconds(blinkInterval);
            on = !on;
            cg.alpha = on ? 1f : 0f;
            t += blinkInterval;
        }

        HideBoth();
        routine = null;
    }
}