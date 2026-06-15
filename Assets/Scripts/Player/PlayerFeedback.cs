using System.Collections;
using UnityEngine;

public class PlayerFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] sprites;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scale Punch")]
    [SerializeField] private float punchScale = 1.08f;
    [SerializeField] private float punchDuration = 0.12f;
    [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [SerializeField] private SpriteRenderer eyesClosedRenderer;
    [SerializeField] private float extraEyesTime = 0f;
    
    private Coroutine flashCo, punchCo, eyesCo;
    private Vector3 baseScale;
    private Color[] originalColors;

    void Reset()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
    }

    void Awake()
    {
        baseScale = transform.localScale;
        if (sprites == null || sprites.Length == 0)
            sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (eyesClosedRenderer) eyesClosedRenderer.enabled = false;
        
        StoreOriginalColors();
    }

    private void StoreOriginalColors()
    {
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            originalColors[i] = sprites[i] ? sprites[i].color : Color.white;
    }

    public void PlayUseItemFX(Color flashColor, bool withEyesClosed = false)
    {
        if (flashCo != null) 
        {
            StopCoroutine(flashCo);
            RestoreOriginalColors();
        }
        
        if (punchCo != null) StopCoroutine(punchCo);
        if (withEyesClosed && eyesCo != null) StopCoroutine(eyesCo);

        flashCo = StartCoroutine(FlashRoutine(flashColor));
        punchCo = StartCoroutine(ScalePunchRoutine());
        
        if (withEyesClosed && eyesClosedRenderer)
            eyesCo = StartCoroutine(EyesClosedRoutine(flashDuration + extraEyesTime));
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < sprites.Length && i < originalColors.Length; i++)
            if (sprites[i]) sprites[i].color = originalColors[i];
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        float t = 0f;
        while (t < flashDuration)
        {
            // שינוי כאן! Unscaled time
            t += Time.unscaledDeltaTime; 
            float a = flashCurve.Evaluate(Mathf.Clamp01(t / flashDuration));
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i]) sprites[i].color = Color.Lerp(originalColors[i], flashColor, a);
            yield return null;
        }
        
        RestoreOriginalColors();
        flashCo = null;
    }

    private IEnumerator ScalePunchRoutine()
    {
        float t = 0f;

        float signX = Mathf.Sign(transform.localScale.x);
        Vector3 baseAbs = new Vector3(Mathf.Abs(baseScale.x), Mathf.Abs(baseScale.y), Mathf.Abs(baseScale.z));

        while (t < punchDuration)
        {
            // שינוי כאן! Unscaled time
            t += Time.unscaledDeltaTime; 
            float a = punchCurve.Evaluate(Mathf.Clamp01(t / punchDuration));
            float s = Mathf.Lerp(1f, punchScale, a);
            
            transform.localScale = new Vector3(signX * baseAbs.x * s, baseAbs.y * s, baseAbs.z);
            yield return null;
        }

        transform.localScale = new Vector3(signX * baseAbs.x, baseAbs.y, baseAbs.z);
        punchCo = null;
    }

    private IEnumerator EyesClosedRoutine(float duration)
    {
        eyesClosedRenderer.enabled = true;
        yield return new WaitForSecondsRealtime(duration); 
        if (eyesClosedRenderer) eyesClosedRenderer.enabled = false;
        eyesCo = null;
    }
    
    public void ForceEyesOpen()
    {
        if (eyesClosedRenderer) eyesClosedRenderer.enabled = false;
    }
}