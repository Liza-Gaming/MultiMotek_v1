using UnityEngine;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private GameObject levelPanel;

    [Header("Level Buttons")]
    [SerializeField] private Button[] buttons;

    [Header("Story Button")]
    [SerializeField] private Button continueButton;

    private void Start()
    {

        if (storyPanel) storyPanel.SetActive(true);
        if (levelPanel) levelPanel.SetActive(false);
        
        if (continueButton)
            continueButton.onClick.AddListener(OnContinueStory);
        
        InitLevelButtons();
    }

    private void InitLevelButtons()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i])
                buttons[i].interactable = (i < unlockedLevel);
        }
    }

    public void OnContinueStory()
    {
        if (storyPanel) storyPanel.SetActive(false);
        if (levelPanel) levelPanel.SetActive(true);
    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level " + levelId;
        AppFlow.StartStandalone(levelName, this);
    }

    public void CloseLevel()
    {
        levelPanel.SetActive(false);
    }
}