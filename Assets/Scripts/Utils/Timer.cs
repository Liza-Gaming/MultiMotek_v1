using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Image background;

    [Header("Default Time (seconds)")]
    [SerializeField] private float defaultTime = 300f;

    private float remainingTime;
    private bool timerIsActive = false;
    private float initialTime;

    public static Timer Instance { get; private set; }
    
    private readonly System.Collections.Generic.Dictionary<string, float> levelTimes =
        new System.Collections.Generic.Dictionary<string, float>()
        {
            { "SampleScene", 180f },
            { "Level2", 300f },
        };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (levelTimes.ContainsKey(scene.name))
        {
            SetInitialTime(levelTimes[scene.name]);
            ShowTimerUI(true);
        }
        else if (scene.name == "Intro")
        {
            timerIsActive = false;
            ShowTimerUI(false);
            Destroy(gameObject);
        }
        else
        {
            timerIsActive = false;
            ShowTimerUI(false);
        }
    }

    public void SetInitialTime(float newTime)
    {
        initialTime = newTime;
        remainingTime = newTime;
        timerIsActive = true;
        UpdateTimerUI();
    }

    void Update()
    {
        if (timerIsActive && remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime < 0) remainingTime = 0;
            UpdateTimerUI();
        }

        if (remainingTime == 0 && timerIsActive)
        {
            timerIsActive = false;
            SceneManager.LoadScene("Intro");
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ShowTimerUI(bool show)
    {
        timerText.gameObject.SetActive(show);
        background.gameObject.SetActive(show);
    }

    public float GetElapsedTime()
    {
        return initialTime - remainingTime;
    }

    public void ResetTimer()
    {
        remainingTime = initialTime;
        timerIsActive = true;
        UpdateTimerUI();
    }
}
