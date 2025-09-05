using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsDisplay : MonoBehaviour
{
    public static HeartsDisplay Instance;
    [Header("Prefabs & Sprites")]
    public Image heartPrefab;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    [Header("Layout")]
    public float spacing = 8f;

    [Header("Animation")]
    public bool animatePop = true;
    public float popDuration = 0.12f;
    public float popScale = 1.2f;
    public bool useUnscaledTime = true;

    private readonly List<Image> hearts = new();
    private int builtForMax = -1;

    void EnsureBuilt(int max)
    {
        if (max < 0) max = 0;
        if (builtForMax == max) return;
        
        foreach (var h in hearts) if (h) Destroy(h.gameObject);
        hearts.Clear();

        for (int i = 0; i < max; i++)
        {
            var img = Instantiate(heartPrefab, transform);
            img.sprite = emptyHeart ? emptyHeart : heartPrefab.sprite;
            img.enabled = true;
            img.rectTransform.localScale = Vector3.one;
            hearts.Add(img);
        }
        
        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            float x = 0f;
            for (int i = 0; i < hearts.Count; i++)
            {
                var rt = hearts[i].rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0f);
                x += rt.sizeDelta.x + spacing;
            }
        }

        builtForMax = max;
    }

    public void SetHearts(int current, int max, bool animate = true)
    {
        if (max < 0) max = 0;
        current = Mathf.Clamp(current, 0, max);

        EnsureBuilt(max);

        for (int i = 0; i < hearts.Count; i++)
        {
            var img = hearts[i];
            if (!img) continue;

            bool shouldBeFull = i < current;
            img.sprite = shouldBeFull && fullHeart ? fullHeart :
                         !shouldBeFull && emptyHeart ? emptyHeart :
                         img.sprite;

            if (animate && shouldBeFull)
                StartCoroutine(Pop(img.rectTransform));
            else
                img.rectTransform.localScale = Vector3.one;
        }
    }

    IEnumerator Pop(RectTransform rt)
    {
        if (!animatePop || rt == null) yield break;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, popDuration);

        // up
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.Lerp(1f, popScale, k);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        
        t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.Lerp(popScale, 1f, k);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        rt.localScale = Vector3.one;
    }
}
