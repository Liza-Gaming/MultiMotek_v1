using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SugarSummaryUI : MonoBehaviour
{
    public SugarStats stats;
    public SugarMeter meter;

    [Header("Level Type Settings")]
    public bool isFinalLevelSummary = false;

    [Header("Final Level Fade Settings")]
    public string fadePanelName = "VictoryEnd";
    public float exitFadeDuration = 2f;

    [Header("Texts")]
    public Text inRangeText;
    public Text belowText;
    public Text aboveText;

    [Header("Bars (SquareProgress)")]
    public SquareProgress barBelow;
    public SquareProgress barAbove;
    public SquareProgress barIn;

    [Header("Panel FX")]
    public CanvasGroup panelGroup;
    public float fadeDuration = 0.25f;
    public float rowDelay = 0.25f;

    [Header("Next Level")]
    public string nextSceneName = "";
    public int nextSceneBuildIndex = -1;

    public HeartsDisplay heartsDisplay;
    bool _shown;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject blurVolumeObject;
    
    public void ShowSummary()
    {
        if (_shown) return;
        _shown = true;
        if (blurVolumeObject) blurVolumeObject.SetActive(true);
        if (!stats) stats = FindObjectOfType<SugarStats>(true);
        if (!meter) meter = FindObjectOfType<SugarMeter>(true);
        
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        float inPct, abovePct, belowPct;
        int summaryHearts = 0;

        if (isFinalLevelSummary)
        {
            if (SugarStats.GlobalTotalTime > 0f)
            {
                SugarStats.GetGlobalPercents(out inPct, out abovePct, out belowPct);
                if (stats) summaryHearts = stats.GetGlobalSummaryHearts();
            }
            else
            {
                inPct = 100f;
                abovePct = 0f;
                belowPct = 0f;
                if (stats) summaryHearts = stats.summaryHeartsMax;
            }
        }
        else
        {
            stats.GetLocalPercents(out inPct, out abovePct, out belowPct);
            if (stats) summaryHearts = stats.GetLocalSummaryHearts();
        }
        
        if (inRangeText) inRangeText.text = $"{inPct:0}%";
        if (belowText)   belowText.text   = $"{belowPct:0}%";
        if (aboveText)   aboveText.text   = $"{abovePct:0}%";
        
        if (heartsDisplay && stats)
        {
            heartsDisplay.SetHearts(summaryHearts, stats.summaryHeartsMax, animate:true);
        }
        
        if (barBelow) barBelow.SetPercent(0, animated:false);
        if (barAbove) barAbove.SetPercent(0, animated:false);
        if (barIn)    barIn.SetPercent(0, animated:false);

        StartCoroutine(PlaySequence(inPct, abovePct, belowPct));
    }

    IEnumerator PlaySequence(float inPct, float abovePct, float belowPct)
    {
        if (panelGroup)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = true;
            panelGroup.interactable = true;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                panelGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            panelGroup.alpha = 1f;
        }
        
        if (barAbove)
        {
            barAbove.SetPercent(abovePct, animated:true, fromZero:true);
            yield return new WaitForSecondsRealtime(barAbove.duration + rowDelay);
        }
        
        if (barIn)
        {
            barIn.SetPercent(inPct, animated:true, fromZero:true);
            yield return new WaitForSecondsRealtime(barIn.duration + rowDelay);
        }
        
        if (barBelow)
        {
            barBelow.SetPercent(belowPct, animated:true, fromZero:true);
        }
    }

    public void ConfirmAndContinue()
    {
        if (blurVolumeObject) blurVolumeObject.SetActive(false);
        Time.timeScale = 1f;
        
        if (isFinalLevelSummary)
        {
            StartCoroutine(FadeToMenuRoutine());
        }
        else
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                int idx = nextSceneBuildIndex >= 0
                    ? nextSceneBuildIndex
                    : SceneManager.GetActiveScene().buildIndex + 1;

                SceneManager.LoadScene(idx);
            }
        }
    }
    
    private IEnumerator FadeToMenuRoutine()
    {
        Image blackPanel = null;
        
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == fadePanelName)
            {
                blackPanel = obj.GetComponent<Image>();
                break;
            }
        }

        if (blackPanel != null)
        {
            blackPanel.gameObject.SetActive(true);
            var c = blackPanel.color;
            c.a = 0f;
            blackPanel.color = c;

            float t = 0f;
            while (t < exitFadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / exitFadeDuration);
                blackPanel.color = c;
                yield return null;
            }
            
            c.a = 1f;
            blackPanel.color = c;
        }
        else
        {
            yield return new WaitForSeconds(exitFadeDuration);
        }
        
        SceneManager.LoadScene(0);
    }
}