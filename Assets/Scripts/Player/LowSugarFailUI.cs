using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LowSugarFailUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image blackPanel;
    [SerializeField] private GameObject failButton;
    [SerializeField] private float fadeDuration = 2f;

    [Header("Low Sugar Settings")]
    [SerializeField] private float sugarThreshold = 60f;
    [SerializeField] private float requiredGameMinutesBelow = 60f;

    private float accumulatedGameMinutes = 0f;
    private bool faintTriggered = false;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (blackPanel != null)
        {
            var c = blackPanel.color;
            c.a = 0f;
            blackPanel.color = c;
            blackPanel.gameObject.SetActive(false);
        }

        if (failButton != null)
            failButton.SetActive(false);
    }

    private void Update()
    {
        if (faintTriggered) return;
        if (SugarMeter.Instance == null || Timer.Instance == null) return;

        float sugar = SugarMeter.Instance.GetSugarLevel();

        float gameMinutesThisFrame =
            Time.deltaTime * (Timer.Instance.GameSecondsPerRealSecond / 60f);

        if (sugar < sugarThreshold)
        {
            accumulatedGameMinutes += gameMinutesThisFrame;

            if (accumulatedGameMinutes >= requiredGameMinutesBelow)
            {
                faintTriggered = true;
                StartFaint();
            }
        }
        else
        {
            accumulatedGameMinutes = 0f;
        }
    }

    public void StartFaint()
    {
        if (blackPanel == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        blackPanel.gameObject.SetActive(true);

        var c = blackPanel.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            c.a = Mathf.Lerp(0f, 1f, normalized);
            blackPanel.color = c;
            yield return null;
        }
        
        c.a = 1f;
        blackPanel.color = c;
        
        Timer.Instance.PauseClock(true);
        SugarMeter.Instance.SetSimulationPaused(true);
        
        if (failButton != null)
            failButton.SetActive(true);
    }
}
