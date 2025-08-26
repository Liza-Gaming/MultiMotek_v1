using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public static Timer Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Text clockText;     // טקסט שמציג את השעה (HH:MM)
    [SerializeField] private Image background;   // לא חובה – רק אם יש רקע מאחורי השעון

    [Header("Clock Settings")]
    [SerializeField] private float gameSecondsPerRealSecond = 30f; // 30 דקות/דקה
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool stopWhenPaused = true;

    [Header("Start Time (HH:MM)")]
    [Range(0, 23)] [SerializeField] private int startHour = 0;
    [Range(0, 59)] [SerializeField] private int startMinute = 0;

    private const float SecondsPerDay = 24f * 60f * 60f; // 86400
    private float secondsSinceMidnight;                  // 0..86400
    private bool isRunning;
    private long dayCount;                               // כמה יממות חלפו (לא חובה, אך שימושי)

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        secondsSinceMidnight = (startHour * 60f + startMinute) * 60f;
        isRunning = autoStart;
        UpdateClockUI();
    }

    private void Update()
    {
        if (!isRunning) return;

        float dt = stopWhenPaused ? Time.deltaTime : Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        secondsSinceMidnight += dt * gameSecondsPerRealSecond;

        // לולאת יממה: חוזר ל-00:00 וממשיך לרוץ
        if (secondsSinceMidnight >= SecondsPerDay)
        {
            dayCount += Mathf.FloorToInt(secondsSinceMidnight / SecondsPerDay);
            secondsSinceMidnight = secondsSinceMidnight % SecondsPerDay;
            // כאן אפשר לירות אירוע "יום חדש" אם תרצי
        }

        UpdateClockUI();
    }

    private void UpdateClockUI()
    {
        int h = Mathf.FloorToInt(secondsSinceMidnight / 3600f) % 24;
        int m = Mathf.FloorToInt((secondsSinceMidnight % 3600f) / 60f);
        if (clockText) clockText.text = $"{h:00}:{m:00}";
        if (background) background.enabled = true; // לא חובה – רק אם משתמשים ברקע
    }

    // --- API נוח לשימוש מבחוץ ---

    public void PauseClock(bool pause) => isRunning = !pause;

    public void SetTime(int hour, int minute)
    {
        hour = Mathf.Clamp(hour, 0, 23);
        minute = Mathf.Clamp(minute, 0, 59);
        secondsSinceMidnight = (hour * 60f + minute) * 60f;
        UpdateClockUI();
    }
    public float GameSecondsPerRealSecond => gameSecondsPerRealSecond;

    public void SetRate_GameMinutesPerRealMinute(float gameMinutesPerRealMinute)
    {
        // 30 דק'/דקה -> 30 שניות-משחק בכל שנייה אמיתית
        gameSecondsPerRealSecond = gameMinutesPerRealMinute;
    }

    public (int hour, int minute) GetCurrentTime()
    {
        int h = Mathf.FloorToInt(secondsSinceMidnight / 3600f) % 24;
        int m = Mathf.FloorToInt((secondsSinceMidnight % 3600f) / 60f);
        return (h, m);
    }

    public long GetDayCount() => dayCount; // כמה יממות חלפו
}

public static class GameTime
{
    private const float FALLBACK_GSRS = 30f; // ברירת מחדל אם אין שעון בסצנה

    private static float GSRS
        => Timer.Instance ? Timer.Instance.GameSecondsPerRealSecond : FALLBACK_GSRS;

    public static float GameSecondsToRealSeconds(float gameSeconds) => gameSeconds / GSRS;
    public static float RealSecondsToGameSeconds(float realSeconds) => realSeconds * GSRS;

    public static float GameMinutesToRealSeconds(float gameMinutes) => GameSecondsToRealSeconds(gameMinutes * 60f);
    public static float GameHoursToRealSeconds  (float gameHours)   => GameSecondsToRealSeconds(gameHours * 3600f);
}