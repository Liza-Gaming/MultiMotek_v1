using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class LevelMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject storyPanel;   // פאנל סיפור פתיחה (רשות)
    [SerializeField] private GameObject levelsPanel;  // הגריד של כפתורי השלבים

    [Header("Level Buttons (Grid)")]
    [SerializeField] private Button[] levelButtons;   // כפתורי שלבים בתפריט

    [Header("Per-Level Confirm Panels")]
    [SerializeField] private LevelPanelEntry[] levelConfirmPanels;

    [Header("Story Button")]
    [SerializeField] private Button continueStoryButton;  // כפתור 'המשך' בפאנל סיפור (אם יש)

    [Header("Fade Settings")]
    [SerializeField, Tooltip("משך הפייד-אין של פאנל האישור")]
    private float confirmFadeInDuration = 0.25f;

    [SerializeField, Tooltip("משך הפייד-אאוט לפני מעבר לשלב")]
    private float confirmFadeOutDuration = 0.25f;

    private int _pendingLevelId = -1; // 1-based level id
    private int _openPanelIndex = -1;
    private bool _isFading = false;

    private void Start()
    {
        if (storyPanel) storyPanel.SetActive(true);
        if (levelsPanel) levelsPanel.SetActive(false);

        if (continueStoryButton)
            continueStoryButton.onClick.AddListener(OnContinueStory);

        // לוודא שלכל פאנל יש CanvasGroup ולהתחיל סגור/שקוף
        PreparePanels();

        InitLevelButtons();
        WireLevelButtons();
        WireConfirmPanels();
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
            levelConfirmPanels[i].canvasGroup = cg; // לשמור רפרנס חזרה במבנה
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
            int idx = i; // לכידת אינדקס
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

    private void OnLevelButtonClicked(int buttonIndexZeroBased)
    {
        if (_isFading) return;

        if (buttonIndexZeroBased < 0 || buttonIndexZeroBased >= levelConfirmPanels.Length)
        {
            Debug.LogWarning($"No confirm panel configured for level index {buttonIndexZeroBased}");
            return;
        }

        _pendingLevelId = buttonIndexZeroBased + 1;

        // לסגור כל פאנל פתוח אחר
        CloseOpenPanelImmediate();

        // לפתוח את הפאנל המתאים בפייד-אין
        var entry = levelConfirmPanels[buttonIndexZeroBased];
        if (!entry.panel) return;

        _openPanelIndex = buttonIndexZeroBased;
        entry.panel.SetActive(true);
        StartCoroutine(FadeCanvasGroup(entry.canvasGroup, 0f, 1f, confirmFadeInDuration, after: () =>
        {
            entry.canvasGroup.interactable = true;
            entry.canvasGroup.blocksRaycasts = true;
        }));
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

        // לנעול אינטראקציה ולבצע פייד-אאוט ואז מעבר
        var entry = levelConfirmPanels[panelIndex];
        if (entry.canvasGroup)
        {
            entry.canvasGroup.interactable = false;
            entry.canvasGroup.blocksRaycasts = false;

            StartCoroutine(FadeCanvasGroup(entry.canvasGroup, 1f, 0f, confirmFadeOutDuration, after: () =>
            {
                // לכבות את הפאנל ואז לטעון את הסצנה
                if (entry.panel) entry.panel.SetActive(false);
                AppFlow.StartStandalone(levelName, this);
            }));
        }
        else
        {
            // גיבוי: אם משום מה אין CanvasGroup, נטען מייד
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
    }
}
