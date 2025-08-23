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
    }

    public void PlayUseItemFX(Color flashColor, bool withEyesClosed = false)
    {
        if (flashCo != null) StopCoroutine(flashCo);
        if (punchCo != null) StopCoroutine(punchCo);
        if (withEyesClosed && eyesCo != null) StopCoroutine(eyesCo);

        flashCo = StartCoroutine(FlashRoutine(flashColor));
        punchCo = StartCoroutine(ScalePunchRoutine());
        
        if (withEyesClosed && eyesClosedRenderer)
            eyesCo = StartCoroutine(EyesClosedRoutine(flashDuration + extraEyesTime));
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        var original = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            original[i] = sprites[i] ? sprites[i].color : Color.white;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float a = flashCurve.Evaluate(Mathf.Clamp01(t / flashDuration));
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i]) sprites[i].color = Color.Lerp(original[i], flashColor, a);
            yield return null;
        }

        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i]) sprites[i].color = original[i];
    }

    private IEnumerator ScalePunchRoutine()
    {
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float a = punchCurve.Evaluate(Mathf.Clamp01(t / punchDuration));
            float s = Mathf.Lerp(1f, punchScale, a);
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }
    
    private IEnumerator EyesClosedRoutine(float duration)
    {
        eyesClosedRenderer.enabled = true;
        yield return new WaitForSeconds(duration);
        if (eyesClosedRenderer) eyesClosedRenderer.enabled = false;
    }
    
    public void ForceEyesOpen()
    {
        if (eyesClosedRenderer) eyesClosedRenderer.enabled = false;
    }
}
