using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DailyPopupController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Animator instructionsAnimator;

    [Header("Behaviour")]
    [SerializeField] private bool showOnSceneStart   = false;
    [SerializeField] private float showDelayOnStart  = 0.05f;
    [SerializeField] private bool unlockInventoryOnConfirm = true;

    [Header("Timer pause policy")]
    [Tooltip("לעצור את השעון רק כשהפופאפ פתוח בסצנה הראשונה")]
    [SerializeField] private bool pauseTimerOnlyInFirstScene = true;

    [Tooltip("איזו סצנה נחשבת 'ראשונה' לפי Build Index")]
    [SerializeField] private int firstSceneBuildIndex = 0;

    // אם מעדיפים לפי שם:
    //[SerializeField] private string firstSceneName = "Intro";

    private PlayerMover playerMover;
    private bool isShowing = false;
    
    private bool pausedTimerByMe = false;

    private void Awake()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (Timer.Instance != null)
        {
            Timer.Instance.OnNewDay += HandleNewDay;
        }
        else
        {
            StartCoroutine(WaitForTimerThenSubscribe());
        }
    }

    private void OnDisable()
    {
        if (Timer.Instance != null)
        {
            Timer.Instance.OnNewDay -= HandleNewDay;
        }

        // אם האובייקט כובה/הושמד בזמן שהפופאפ פתוח – נשחרר את הפאוזה אם אנחנו יצרנו אותה
        TryUnpauseTimer();
    }

    private IEnumerator WaitForTimerThenSubscribe()
    {
        while (Timer.Instance == null) yield return null;
        Timer.Instance.OnNewDay += HandleNewDay;
    }

    private void Start()
    {
        if (showOnSceneStart)
        {
            StartCoroutine(ShowAfterDelay(showDelayOnStart));
        }
    }

    private IEnumerator ShowAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        ShowPopup();
    }

    private void HandleNewDay(long newDayCount)
    {
        ShowPopup();
    }

    private bool IsFirstScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.buildIndex == 1;
        // אם מעדיפים לפי שם:
        // return scene.name == firstSceneName;
    }

    private bool ShouldPauseTimer()
    {
        if (!pauseTimerOnlyInFirstScene) return false; // ביקשת שלא לעצור בכלל
        return IsFirstScene();
    }

    private void ShowPopup()
    {
        if (isShowing) return;

        if (playerMover == null)
            playerMover = FindObjectOfType<PlayerMover>();

        if (popupPanel != null) popupPanel.SetActive(true);
        if (instructionsAnimator != null) instructionsAnimator.SetTrigger("Show");
        if (playerMover != null) playerMover.SetInputLocked(true);

        // עצירת השעון – רק אם זו הסצנה הראשונה ולפי המדיניות
        if (Timer.Instance != null && ShouldPauseTimer())
        {
            Timer.Instance.PauseClock(true);
            pausedTimerByMe = true;
        }

        isShowing = true;
    }

    public void ClosePopup()
    {
        if (!isShowing) return;

        if (instructionsAnimator != null) instructionsAnimator.SetTrigger("Hide");
        if (popupPanel != null) popupPanel.SetActive(false);
        if (playerMover != null) playerMover.SetInputLocked(false);

        TryUnpauseTimer();
        isShowing = false;
    }

    private void TryUnpauseTimer()
    {
        if (pausedTimerByMe && Timer.Instance != null)
        {
            Timer.Instance.PauseClock(false);
            pausedTimerByMe = false;
        }
    }
}
