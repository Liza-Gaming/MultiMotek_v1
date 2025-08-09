using UnityEngine;
using UnityEngine.UI;

public class SugarBar : MonoBehaviour
{
    public SugarMeter sugar;

    [Header("Mapping (20 -> 0%, 250 -> 100%)")]
    public float minSugar = 20f;
    public float maxSugar = 250f;

    [Header("UI")]
    public Image fill;
    public Gradient gradient;

    [Header("Smoothing")]
    public bool smooth = true;
    public float smoothSpeed = 5f;

    private float current01;

    void Awake()
    {
        if (sugar == null) sugar = SugarMeter.Instance;
    }
    
    void OnEnable()
    {
        SnapToCurrent();
    }

    void Update()
    {
        if (sugar == null || fill == null) return;

        float raw = sugar.GetSugarLevel();
        float t = Mathf.InverseLerp(minSugar, maxSugar, raw);
        t = Mathf.Clamp01(t);
        
        current01 = smooth
            ? Mathf.Lerp(current01, t, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime))
            : t;

        fill.fillAmount = current01;
        if (gradient != null) fill.color = gradient.Evaluate(current01);
    }
    
    public void SnapToCurrent()
    {
        if (sugar == null || fill == null) return;
        float t = Mathf.Clamp01(Mathf.InverseLerp(minSugar, maxSugar, sugar.GetSugarLevel()));
        current01 = t;
        fill.fillAmount = t;
        if (gradient != null) fill.color = gradient.Evaluate(t);
    }
}