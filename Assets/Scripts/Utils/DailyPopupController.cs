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
    [SerializeField] private int firstSceneBuildIndex = 1;
    
    //[SerializeField] private string firstSceneName = "Intro";

    private PlayerMover playerMover;
    private bool isShowing = false;
    
    private bool pausedTimerByMe = false;
    
    private float? savedBaselineRate;
    
    private bool pausedHeartsByMe = false;
    
    private static bool s_didGlobalPauseOnce = false;
    private bool didGlobalPauseThisShow = false;

    private void Awake()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (Timer.Instance != null) Timer.Instance.OnDailyAlarm += HandleDailyAlarm;
        else StartCoroutine(WaitForTimerThenSubscribe());
    }

    private IEnumerator WaitForTimerThenSubscribe()
    {
        while (Timer.Instance == null) yield return null;
        Timer.Instance.OnDailyAlarm += HandleDailyAlarm;
    }

    private void OnDisable()
    {
        if (Timer.Instance != null) Timer.Instance.OnDailyAlarm -= HandleDailyAlarm;
        TryUnpauseTimer();
        TryRestore();
        if (pausedHeartsByMe) {
            var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
            if (sm != null) sm.SetHeartsPaused(false);
            pausedHeartsByMe = false;
        }
    }

    private void HandleDailyAlarm(long dayCount)
    {
        ShowPopup();
    }
    
    private bool IsFirstScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.buildIndex == firstSceneBuildIndex;
    }

    private bool ShouldDoGlobalPauseNow()
    {

        return pauseTimerOnlyInFirstScene && IsFirstScene() && !s_didGlobalPauseOnce;
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

    private bool ShouldPauseTimer()
    {
        if (!pauseTimerOnlyInFirstScene) return false;
        return IsFirstScene();
    }

    private void ShowPopup()
    {
        if (isShowing) return;

        if (playerMover == null)
            playerMover = FindObjectOfType<PlayerMover>();

        if (popupPanel) popupPanel.SetActive(true);
        if (instructionsAnimator) instructionsAnimator.SetTrigger("Show");
        if (playerMover) playerMover.SetInputLocked(true);

        didGlobalPauseThisShow = ShouldDoGlobalPauseNow();

        if (didGlobalPauseThisShow && Timer.Instance != null)
        {
            Timer.Instance.PauseClock(true);
            pausedTimerByMe = true;
        }

        isShowing = true;

        if (didGlobalPauseThisShow)
        {
            var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
            if (sm != null)
            {
                savedBaselineRate = sm.sugarDecreaseRate;
                sm.sugarDecreaseRate = 0f;
                sm.SetHeartsPaused(true, resetProgress:true);
                pausedHeartsByMe = true;
            }
        }
    }


    public void ClosePopup()
    {
        if (!isShowing) return;

        if (instructionsAnimator) instructionsAnimator.SetTrigger("Hide");
        if (popupPanel) popupPanel.SetActive(false);
        if (playerMover) playerMover.SetInputLocked(false);

        TryUnpauseTimer();
        isShowing = false;

        TryRestore();

        if (pausedHeartsByMe) {
            var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
            if (sm != null) sm.SetHeartsPaused(false);
            pausedHeartsByMe = false;
        }

        if (didGlobalPauseThisShow) {
            s_didGlobalPauseOnce = true;
            didGlobalPauseThisShow = false;
        }
    }

    
    
    private void TryRestore() {
        var sm = SugarMeter.Instance;
        if (sm != null && savedBaselineRate.HasValue) {
            sm.sugarDecreaseRate = savedBaselineRate.Value;
            savedBaselineRate = null;
        }
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