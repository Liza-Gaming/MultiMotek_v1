using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SugarSummaryUI : MonoBehaviour
{
    public SugarStats stats;
    public SugarMeter meter;

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

    public void ShowSummary()
    {
        if (_shown) return;
        _shown = true;

        if (!stats) stats = FindObjectOfType<SugarStats>(true);
        if (!meter) meter = FindObjectOfType<SugarMeter>(true);
        
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        
        stats.GetPercents(out float inPct, out float abovePct, out float belowPct);
        
        if (inRangeText) inRangeText.text = $"{inPct:0}%";
        if (belowText)   belowText.text   = $"{belowPct:0}%";
        if (aboveText)   aboveText.text   = $"{abovePct:0}%";
        
        if (heartsDisplay && meter)
        {
            heartsDisplay.SetHearts(meter.CurrentHearts, meter.maxHearts, animate:true);
        }
        
        if (barBelow) barBelow.SetPercent(0, animated:false);
        if (barAbove) barAbove.SetPercent(0, animated:false);
        if (barIn)    barIn.SetPercent(0, animated:false);

        gameObject.SetActive(true);
        Time.timeScale = 0f;
        
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
            // yield return new WaitForSecondsRealtime(barBelow.duration + rowDelay);
        }
        
        
    }

    public void ConfirmAndContinue()
    {
        Time.timeScale = 1f;

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
