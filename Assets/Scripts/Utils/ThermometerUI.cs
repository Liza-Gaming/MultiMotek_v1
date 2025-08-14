using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ThermometerUI : MonoBehaviour
{

    [SerializeField] private Image column;
    [SerializeField] private Image bulb;

    [Header("Start Behaviour")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private float startTemperatureC = 32f;
    [SerializeField] private float minTempC = 0f;
    [SerializeField] private float maxTempC = 40f;

    [Header("Animation")]
    [SerializeField] private float fillDuration = 1.5f;
    [SerializeField] private AnimationCurve fillCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Coloring")]
    [SerializeField] private bool useVerticalGradientSprite = true;
    [SerializeField] private int gradientTextureHeight = 128;
    [SerializeField] private Gradient colorByPercent;
    
    public enum ColorMode { SolidByPercent, VerticalGradient }

    [Header("Coloring")]
    [SerializeField] private ColorMode colorMode = ColorMode.SolidByPercent;

    private Texture2D gradTex;
    private Sprite gradSprite, solidSprite;
    
    void Reset()
    {
        if (!column) column = transform.Find("Column")?.GetComponent<Image>();
        if (!bulb)   bulb   = transform.Find("Bulb")?.GetComponent<Image>();
    }

    void Awake()
    {
        if (column)
        {
            column.type = Image.Type.Filled;
            column.fillMethod = Image.FillMethod.Vertical;
            column.fillOrigin = 0;
        }

        EnsureDefaultGradient();
        
        useVerticalGradientSprite = (colorMode == ColorMode.VerticalGradient);

        if (useVerticalGradientSprite)
        {
            BuildGradientSprite();
            column.sprite = gradSprite;
            column.color = Color.white;
        }
        else
        {
            EnsureSolidSprite();
            column.sprite = solidSprite;
        }
    }
    
    void Start()
    {
        if (animateOnStart) SetTemperature(startTemperatureC, true);
        else                SetTemperature(startTemperatureC, false);
    }
    
    public void SetTemperature(float tempC, bool animate = true)
    {
        float n = Mathf.InverseLerp(minTempC, maxTempC, tempC);
        SetNormalized(n, animate);
    }
    
    public void SetNormalized(float n, bool animate = true)
    {
        n = Mathf.Clamp01(n);
        StopAllCoroutines();
        if (animate) StartCoroutine(AnimateTo(n));
        else Apply(n);
    }

    IEnumerator AnimateTo(float target)
    {
        float from = column ? column.fillAmount : 0f;
        float t = 0f;
        while (t < fillDuration)
        {
            t += Time.deltaTime;
            float a = fillCurve.Evaluate(Mathf.Clamp01(t / fillDuration));
            Apply(Mathf.Lerp(from, target, a));
            yield return null;
        }
        Apply(target);
    }

    void Apply(float n)
    {
        if (!column) return;
        column.fillAmount = n;

        Color c = colorByPercent.Evaluate(n);

        if (colorMode == ColorMode.SolidByPercent)
            column.color = c;

        if (bulb) bulb.color = c;
    }


    void EnsureSolidSprite()
    {
        if (solidSprite != null) return;
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Point;
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        solidSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 1);
    }

    void BuildGradientSprite()
    {
        if (gradTex == null || gradTex.height != gradientTextureHeight)
        {
            gradTex = new Texture2D(1, gradientTextureHeight, TextureFormat.RGBA32, false);
            gradTex.wrapMode = TextureWrapMode.Clamp;
            gradTex.filterMode = FilterMode.Point;
        }
        for (int y = 0; y < gradientTextureHeight; y++)
        {
            float t = y / (gradientTextureHeight - 1f);
            gradTex.SetPixel(0, y, colorByPercent.Evaluate(t));
        }
        gradTex.Apply();

        gradSprite = Sprite.Create(gradTex, new Rect(0, 0, 1, gradientTextureHeight),
            new Vector2(0.5f, 0f), gradientTextureHeight);
    }


    void EnsureDefaultGradient()
    {
        if (colorByPercent.colorKeys == null || colorByPercent.colorKeys.Length == 0)
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.2f,0.6f,1f), 0f),  // כחול
                    new GradientColorKey(new Color(0.2f,1f,1f),  0.35f),// טורקיז
                    new GradientColorKey(new Color(1f,0.9f,0.2f),0.7f), // צהוב
                    new GradientColorKey(new Color(1f,0.3f,0.2f),1f),   // אדום
                },
                new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            );
            colorByPercent = g;
        }
    }
}
