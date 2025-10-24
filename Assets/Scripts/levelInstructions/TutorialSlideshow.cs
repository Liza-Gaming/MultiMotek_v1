using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // 1. להוסיף את זה

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
    [Tooltip("כאשר לוחצים 'ימינה' על השקופית האחרונה – לסגור את הפאנל")]
    [SerializeField] private bool closeOnLastNext = true;

    [Tooltip("להציג רק פעם אחת בשלב הזה (נשמר ב-PlayerPrefs)")]
    [SerializeField] private bool showOnce = true;

    [Tooltip("מפתח לשמירה אם כבר הוצג (החליפי לפי שם השלב)")]
    [SerializeField] private string playerPrefsKey = "Level1_TutorialShown";

    // 2. להוסיף את האירוע הזה
    [Header("Events")]
    [Tooltip("אירוע שמופעל כאשר ההדרכה נסגרת")]
    [SerializeField] private UnityEvent onTutorialClosed;

    private int index = 0;

    // ... (כל הקוד של Awake נשאר זהה) ...

    private void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(OnPrev);
        if (nextButton) nextButton.onClick.AddListener(OnNext);

        if (!rootPanel) rootPanel = gameObject;
    }

    private void Start()
    {
        // להציג פעם אחת בלבד?
        if (showOnce && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
        {
            if (rootPanel) rootPanel.SetActive(false);
            enabled = false;
            return;
        }

        // להבטיח שיש מה להציג
        if (slides == null || slides.Count == 0 || slideImage == null)
        {
            Debug.LogWarning("[TutorialSlideshow] No slides or slideImage assigned");
            if (rootPanel) rootPanel.SetActive(false);
            enabled = false;
            return;
        }

        index = 0;
        if (rootPanel) rootPanel.SetActive(true);
        ApplySlide();
        UpdateButtons();
    }
    
    // ... (OnPrev, OnNext, ApplySlide, UpdateButtons נשארים זהים) ...

    private void OnPrev()
    {
        if (index > 0)
        {
            index--;
            ApplySlide();
            UpdateButtons();
        }
    }

    private void OnNext()
    {
        if (index < slides.Count - 1)
        {
            index++;
            ApplySlide();
            UpdateButtons();
        }
        else
        {
            if (closeOnLastNext)
            {
                ClosePanel();
            }
        }
    }

    private void ApplySlide()
    {
        if (slideImage && index >= 0 && index < slides.Count)
        {
            slideImage.sprite = slides[index];
        }
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

        // 3. להפעיל את האירוע בסגירה
        onTutorialClosed?.Invoke();
    }
    
    private void Update()
    {
        if (!rootPanel || !rootPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) OnNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow))  OnPrev();
        if (Input.GetKeyDown(KeyCode.Escape))     ClosePanel();
    }

    // 4. להוסיף את הפונקציה הציבורית הזו
    // היא בודקת אם ההדרכה פעילה כרגע או עומדת להיות פעילה
    public bool IsPendingOrActive()
    {
        // בדיקה אם היא כבר פעילה
        if (rootPanel != null && rootPanel.activeSelf) return true;

        // בדיקה אם היא *עומדת* להיות פעילה (לפי הלוגיקה ב-Start)
        if (showOnce && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
        {
            return false; // כבר הוצגה, לא תהיה פעילה
        }

        if (slides == null || slides.Count == 0 || slideImage == null)
        {
            return false; // לא מוגדרת, לא תהיה פעילה
        }
        
        // אם עברנו את הבדיקות, היא כנראה עומדת להיות פעילה
        return true;
    }
}