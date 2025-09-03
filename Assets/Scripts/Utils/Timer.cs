using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public static Timer Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Text  clockText;
    [SerializeField] private Image background;

    [Header("Clock Settings")]
    [SerializeField] private float gameSecondsPerRealSecond = 30f;
    [SerializeField] private bool  autoStart = true;
    [SerializeField] private bool  stopWhenPaused = true;

    [Header("Start Time (HH:MM)")]
    [Range(0, 23)] [SerializeField] private int startHour = 0;
    [Range(0, 59)] [SerializeField] private int startMinute = 0;

    [Header("Scene-load guards")]
    [SerializeField] private float maxRealDeltaClamp = 0.25f;
    [SerializeField] private float newDayGraceAfterLoad = 0.2f;
    private int   skipFramesAfterLoad = 0; 
    private float suppressNewDayUntilUnscaled = -1f;

    private const float SecondsPerDay = 24f * 60f * 60f;
    private float secondsSinceMidnight;
    private bool  isRunning;
    private long  dayCount;

    public event Action<long> OnNewDay;


    private static bool  s_hasSaved;
    private static float s_savedSeconds;
    private static long  s_savedDays;
    private static float s_savedRate;
    private static bool  s_savedRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        
        if (transform.parent != null) transform.SetParent(null, worldPositionStays: true);

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (s_hasSaved)
        {
            secondsSinceMidnight     = s_savedSeconds;
            dayCount                 = s_savedDays;
            gameSecondsPerRealSecond = s_savedRate;
            isRunning                = s_savedRunning;
        }
        else
        {
            secondsSinceMidnight = (startHour * 60f + startMinute) * 60f;
            isRunning = autoStart;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateClockUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            s_hasSaved     = true;
            s_savedSeconds = secondsSinceMidnight;
            s_savedDays    = dayCount;
            s_savedRate    = gameSecondsPerRealSecond;
            s_savedRunning = isRunning;
            Instance       = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // <<< חדש
    {
        // דילוג על הפריים הראשון + דיכוי אירוע חצות ממש אחרי טעינה
        skipFramesAfterLoad          = 1;
        suppressNewDayUntilUnscaled  = Time.unscaledTime + newDayGraceAfterLoad;

        // (אופציונלי) כאן אפשר לרענן רפרנסים ל־clockText/background מהסצנה החדשה
    }

    private void Update()
    {
        if (!isRunning) return;

        if (skipFramesAfterLoad > 0) // <<< חדש – לא מתקדמים בפריים הראשון אחרי Load
        {
            skipFramesAfterLoad--;
            UpdateClockUI();
            return;
        }

        float dt = stopWhenPaused ? Time.deltaTime : Time.unscaledDeltaTime;
        dt = Mathf.Min(dt, maxRealDeltaClamp); // <<< חדש – Clamping לדלתא

        if (dt <= 0f) return;

        secondsSinceMidnight += dt * gameSecondsPerRealSecond;

        if (secondsSinceMidnight >= SecondsPerDay)
        {
            int daysPassed = Mathf.FloorToInt(secondsSinceMidnight / SecondsPerDay);
            secondsSinceMidnight = secondsSinceMidnight % SecondsPerDay;

            for (int i = 0; i < daysPassed; i++)
            {
                dayCount++;
                // <<< חדש – לא לירות OnNewDay מייד אחרי טעינה
                if (Time.unscaledTime >= suppressNewDayUntilUnscaled)
                    OnNewDay?.Invoke(dayCount);
            }
        }

        UpdateClockUI();
    }

    private void UpdateClockUI()
    {
        int h = Mathf.FloorToInt(secondsSinceMidnight / 3600f) % 24;
        int m = Mathf.FloorToInt((secondsSinceMidnight % 3600f) / 60f);
        if (clockText) clockText.text = $"{h:00}:{m:00}";
        if (background) background.enabled = true;
    }

    public void PauseClock(bool pause) => isRunning = !pause;

    public void SetTime(int hour, int minute)
    {
        hour   = Mathf.Clamp(hour,   0, 23);
        minute = Mathf.Clamp(minute, 0, 59);
        secondsSinceMidnight = (hour * 60f + minute) * 60f;
        UpdateClockUI();
    }

    public float GameSecondsPerRealSecond => gameSecondsPerRealSecond;

    public void SetRate_GameMinutesPerRealMinute(float gameMinutesPerRealMinute)
    {
        gameSecondsPerRealSecond = gameMinutesPerRealMinute;
    }

    public (int hour, int minute) GetCurrentTime()
    {
        int h = Mathf.FloorToInt(secondsSinceMidnight / 3600f) % 24;
        int m = Mathf.FloorToInt((secondsSinceMidnight % 3600f) / 60f);
        return (h, m);
    }

    public long GetDayCount() => dayCount;
    
    public void BindUI(Text newClockText, Image newBackground)
    {
        clockText = newClockText;
        background = newBackground;
        UpdateClockUI();
    }
}

public static class GameTime
{
    private const float FALLBACK_GSRS = 30f;

    private static float GSRS
        => Timer.Instance ? Timer.Instance.GameSecondsPerRealSecond : FALLBACK_GSRS;

    public static float GameSecondsToRealSeconds(float gameSeconds) => gameSeconds / GSRS;
    public static float RealSecondsToGameSeconds(float realSeconds) => realSeconds * GSRS;

    public static float GameMinutesToRealSeconds(float gameMinutes) => GameSecondsToRealSeconds(gameMinutes * 60f);
    public static float GameHoursToRealSeconds  (float gameHours)   => GameSecondsToRealSeconds(gameHours * 3600f);
}