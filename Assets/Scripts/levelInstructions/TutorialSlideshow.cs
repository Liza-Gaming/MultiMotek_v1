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

    [Header("Timer policy")]
    [SerializeField] private bool pauseTimerWhileOpen = true;
    private bool pausedTimerByMe = false;

    [Header("Guide Character")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite girlSprite;
    [SerializeField] private Sprite boySprite;
    [SerializeField] private bool startWithGirl = true;
    [SerializeField] private bool alternateByIndex = true;

    [SerializeField] private bool pauseOnlyInFirstScene = true;
    [SerializeField] private int firstSceneBuildIndex = 1;
    private int index = 0;
    
    private PlayerMover playerMover;
    
    [Header("Audio (SFX Source)")]
    [Tooltip("הווליום של הקריינות")]
    [SerializeField, Range(0f, 1f)] private float narrationVolume = 1f;
    [SerializeField] private List<AudioClip> slideClips = new List<AudioClip>();
    
    private AudioSource playerSFXSource;

    private void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(OnPrev);
        if (nextButton) nextButton.onClick.AddListener(OnNext);
        if (!rootPanel) rootPanel = gameObject;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerSFXSource = player.GetComponent<AudioSource>();
        }

        if (playerMover == null)
            playerMover = FindObjectOfType<PlayerMover>();

        if (playerMover) 
            playerMover.SetInputLocked(true);

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
        
        if (MusicManager.Instance != null) MusicManager.Instance.SetTutorialMode(true);
        
        PauseGame(true);

        ApplySlide();
        ApplyCharacter();
        PlaySlideAudio();
        UpdateButtons();
    }
    

    private void OnPrev()
    {
        if (index > 0)
        {
            index--;
            ApplySlide();
            ApplyCharacter();
            PlaySlideAudio();
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
            PlaySlideAudio();
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

    private void ClosePanel()
    {
        if (showOnce) PlayerPrefs.SetInt(playerPrefsKey, 1);
        if (rootPanel) rootPanel.SetActive(false);
        
        if (playerSFXSource != null) playerSFXSource.Stop();

        if (playerMover)
            playerMover.SetInputLocked(false);
        
        if (MusicManager.Instance != null) MusicManager.Instance.SetTutorialMode(false);
        
        PauseGame(false);
    }

    private void PlaySlideAudio()
    {
        if (playerSFXSource == null || slideClips == null || index >= slideClips.Count)
        {
            if (playerSFXSource != null) playerSFXSource.Stop();
            return;
        }

        AudioClip clip = slideClips[index];
        
        if (clip == null)
        {
            playerSFXSource.Stop();
            return;
        }
        
        playerSFXSource.Stop();
        playerSFXSource.clip = clip;
        playerSFXSource.volume = narrationVolume;
        playerSFXSource.Play();
    }
    
    private void PauseGame(bool pause)
    {
        if (Timer.Instance != null) Timer.Instance.PauseClock(pause);
        if (SugarMeter.Instance != null) SugarMeter.Instance.SetSimulationPaused(pause);
    }

    private void ApplySlide()
    {
        if (slideImage && index >= 0 && index < slides.Count)
            slideImage.sprite = slides[index];
    }

    private void ApplyCharacter()
    {
        if (!characterImage) return;
        if (!alternateByIndex)
        {
            characterImage.sprite = startWithGirl ? girlSprite : boySprite;
            return;
        }
        bool showGirl = (index % 2 == 0) == startWithGirl;
        characterImage.sprite = showGirl ? girlSprite : boySprite;
    }

    private void UpdateButtons()
    {
        if (prevButton) prevButton.interactable = index > 0;
    }

    private void Update()
    {
        if (!rootPanel || !rootPanel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.RightArrow)) OnNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow))  OnPrev();
        if (Input.GetKeyDown(KeyCode.Escape))     ClosePanel();
    }

    private void OnDisable() => TryUnpauseTimer();

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
        if (pausedTimerByMe && Timer.Instance != null)
        {
            Timer.Instance.PauseClock(false);
            pausedTimerByMe = false;
        }
    }
}