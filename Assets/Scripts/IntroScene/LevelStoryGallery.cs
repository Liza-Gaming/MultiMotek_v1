using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class LevelStoryGallery : MonoBehaviour
{
    [SerializeField] private GameObject galleryPanel;
    [SerializeField] private Image storyImageDisplay;
    [SerializeField] private Button nextButton;

    private Sprite[] _currentImages;
    private int _currentIndex;
    private Action _onFinishedCallback;
    
    private void Awake()
    {
        if (galleryPanel) galleryPanel.SetActive(false);
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);
    }

    public void StartGallery(Sprite[] images, Action onFinished)
    {
        if (images == null || images.Length == 0)
        {
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