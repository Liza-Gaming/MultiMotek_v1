using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class LevelStoryGallery : MonoBehaviour
{
    [SerializeField] private GameObject galleryPanel; // הפאנל שמכיל את התמונה והכפתור
    [SerializeField] private Image storyImageDisplay;  // רכיב התמונה שמציג את התמונות
    [SerializeField] private Button nextButton;        // כפתור "הבא" או "המשך"

    private Sprite[] _currentImages;      // מערך התמונות הנוכחי להצגה
    private int _currentIndex;            // האינדקס של התמונה הנוכחית
    private Action _onFinishedCallback;   // הפעולה שתופעל בסיום הגלריה
    
    private void Awake()
    {
        // הסתר את הפאנל בהתחלה וחבר את הכפתור
        if (galleryPanel) galleryPanel.SetActive(false);
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);
    }

    /// <summary>
    /// מתחיל את הגלריה עם רשימת תמונות ופעולת סיום
    /// </summary>
    public void StartGallery(Sprite[] images, Action onFinished)
    {
        if (images == null || images.Length == 0)
        {
            // אם אין תמונות, פשוט הפעל את הקולבק וסיים
            onFinished?.Invoke();
            return;
        }

        _currentImages = images;
        _onFinishedCallback = onFinished;
        _currentIndex = 0;

        UpdateImage();
        galleryPanel.SetActive(true);
    }
    
    private void OnNextClicked()
    {
        _currentIndex++;

        if (_currentIndex < _currentImages.Length)
        {
            UpdateImage();
        }
        else
        {

            var callback = _onFinishedCallback;

            HidePanel();

            callback?.Invoke();
            
        }
    }
    
    private void UpdateImage()
    {
        if (storyImageDisplay && _currentIndex < _currentImages.Length)
        {
            storyImageDisplay.sprite = _currentImages[_currentIndex];
        }
    }
    
    public void HidePanel()
    {
        if (galleryPanel) galleryPanel.SetActive(false);
        _onFinishedCallback = null;
        _currentImages = null;
    }
}