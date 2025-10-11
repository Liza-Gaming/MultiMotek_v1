using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SkySwitcher : MonoBehaviour
{
    [Header("Sky Roots")]
    [SerializeField] private GameObject dayRoot;
    [SerializeField] private GameObject nightRoot;

    [Header("Switch Times (24h)")]
    [SerializeField] private int dayStartHour = 6;   // 06:00
    [SerializeField] private int nightStartHour = 19; // 19:00

    [Header("Clock (Optional)")]
    [SerializeField] private Timer clockProvider;  // אל תסמכי עליו באינספקטור בין סצנות

    [SerializeField] private int debugHour = 12;
    [SerializeField] private int debugMinute = 0;

    private bool _isDayActive;

    void OnEnable()
    {
        // נתעדכן בכל טעינת סצנה (חשוב כש-SkySwitcher נטען מחדש)
        SceneManager.sceneLoaded += OnSceneLoaded;
        // וגם ננסה לקשור מיד
        StartCoroutine(EnsureClockBound());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // כל פעם שסצנה נטענת – ודאי שיש לנו קישור לשעון
        StartCoroutine(EnsureClockBound());
        // תרענני מצב מיד לפי הזמן העדכני
        var (h, mnt) = GetTimeHM();
        ApplySet(IsDay(h, mnt));
    }

    private IEnumerator EnsureClockBound()
    {
        // נחכה עד שה-Timer.Instance קיים (Boot יכול ליצור אותו פריים-שניים אחרייך)
        while (clockProvider == null)
        {
            clockProvider = Timer.Instance ?? FindObjectOfType<Timer>(true);
            if (clockProvider == null) yield return null;
            else break;
        }
    }

    void Start()
    {
        var (h, m) = GetTimeHM();
        ApplySet(IsDay(h, m));
        Debug.Log($"[SkySwitcher] Started at {h:00}:{m:00}, IsDay={_isDayActive}");
    }

    void Update()
    {
        var (h, m) = GetTimeHM();
        bool shouldBeDay = IsDay(h, m);
        if (shouldBeDay != _isDayActive)
        {
            Debug.Log($"[SkySwitcher] Time: {h:00}:{m:00} - Switching to {(shouldBeDay ? "DAY" : "NIGHT")}");
            ApplySet(shouldBeDay);
        }
    }

    (int h, int m) GetTimeHM()
    {
        // Lazy-bind: אם עדיין אין, ננסה לתפוס את הסינגלטון עכשיו
        if (clockProvider == null)
            clockProvider = Timer.Instance ?? FindObjectOfType<Timer>(true);

        if (clockProvider != null)
            return clockProvider.GetCurrentTime();

        // Fallback
        return (debugHour, debugMinute);
    }

    bool IsDay(int h, int m)
    {
        int t = h * 60 + m;
        int dayStart = dayStartHour * 60;
        int nightStart = nightStartHour * 60;
        if (dayStart < nightStart)
            return t >= dayStart && t < nightStart; // 06:00–18:59
        else
            return !(t >= nightStart && t < dayStart);
    }

    void ApplySet(bool day)
    {
        _isDayActive = day;
        if (dayRoot) dayRoot.SetActive(day);
        if (nightRoot) nightRoot.SetActive(!day);
        // (לא חובה) אפשר לרענן כאן Parallax אם צריך
        Debug.Log($"[SkySwitcher] Applied: Day={day}, DayRoot={dayRoot?.activeSelf}, NightRoot={nightRoot?.activeSelf}");
    }
}
