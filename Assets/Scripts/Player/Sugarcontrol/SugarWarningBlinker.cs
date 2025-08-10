using System.Collections;
using UnityEngine;

public class SugarWarningBlinker : MonoBehaviour
{
    [Header("Data source")]
    public SugarMeter sugar;
    
    public float lowThreshold = 60f;
    public float highThreshold = 190f;

    [Header("Blink target & speed")]
    public GameObject target;
    public float blinkInterval = 1f;

    private Coroutine blinkRoutine;
    private CanvasGroup cg;

    void Start()
    {
        if (sugar == null) sugar = SugarMeter.Instance ?? FindObjectOfType<SugarMeter>();
        if (target == null) target = gameObject;

        cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        Show(false);
    }

    void Update()
    {
        if (sugar == null) return;

        float s = sugar.GetSugarLevel();
        bool shouldBlink = (s < lowThreshold) || (s > highThreshold);

        if (shouldBlink)
        {
            if (blinkRoutine == null)
                blinkRoutine = StartCoroutine(Blink());
        }
        else
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
            }
            Show(false);
        }
    }

    private IEnumerator Blink()
    {
        Show(true);
        while (true)
        {
            yield return new WaitForSeconds(blinkInterval);
            Toggle();
        }
    }

    private void Show(bool on)   { if (cg) cg.alpha = on ? 1f : 0f; }
    private void Toggle()        { if (cg) cg.alpha = (cg.alpha > 0.5f) ? 0f : 1f; }
}