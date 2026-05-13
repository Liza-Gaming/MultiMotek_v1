using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SugarFailManager : MonoBehaviour
{
    public static SugarFailManager Instance { get; private set; }

    [Header("General Settings")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("Low Sugar Settings")]
    [SerializeField] private float lowSugarThreshold = 60f;
    [SerializeField] private float requiredGameMinutesBelow = 60f;

    [Header("High Sugar Settings")]
    [SerializeField] private float highSugarThreshold = 500f;
    [SerializeField] private float requiredGameMinutesAbove = 1440f;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip failSound;
    [SerializeField, Range(0f, 1f)] private float failSoundVolume = 1f;
    
    private float accumulatedGameMinutesLow = 0f;
    private float accumulatedGameMinutesHigh = 0f;
    
    private bool faintTriggered = false;
    private Coroutine fadeRoutine;

    // הרפרנס ל-UI של הסצנה הנוכחית
    private FailUIProvider currentUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetupUI(FailUIProvider provider)
    {
        currentUI = provider;
        
        faintTriggered = false;
        accumulatedGameMinutesLow = 0f;
        accumulatedGameMinutesHigh = 0f;

        if (currentUI.blackPanel != null)
        {
            var c = currentUI.blackPanel.color;
            c.a = 0f;
            currentUI.blackPanel.color = c;
            currentUI.blackPanel.gameObject.SetActive(false);
        }

        if (currentUI.loseLowPanel != null) currentUI.loseLowPanel.SetActive(false);
        if (currentUI.loseHighPanel != null) currentUI.loseHighPanel.SetActive(false);
        
        // כיבוי הכפתור המשותף בהתחלה
        if (currentUI.returnToMenuButton != null) currentUI.returnToMenuButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        // אם כבר נפסלנו או שעדיין אין UI מחובר, לא ממשיכים
        if (faintTriggered || currentUI == null) return;
        if (SugarMeter.Instance == null || Timer.Instance == null) return;

        float sugar = SugarMeter.Instance.GetSugarLevel();
        float gameMinutesThisFrame = Time.deltaTime * (Timer.Instance.GameSecondsPerRealSecond / 60f);
        
        // בדיקת סוכר נמוך
        if (sugar < lowSugarThreshold)
        {
            accumulatedGameMinutesLow += gameMinutesThisFrame;
            if (accumulatedGameMinutesLow >= requiredGameMinutesBelow)
            {
                TriggerFail(isLowSugar: true);
            }
        }
        else
        {
            accumulatedGameMinutesLow = 0f; 
        }
        
        // בדיקת סוכר גבוה
        if (sugar >= highSugarThreshold)
        {
            accumulatedGameMinutesHigh += gameMinutesThisFrame;
            if (accumulatedGameMinutesHigh >= requiredGameMinutesAbove)
            {
                TriggerFail(isLowSugar: false);
            }
        }
        else
        {
            accumulatedGameMinutesHigh = 0f;
        }
    }
    
    private void TriggerFail(bool isLowSugar)
    {
        faintTriggered = true;
        
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }

        if (currentUI.blackPanel == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FailSequenceRoutine(isLowSugar));
    }

    private IEnumerator FailSequenceRoutine(bool isLowSugar)
    {
        currentUI.blackPanel.gameObject.SetActive(true);
        var c = currentUI.blackPanel.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            c.a = Mathf.Lerp(0f, 1f, normalized);
            currentUI.blackPanel.color = c;
            yield return null;
        }
        
        c.a = 1f;
        currentUI.blackPanel.color = c;
        PlayFailSound();
        Timer.Instance.PauseClock(true);
        SugarMeter.Instance.SetSimulationPaused(true);

        GameObject activePanel = isLowSugar ? currentUI.loseLowPanel : currentUI.loseHighPanel;
        TypewriterEffect text1 = isLowSugar ? currentUI.lowText1 : currentUI.highText1;
        TypewriterEffect text2 = isLowSugar ? currentUI.lowText2 : currentUI.highText2;

        if (activePanel != null) activePanel.SetActive(true);
        if (text1 != null) yield return StartCoroutine(text1.PlayTypewriter());
        if (text2 != null) yield return StartCoroutine(text2.PlayTypewriter());
        
        // הופעת הכפתור המשותף בסוף התהליך
        if (currentUI.returnToMenuButton != null) currentUI.returnToMenuButton.gameObject.SetActive(true);
    }
    
    public void ReturnToMenu()
    {
        // שחרור הפוז לפני מעבר הסצנה! קריטי, אחרת התפריט יכול לקפוא
        if (Timer.Instance != null) Timer.Instance.PauseClock(false);
        if (SugarMeter.Instance != null) SugarMeter.Instance.SetSimulationPaused(false);
        Time.timeScale = 1f; 
        
        faintTriggered = false; // איפוס לקראת המשחק הבא
        
        // תחליפי את "MainMenu" בשם המדויק של סצנת התפריט שלך
        SceneManager.LoadScene("Intro"); 
    }
    
    private void PlayFailSound()
    {
        if (failSound == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            AudioSource playerSource = player.GetComponent<AudioSource>();
            if (playerSource != null)
            {
                playerSource.PlayOneShot(failSound, failSoundVolume);
            }
        }
    }
}