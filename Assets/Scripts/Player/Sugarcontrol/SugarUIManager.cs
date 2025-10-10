using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SugarUIManager : MonoBehaviour
{
    public GameObject sugarPanel;
    public GameObject sugarCheckButton;
    public float displayDuration = 3f;

    private Coroutine hideRoutine;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        RefreshForCurrentScene();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void Start()
    {
        RefreshForCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForCurrentScene();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        RefreshForCurrentScene();
    }

    private void RefreshForCurrentScene()
    {
        int idx = SceneManager.GetActiveScene().buildIndex;
        bool alwaysVisible = idx >= 3;
        
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        sugarPanel.SetActive(alwaysVisible);
        sugarCheckButton.SetActive(!alwaysVisible);
        
        if (!alwaysVisible)
        {

        }
    }

    public void OnSugarCheckButtonClicked()
    {

        if (SceneManager.GetActiveScene().buildIndex >= 3)
            return;

        sugarPanel.SetActive(true);
        sugarCheckButton.SetActive(false);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideSugarPanelAfterDelay());
    }

    private IEnumerator HideSugarPanelAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        
        if (SceneManager.GetActiveScene().buildIndex < 3)
        {
            sugarPanel.SetActive(false);
            sugarCheckButton.SetActive(true);
        }

        hideRoutine = null;
    }
}
