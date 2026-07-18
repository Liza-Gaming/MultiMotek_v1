using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SugarStats : MonoBehaviour
{
    [Header("Global Settings (Cross-Scene)")]
    public int menuSceneBuildIndex = 0;
    public int maxSceneToAccumulate = 8;

    [Header("Refs")]
    public SugarMeter sugarMeter;

    [Header("UI (optional)")]
    public Text statsText;
    public float uiUpdateInterval = 0.3f;

    [Header("Summary Rating (Hearts)")]
    public int summaryHeartsMax = 5;

    [Header("Local Stats (read-only)")]
    [SerializeField] private float totalTime = 0f;
    [SerializeField] private float timeInRange = 0f;
    [SerializeField] private float timeAboveRange = 0f;
    [SerializeField] private float timeBelowRange = 0f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject blurVolumeObject;

    private float uiTimer = 0f;
    
    private static float globalTotalTime = 0f;
    private static float globalTimeInRange = 0f;
    private static float globalTimeAboveRange = 0f;
    private static float globalTimeBelowRange = 0f;
    
    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    
    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        ResetLocalStats(); 
        
        if (sugarMeter == null && SugarMeter.Instance != null)
            sugarMeter = SugarMeter.Instance;

        if (s.buildIndex == menuSceneBuildIndex)
        {
            ResetGlobalStats();
        }
    }

    void Start()
    {
        if (sugarMeter == null && SugarMeter.Instance != null)
            sugarMeter = SugarMeter.Instance;

        ResetLocalStats();
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

        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentBuildIndex <= maxSceneToAccumulate && currentBuildIndex != menuSceneBuildIndex)
        {
            globalTotalTime += dt;
            if (s < min)       globalTimeBelowRange += dt;
            else if (s > max)  globalTimeAboveRange += dt;
            else               globalTimeInRange   += dt;
        }

        uiTimer += dt;
        if (statsText != null && uiTimer >= uiUpdateInterval)
        {
            uiTimer = 0f;
            GetLocalPercents(out float inPct, out float abovePct, out float belowPct);
            statsText.text = $"In Range: {inPct:0}% | Above: {abovePct:0}% | Below: {belowPct:0}%";
        }
    }
    
    public void ResetLocalStats()
    {
        totalTime = 0f;
        timeInRange = 0f;
        timeAboveRange = 0f;
        timeBelowRange = 0f;
    }

    public static void ResetGlobalStats()
    {
        globalTotalTime = 0f;
        globalTimeInRange = 0f;
        globalTimeAboveRange = 0f;
        globalTimeBelowRange = 0f;
    }

    public void GetLocalPercents(out float inRangePct, out float abovePct, out float belowPct)
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
    
    public static void GetGlobalPercents(out float inRangePct, out float abovePct, out float belowPct)
    {
        if (globalTotalTime <= 0f)
        {
            inRangePct = abovePct = belowPct = 0f;
            return;
        }
        inRangePct = (globalTimeInRange   / globalTotalTime) * 100f;
        abovePct   = (globalTimeAboveRange / globalTotalTime) * 100f;
        belowPct   = (globalTimeBelowRange / globalTotalTime) * 100f;
    }

    public int GetLocalSummaryHearts()
    {
        GetLocalPercents(out float inPct, out _, out _);
        if (summaryHeartsMax <= 0) return 0;
        int hearts = Mathf.RoundToInt((inPct / 100f) * summaryHeartsMax);
        return Mathf.Clamp(hearts, 0, summaryHeartsMax);
    }
    
    public int GetGlobalSummaryHearts()
    {
        GetGlobalPercents(out float inPct, out _, out _);
        if (summaryHeartsMax <= 0) return 0;
        int hearts = Mathf.RoundToInt((inPct / 100f) * summaryHeartsMax);
        return Mathf.Clamp(hearts, 0, summaryHeartsMax);
    }

    // Local Properties
    public float TotalTime   => totalTime;
    public float TimeInRange => timeInRange;
    public float TimeAbove   => timeAboveRange;
    public float TimeBelow   => timeBelowRange;

    // Global Properties
    public static float GlobalTotalTime   => globalTotalTime;
    public static float GlobalTimeInRange => globalTimeInRange;
    public static float GlobalTimeAbove   => globalTimeAboveRange;
    public static float GlobalTimeBelow   => globalTimeBelowRange;
}