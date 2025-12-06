using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class LevelMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject levelsPanel;

    [Header("Level Buttons (Grid)")]
    [SerializeField] private Button[] levelButtons;   // כפתורי שלבים בתפריט

    [Header("Per-Level Confirm Panels")]
    [SerializeField] private LevelPanelEntry[] levelConfirmPanels;

    [Header("Story Button")]
    [SerializeField] private Button continueStoryButton;

    [Header("Fade Settings")]
    private float confirmFadeInDuration = 0.25f;
    
    private float confirmFadeOutDuration = 0.25f;
    
    [Header("Story Gallery")]
    [SerializeField] private LevelStoryGallery storyGallery;
    
    [SerializeField] private DisclaimerManager disclaimerManager;

    private int _pendingLevelId = -1; // 1-based level id
    private int _openPanelIndex = -1;
    private bool _isFading = false;
    
    [Header("Audio per slide")]
    [SerializeField] private AudioSource narrationSource;

    [SerializeField] private AudioClip clip;

    private void Start()
    {
        if (levelsPanel) levelsPanel.SetActive(false);
        
        if (storyGallery != null) storyGallery.HidePanel();

        if (continueStoryButton)
            continueStoryButton.onClick.AddListener(OnContinueStory);
        
        PreparePanels();
        narrationSource.clip = clip;
        InitLevelButtons();
        WireLevelButtons();
        WireConfirmPanels();
        
        if (disclaimerManager != null)
        {
            // קרא למנהל הדיסקליימר.
            // הפונקציה ShowInitialStoryPanel תופעל רק בסיום.
            disclaimerManager.CheckDisclaimer(ShowInitialStoryPanel);
        }
        else
        {
            // אם אין מנהל, פשוט הצג את הפאנל כרגיל.
            Debug.LogWarning("DisclaimerManager not assigned in LevelMenu!");
            ShowInitialStoryPanel();
        }
    }
    
    
    /// <summary>
    /// פונקציה זו נקראת *אחרי* שהדיסקליימר טופל.
    /// היא מציגה את פאנל הסיפור הראשון.
    /// </summary>
    private void ShowInitialStoryPanel()
    {
        narrationSource.Play();
        if (storyPanel) storyPanel.SetActive(true);
    }
    

    private void PreparePanels()
    {
        for (int i = 0; i < levelConfirmPanels.Length; i++)
        {
            var entry = levelConfirmPanels[i];
            if (!entry.panel) continue;

            var cg = entry.panel.GetComponent<CanvasGroup>();
            if (!cg) cg = entry.panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            entry.panel.SetActive(false);
            levelConfirmPanels[i].canvasGroup = cg;
        }
    }

    private void InitLevelButtons()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i])
                levelButtons[i].interactable = (i < unlockedLevel);
        }
    }

    private void WireLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            if (levelButtons[i] != null)
            {
                levelButtons[i].onClick.RemoveAllListeners();
                levelButtons[i].onClick.AddListener(() => OnLevelButtonClicked(idx));
            }
        }
    }

    private void WireConfirmPanels()
    {
        for (int i = 0; i < levelConfirmPanels.Length; i++)
        {
            int idx = i;
            var entry = levelConfirmPanels[i];

            if (entry.continueButton)
            {
                entry.continueButton.onClick.RemoveAllListeners();
                entry.continueButton.onClick.AddListener(() => OnConfirmContinue(idx));
            }
        }
    }
    

    public void OnContinueStory()
    {
        if (storyPanel) storyPanel.SetActive(false);
        if (levelsPanel) levelsPanel.SetActive(true);
    }
    
    private void ShowConfirmPanel(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= levelConfirmPanels.Length) return;

        var entry = levelConfirmPanels[panelIndex];
        if (!entry.panel) return;

        _openPanelIndex = panelIndex;
        entry.panel.SetActive(true);
        StartCoroutine(FadeCanvasGroup(entry.canvasGroup, 0f, 1f, confirmFadeInDuration, after: () =>
        {
            entry.canvasGroup.interactable = true;
            entry.canvasGroup.blocksRaycasts = true;
        }));
    }
    
    
    private void OnLevelButtonClicked(int buttonIndexZeroBased)
    {
        if (_isFading) return;

        if (buttonIndexZeroBased < 0 || buttonIndexZeroBased >= levelConfirmPanels.Length)
        {
            Debug.LogWarning($"No confirm panel configured for level index {buttonIndexZeroBased}");
            return;
        }

        _pendingLevelId = buttonIndexZeroBased + 1;
        
        CloseOpenPanelImmediate();

        var entry = levelConfirmPanels[buttonIndexZeroBased];

        if (entry.showStoryGalleryFirst && entry.storyImages != null && entry.storyImages.Length > 0 && storyGallery != null)
        {

            storyGallery.StartGallery(entry.storyImages, () => {
                ShowConfirmPanel(buttonIndexZeroBased);
            });
        }
        else
        {
            ShowConfirmPanel(buttonIndexZeroBased);
        }
    }

    private void OnConfirmContinue(int panelIndex)
    {
        if (_isFading) return;

        if (_pendingLevelId < 1)
        {
            Debug.LogWarning("No pending level selected.");
            return;
        }

        if (panelIndex < 0 || panelIndex >= levelConfirmPanels.Length)
        {
            Debug.LogWarning("Confirm panel index out of range.");
            return;
        }

        string levelName = "Level " + _pendingLevelId;
        
        var entry = levelConfirmPanels[panelIndex];
        if (entry.canvasGroup)
        {
            entry.canvasGroup.interactable = false;
            entry.canvasGroup.blocksRaycasts = false;

            StartCoroutine(FadeCanvasGroup(entry.canvasGroup, 1f, 0f, confirmFadeOutDuration, after: () =>
            {

                if (entry.panel) entry.panel.SetActive(false);
                AppFlow.StartStandalone(levelName, this);
            }));
        }
        else
        {

            AppFlow.StartStandalone(levelName, this);
        }
    }

    private void CloseOpenPanelImmediate()
    {
        if (_openPanelIndex < 0) return;

        var prev = levelConfirmPanels[_openPanelIndex];
        if (prev.canvasGroup)
        {
            prev.canvasGroup.alpha = 0f;
            prev.canvasGroup.interactable = false;
            prev.canvasGroup.blocksRaycasts = false;
        }
        if (prev.panel) prev.panel.SetActive(false);

        _openPanelIndex = -1;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, Action after = null)
    {
        _isFading = true;
        float t = 0f;
        cg.alpha = from;

        // אם מתחילים פייד-אין—להבטיח שהאובייקט פעיל
        if (!cg.gameObject.activeSelf) cg.gameObject.SetActive(true);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // שלא יושפע מפאוז אם יש
            float k = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        cg.alpha = to;
        _isFading = false;
        after?.Invoke();
    }

    private void HideAllLevelConfirmPanels()
    {
        foreach (var entry in levelConfirmPanels)
        {
            if (!entry.panel) continue;
            if (!entry.canvasGroup)
            {
                var cg = entry.panel.GetComponent<CanvasGroup>();
                if (!cg) cg = entry.panel.AddComponent<CanvasGroup>();
                entry.canvasGroup = cg;
            }
            entry.canvasGroup.alpha = 0f;
            entry.canvasGroup.interactable = false;
            entry.canvasGroup.blocksRaycasts = false;
            entry.panel.SetActive(false);
        }
        _openPanelIndex = -1;
    }

    [Serializable]
    private class LevelPanelEntry
    {
        public GameObject panel;
        public Button continueButton;
        [HideInInspector] public CanvasGroup canvasGroup;
        
        [Header("Optional Story Gallery")]
        public bool showStoryGalleryFirst = false;
        public Sprite[] storyImages;
    }
}
