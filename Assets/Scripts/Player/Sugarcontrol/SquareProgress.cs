using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SquareProgress : MonoBehaviour
{
   [Header("Refs")]
    public Image fillImage;
    public Text label;

    [Header("Animation")]
    [Range(0,100)] public float valueOnEnable = 0f;
    public bool animateOnEnable = false;
    public float duration = 0.8f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    public bool useUnscaledTime = true;

    float currentPct = 0f;
    Coroutine animCo;

    void Awake()
    {
        
        SetupFillImage();
    }

    void OnEnable()
    {
        if (animateOnEnable) SetPercent(valueOnEnable, true, true);
        else                 SetPercent(valueOnEnable, false);
    }

    void SetupFillImage()
    {
        if (!fillImage) return;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 0f;
        fillImage.preserveAspect = false;
    }

    public void SetPercent(float pct, bool animated = true, bool fromZero = false)
    {
        pct = Mathf.Clamp(pct, 0f, 100f);
        if (animCo != null) StopCoroutine(animCo);

        if (!animated)
        {
            currentPct = pct;
            Apply(currentPct);
        }
        else
        {
            float start = fromZero ? 0f : currentPct;
            animCo = StartCoroutine(Animate(start, pct));
        }
    }

    IEnumerator Animate(float from, float to)
    {
        float t = 0f, dur = Mathf.Max(0.0001f, duration);
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            currentPct = Mathf.Lerp(from, to, ease.Evaluate(k));
            Apply(currentPct);
            yield return null;
        }
        currentPct = to;
        Apply(currentPct);
        animCo = null;
    }

    void Apply(float pct)
    {
        if (fillImage)
            fillImage.fillAmount = pct / 100f;

        if (label)
            label.text = $"{pct:0}%";
    }
}
