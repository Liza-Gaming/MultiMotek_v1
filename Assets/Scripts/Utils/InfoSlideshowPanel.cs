using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class InfoSlideshowPanel : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("האובייקט הראשי של פאנל הספר שיוסתר ויוצג")]
    [SerializeField] private GameObject rootPanel; 
    [Tooltip("רכיב ה-Image שיחליף תמונה (כל עמוד בספר)")]
    [SerializeField] private Image slideImage;  
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Slides")]
    [Tooltip("רשימת כל העמודים (Sprites) שיוצגו בספר")]
    [SerializeField] private List<Sprite> slides = new List<Sprite>(); 

    [Header("Behavior")]
    [Tooltip("האם לחיצה על 'הבא' בעמוד האחרון תסגור את הפאנל?")]
    [SerializeField] private bool closeOnLastNext = false; 
    
    private int index = 0;
    private Pause pauseManager;
    private PlayerMover playerMover;
    

    private void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(OnPrev);
        if (nextButton) nextButton.onClick.AddListener(OnNext);
        if (!rootPanel) rootPanel = gameObject;
        
        pauseManager = FindObjectOfType<Pause>();
        playerMover = FindObjectOfType<PlayerMover>(); // מצאנו מהסקריפט הקודם

        if (pauseManager == null)
        {
            Debug.LogError("InfoSlideshowPanel: לא נמצא סקריפט Pause בסצנה!");
        }
    }

    private void Start()
    {
        if (rootPanel) rootPanel.SetActive(false);
        
        if (slides == null || slides.Count == 0 || slideImage == null)
        {
            Debug.LogWarning("[InfoSlideshowPanel] לא הוגדרו תמונות (slides) או רכיב slideImage");
            enabled = false;
        }
    }
    
    
    public void OnClick()
    {
        if (!rootPanel || !pauseManager) return;
        
        bool willBeActive = !rootPanel.activeSelf;
        
        if (willBeActive)
        {
            OpenPanel();
        }
        else
        {
            ClosePanel();
        }
    }
    
    public void Close()
    {
        if (rootPanel.activeSelf)
        {
            ClosePanel();
        }
    }
    

    private void OpenPanel()
    {
        if (!rootPanel || !pauseManager) return;
        
        index = 0; 
        
        rootPanel.SetActive(true);
        pauseManager.SoftPauseFor("InfoBook");
        if (playerMover) playerMover.SetInputLocked(true);

        ApplySlide();
        UpdateButtons();
    }

    private void ClosePanel()
    {
        if (!rootPanel || !pauseManager) return;

        rootPanel.SetActive(false);
        pauseManager.SoftResumeFor("InfoBook");
        if (playerMover) playerMover.SetInputLocked(false);
    }
    

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
        
        if (nextButton && !closeOnLastNext)
        {
             nextButton.interactable = index < slides.Count - 1;
        }
        else if (nextButton && closeOnLastNext)
        {
            nextButton.interactable = true; 
        }
    }
    
    private void Update()
    {
        if (!rootPanel || !rootPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) OnNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow))  OnPrev();
        if (Input.GetKeyDown(KeyCode.Escape))     ClosePanel();
    }
}