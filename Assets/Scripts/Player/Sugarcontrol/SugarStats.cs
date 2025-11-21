using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SugarStats : MonoBehaviour
{
    [Header("Refs")]
    public SugarMeter sugarMeter;

    [Header("UI (optional)")]
    public Text statsText;
    public float uiUpdateInterval = 0.3f;

    [Header("Summary Rating (Hearts)")]
    public int summaryHeartsMax = 5;

    [Header("Stats (read-only)")]
    [SerializeField] private float totalTime = 0f;
    [SerializeField] private float timeInRange = 0f;
    [SerializeField] private float timeAboveRange = 0f;
    [SerializeField] private float timeBelowRange = 0f;

    private float uiTimer = 0f;
    
    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    
    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        ResetStats();
        if (sugarMeter == null && SugarMeter.Instance != null)
            sugarMeter = SugarMeter.Instance;
    }

    void Start()
    {
        if (sugarMeter == null && SugarMeter.Instance != null)
            sugarMeter = SugarMeter.Instance;

        ResetStats();
    }

    void Update()
    {
        if (sugarMeter == null) return;

        float dt = Time.deltaTime;
        totalTime += dt;

        float s = sugarMeter.GetSugarLevel();
        float min = sugarMeter.minSugar;
        float max = sugarMeter.maxSugar;

        if (s < min)       timeBelowRange += dt;
        else if (s > max)  timeAboveRange += dt;
        else               timeInRange   += dt;

        uiTimer += dt;
        if (statsText != null && uiTimer >= uiUpdateInterval)
        {
            uiTimer = 0f;
            GetPercents(out float inPct, out float abovePct, out float belowPct);
            statsText.text = $"In Range: {inPct:0}% | Above: {abovePct:0}% | Below: {belowPct:0}%";
        }
    }
    
    public void ResetStats()
    {
        totalTime = 0f;
        timeInRange = 0f;
        timeAboveRange = 0f;
        timeBelowRange = 0f;
    }

    public void GetPercents(out float inRangePct, out float abovePct, out float belowPct)
    {
        if (totalTime <= 0f)
        {
            inRangePct = abovePct = belowPct = 0f;
            return;
        }
        inRangePct = (timeInRange   / totalTime) * 100f;
        abovePct   = (timeAboveRange / totalTime) * 100f;
        belowPct   = (timeBelowRange / totalTime) * 100f;
    }

    public int GetSummaryHearts()
    {
        GetPercents(out float inPct, out _, out _);

        if (summaryHeartsMax <= 0) return 0;

        int hearts = Mathf.RoundToInt((inPct / 100f) * summaryHeartsMax);
        return Mathf.Clamp(hearts, 0, summaryHeartsMax);
    }

    
    public float TotalTime   => totalTime;
    public float TimeInRange => timeInRange;
    public float TimeAbove   => timeAboveRange;
    public float TimeBelow   => timeBelowRange;
}
