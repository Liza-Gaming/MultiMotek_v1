using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SugarFailManager : MonoBehaviour
{
    [Header("UI - General")]
    [SerializeField] private Image blackPanel;
    [SerializeField] private float fadeDuration = 2f;

    [Header("UI - LOSELOW Setup")]
    [SerializeField] private GameObject loseLowPanel; // האובייקט שמכיל הכל
    [SerializeField] private TypewriterEffect lowText1; // טקסט ראשון
    [SerializeField] private TypewriterEffect lowText2; // טקסט שני
    [SerializeField] private GameObject lowButton;      // כפתור חזרה/ריסט

    [Header("UI - LOSEHIGH Setup")]
    [SerializeField] private GameObject loseHighPanel;
    [SerializeField] private TypewriterEffect highText1;
    [SerializeField] private TypewriterEffect highText2;
    [SerializeField] private GameObject highButton;

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

    private void Awake()
    {
        if (blackPanel != null)
        {
            var c = blackPanel.color;
            c.a = 0f;
            blackPanel.color = c;
            blackPanel.gameObject.SetActive(false);
        }

        // כיבוי הפאנלים והכפתורים בהתחלה
        if (loseLowPanel != null) loseLowPanel.SetActive(false);
        if (loseHighPanel != null) loseHighPanel.SetActive(false);
        if (lowButton != null) lowButton.SetActive(false);
        if (highButton != null) highButton.SetActive(false);
    }

    private void Update()
    {
        if (faintTriggered) return;
        if (SugarMeter.Instance == null || Timer.Instance == null) return;

        float sugar = SugarMeter.Instance.GetSugarLevel();
        float gameMinutesThisFrame = Time.deltaTime * (Timer.Instance.GameSecondsPerRealSecond / 60f);
        
        // בדיקת סוכר נמוך
        if (sugar < lowSugarThreshold)
        {
            accumulatedGameMinutesLow += gameMinutesThisFrame;
            if (accumulatedGameMinutesLow >= requiredGameMinutesBelow)
            {
                TriggerFail(isLowSugar: true); // מודיע שמדובר בסוכר נמוך
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
                TriggerFail(isLowSugar: false); // מודיע שמדובר בסוכר גבוה
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
        // ----------------------------------------------

        if (blackPanel == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FailSequenceRoutine(isLowSugar));
    }

    private IEnumerator FailSequenceRoutine(bool isLowSugar)
    {
        // 1. החשכת המסך
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
        PlayFailSound();
        Timer.Instance.PauseClock(true);
        SugarMeter.Instance.SetSimulationPaused(true);

        // 2. בחירת האובייקטים הרלוונטיים לפי סוג הפסילה
        GameObject activePanel = isLowSugar ? loseLowPanel : loseHighPanel;
        TypewriterEffect text1 = isLowSugar ? lowText1 : highText1;
        TypewriterEffect text2 = isLowSugar ? lowText2 : highText2;
        GameObject activeButton = isLowSugar ? lowButton : highButton;

        // 3. הדלקת הפאנל המתאים (הטקסטים יהיו ריקים כי דאגנו לזה ב-Awake שלהם)
        if (activePanel != null) activePanel.SetActive(true);

        // 4. הפעלת טקסט ראשון והמתנה עד שיסיים לכתוב (yield return StartCoroutine)
        if (text1 != null) yield return StartCoroutine(text1.PlayTypewriter());

        // 5. הפעלת טקסט שני והמתנה עד שיסיים
        if (text2 != null) yield return StartCoroutine(text2.PlayTypewriter());

        // 6. הופעת הכפתור בסוף התהליך
        if (activeButton != null) activeButton.SetActive(true);
    }
    
    private void PlayFailSound()
    {
        if (failSound == null) return;

        // מחפשים את השחקן לפי התג שלו
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            AudioSource playerSource = player.GetComponent<AudioSource>();
            if (playerSource != null)
            {
                // משתמשים ב-PlayOneShot כדי לא לקטוע סאונדים אחרים אם היו
                playerSource.PlayOneShot(failSound, failSoundVolume);
            }
        }
    }
    
    
}