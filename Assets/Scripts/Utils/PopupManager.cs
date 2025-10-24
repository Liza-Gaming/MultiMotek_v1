using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }
    
    [Header("Daily gating")]
    [SerializeField] private bool requireTutorialGate = true;
    private bool _tutorialGateOpened = false;

    [Header("Global scene rules")]
    [SerializeField] private int firstSceneBuildIndex = 1;
    [SerializeField] private int stage3BuildIndex = 3;

    // ✅ הוספה: איזה שלב מדכא את הפופאפ היומי
    [Header("Per-scene suppression")]
    [SerializeField] private int suppressDailyPopupBuildIndex = 4; // Level 4
    private bool _suppressDailyThisScene; // דגל ריצה לסצנה הנוכחית

    [Header("Optional refs")]
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private SugarMeter  sugarMeter;

    [System.Serializable]
    public class UIPopup
    {
        public string id = "popup";
        [Header("UI")] public GameObject panel;
        public Animator animator;
        [Header("Behaviour")]
        public bool lockPlayerInput = true;
        public bool pauseTimerOnlyInFirstScene = true;
        public bool pauseHearts = true;
        public bool showOnceGlobally = false;
        [HideInInspector] public bool shownOnce;
    }

    [Header("Popups")]
    [Tooltip("פופאפ יומי ב-07:00")]
    public UIPopup dailyPopup;

    [Tooltip("פופאפ שיקפוץ כשמגיעים לשלב 3 ומעלה")]
    public UIPopup stage3Popup;

    // --- Runtime state ---
    private UIPopup current;
    private bool pausedTimerByMe;
    private bool pausedHeartsByMe;
    private float? savedBaselineRate;
    private static bool s_didGlobalPauseOnce = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        HideAll();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (Timer.Instance != null) Timer.Instance.OnDailyAlarm += OnDailyAlarm;
        else StartCoroutine(WaitForTimerThenSubscribe());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Timer.Instance != null) Timer.Instance.OnDailyAlarm -= OnDailyAlarm;
    }

    private IEnumerator WaitForTimerThenSubscribe()
    {
        while (Timer.Instance == null) yield return null;
        Timer.Instance.OnDailyAlarm += OnDailyAlarm;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // עדכון דגל ההשתקה לפי הסצנה הנוכחית
        _suppressDailyThisScene = (scene.buildIndex == suppressDailyPopupBuildIndex);

        // אם נכנסנו לשלב 4 עם פופאפ פתוח – לסגור אותו
        ForceCloseCurrentWithoutRestore();

        TryShowStage3IfNeeded();
    }

    private bool IsFirstScene() =>
        SceneManager.GetActiveScene().buildIndex == firstSceneBuildIndex;

    private void TryShowStage3IfNeeded()
    {
        if (SceneManager.GetActiveScene().buildIndex == stage3BuildIndex)
            TryShow(stage3Popup);
    }
    
    public void TryShow(UIPopup popup)
    {
        if (popup == null || popup.panel == null) return;
        if (current != null) return;
        if (popup.showOnceGlobally && popup.shownOnce) return;

        if (playerMover == null) playerMover = FindObjectOfType<PlayerMover>();
        if (sugarMeter  == null) sugarMeter  = SugarMeter.Instance ?? FindObjectOfType<SugarMeter>();

        current = popup;
        current.shownOnce = true;

        popup.panel.SetActive(true);
        if (popup.animator) popup.animator.SetTrigger("Show");
        if (popup.lockPlayerInput && playerMover != null)
            playerMover.SetInputLocked(true);

        bool doGlobalPause =
            popup.pauseTimerOnlyInFirstScene && IsFirstScene() && !s_didGlobalPauseOnce;

        if (doGlobalPause && Timer.Instance != null)
        {
            Timer.Instance.PauseClock(true);
            pausedTimerByMe = true;
            s_didGlobalPauseOnce = true;
        }

        if (doGlobalPause && popup.pauseHearts && sugarMeter != null)
        {
            savedBaselineRate = sugarMeter.sugarDecreaseRate;
            sugarMeter.sugarDecreaseRate = 0f;
            sugarMeter.SetHeartsPaused(true, resetProgress: true);
            pausedHeartsByMe = true;
        }
    }
    
    public void CloseCurrent()
    {
        if (current == null) return;

        if (current.animator) current.animator.SetTrigger("Hide");
        if (current.panel)    current.panel.SetActive(false);

        if (current.lockPlayerInput && playerMover != null)
            playerMover.SetInputLocked(false);

        TryUnpauseAndRestore();
        current = null;
    }
    
    private void ForceCloseCurrentWithoutRestore()
    {
        if (current == null) return;

        if (current.panel) current.panel.SetActive(false);
        if (current.lockPlayerInput && playerMover != null)
            playerMover.SetInputLocked(false);
        
        TryUnpauseAndRestore();
        current = null;
    }

    private void TryUnpauseAndRestore()
    {
        if (pausedTimerByMe && Timer.Instance != null)
        {
            Timer.Instance.PauseClock(false);
            pausedTimerByMe = false;
        }

        if (pausedHeartsByMe && sugarMeter != null)
        {
            sugarMeter.SetHeartsPaused(false);
            pausedHeartsByMe = false;
        }

        if (savedBaselineRate.HasValue && sugarMeter != null)
        {
            sugarMeter.sugarDecreaseRate = savedBaselineRate.Value;
            savedBaselineRate = null;
        }
    }

    private void HideAll()
    {
        if (dailyPopup  != null && dailyPopup.panel  != null) dailyPopup.panel.SetActive(false);
        if (stage3Popup != null && stage3Popup.panel != null) stage3Popup.panel.SetActive(false);
    }

    public void ResetRuntimeState()
    {
        s_didGlobalPauseOnce = false;
        if (dailyPopup  != null) dailyPopup.shownOnce  = false;
        if (stage3Popup != null) stage3Popup.shownOnce = false;
        ForceCloseCurrentWithoutRestore();
    }
    
    public void ShowDailyFromTutorialGate()
    {
        _tutorialGateOpened = true;
        TryShowDailyIfAllowed();
    }
    
    private void TryShowDailyIfAllowed()
    {
        // דיכוי בשלב מסוים?
        if (_suppressDailyThisScene) return;

        // חייבים שהשער יהיה פתוח (אם נדרש)
        if (requireTutorialGate && !_tutorialGateOpened) return;

        // רק בסצנה הראשונה
        if (!IsFirstScene()) return;

        TryShow(dailyPopup);
    }
    
    private void OnDailyAlarm(long dayCount)
    {
        TryShowDailyIfAllowed();
    }
}
