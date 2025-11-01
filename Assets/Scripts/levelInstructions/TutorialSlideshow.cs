using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialSlideshow : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Image slideImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Slides")]
    [SerializeField] private List<Sprite> slides = new List<Sprite>();

    [Header("Behavior")]
    [SerializeField] private bool closeOnLastNext = true;
    [SerializeField] private bool showOnce = true;
    [SerializeField] private string playerPrefsKey = "Level1_TutorialShown";

    // --- NEW ---
    [Header("Timer policy")]
    [Tooltip("לעצור את הטיימר בזמן שההוראות פתוחות")]
    [SerializeField] private bool pauseTimerWhileOpen = true;
    private bool pausedTimerByMe = false;

    // --- Character bubble (כמו שהיה) ---
    [Header("Guide Character (alternates every slide)")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite girlSprite;
    [SerializeField] private Sprite boySprite;
    [SerializeField] private bool startWithGirl = true;
    [SerializeField] private bool alternateByIndex = true;

    [SerializeField] private bool pauseOnlyInFirstScene = true;
    [SerializeField] private int firstSceneBuildIndex = 1;
    private int index = 0;
    
    private PlayerMover playerMover;

    private void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(OnPrev);
        if (nextButton) nextButton.onClick.AddListener(OnNext);
        if (!rootPanel) rootPanel = gameObject;
    }

    private void Start()
    {
        if (playerMover == null)
            playerMover = FindObjectOfType<PlayerMover>();
        
        if (playerMover) playerMover.SetInputLocked(true);
        
        if (showOnce && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
        {
            if (rootPanel) rootPanel.SetActive(false);
            enabled = false;
            return;
        }

        if (slides == null || slides.Count == 0 || slideImage == null)
        {
            Debug.LogWarning("[TutorialSlideshow] No slides or slideImage assigned");
            if (rootPanel) rootPanel.SetActive(false);
            enabled = false;
            return;
        }

        index = 0;
        if (rootPanel) rootPanel.SetActive(true);
        
        TryPauseTimer();

        ApplySlide();
        ApplyCharacter();
        UpdateButtons();
    }

    private void OnDisable()
    {
        TryUnpauseTimer();
    }

    private void OnPrev()
    {
        if (index > 0)
        {
            index--;
            ApplySlide();
            ApplyCharacter();
            UpdateButtons();
        }
    }

    private void OnNext()
    {
        if (index < slides.Count - 1)
        {
            index++;
            ApplySlide();
            ApplyCharacter();
            UpdateButtons();
        }
        else
        {
            if (SceneManager.GetActiveScene().buildIndex == 1)
            {
                if (PopupManager.Instance != null)
                    PopupManager.Instance.ShowDailyFromTutorialGate();
            }

            if (closeOnLastNext)
                ClosePanel();
        }
    }

    private void ApplySlide()
    {
        if (slideImage && index >= 0 && index < slides.Count)
        {
            slideImage.sprite = slides[index];
        }
    }

    private void ApplyCharacter()
    {
        if (!characterImage) return;
        if (!alternateByIndex)
        {
            characterImage.sprite = startWithGirl ? girlSprite : boySprite;
            characterImage.enabled = characterImage.sprite != null;
            return;
        }

        bool showGirl = (index % 2 == 0) == startWithGirl;
        characterImage.sprite = showGirl ? girlSprite : boySprite;
        characterImage.enabled = characterImage.sprite != null;
    }

    private void UpdateButtons()
    {
        if (prevButton) prevButton.interactable = index > 0;
        if (nextButton) nextButton.interactable = true;
    }

    private void ClosePanel()
    {
        if (showOnce) PlayerPrefs.SetInt(playerPrefsKey, 1);
        if (rootPanel) rootPanel.SetActive(false);

        // --- NEW: resume timer when closing ---
        playerMover.SetInputLocked(false);
        TryUnpauseTimer();
    }

    private void Update()
    {
        if (!rootPanel || !rootPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) OnNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow))  OnPrev();
        if (Input.GetKeyDown(KeyCode.Escape))     ClosePanel();
    }
    
    private void TryPauseTimer()
    {
        if (!pauseTimerWhileOpen) return;
        if (pauseOnlyInFirstScene && SceneManager.GetActiveScene().buildIndex != firstSceneBuildIndex) return;

        if (Timer.Instance != null && !pausedTimerByMe)
        {
            Timer.Instance.PauseClock(true);
            pausedTimerByMe = true;
        }
    }

    private void TryUnpauseTimer()
    {
        if (!pauseTimerWhileOpen) return;
        if (pausedTimerByMe && Timer.Instance != null)
        {
            Timer.Instance.PauseClock(false);
            pausedTimerByMe = false;
        }
    }
}
