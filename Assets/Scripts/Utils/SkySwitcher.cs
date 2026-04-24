using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class SkySwitcher : MonoBehaviour
{
    [Header("Sky Roots")]
    [SerializeField] private GameObject dayRoot;
    [SerializeField] private GameObject nightRoot;

    [Header("Switch Times (24h)")]
    [SerializeField] private int dayStartHour = 7;
    [SerializeField] private int nightStartHour = 19;

    [Header("Global Lighting")]
    [Tooltip("גררי לכאן את ה-Global Light 2D שלך")]
    [SerializeField] private Light2D globalLight;
    
    [SerializeField] private Color dayLightColor = Color.white;
    [SerializeField] private float dayLightIntensity = 1f;
    
    [SerializeField] private Color nightLightColor = new Color(0.3f, 0.3f, 0.5f, 1f);
    [SerializeField] private float nightLightIntensity = 0.8f;

    [Header("Clock (Optional)")]
    [SerializeField] private Timer clockProvider;  

    [SerializeField] private int debugHour = 12;
    [SerializeField] private int debugMinute = 0;

    // ----- התוספת: משתנה שכופה יום -----
    [Header("Overrides")]
    public bool forceDayOverride = false; 

    private bool _isDayActive;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(EnsureClockBound());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        StartCoroutine(EnsureClockBound());
        var (h, mnt) = GetTimeHM();
        ApplySet(IsDay(h, mnt));
    }

    private IEnumerator EnsureClockBound()
    {
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
        if (clockProvider == null)
            clockProvider = Timer.Instance ?? FindObjectOfType<Timer>(true);

        if (clockProvider != null)
            return clockProvider.GetCurrentTime();

        return (debugHour, debugMinute);
    }

    bool IsDay(int h, int m)
    {
        // התנאי החדש - אם הכרחנו יום, פשוט תחזיר שזה יום!
        if (forceDayOverride) return true; 

        int t = h * 60 + m;
        int dayStart = dayStartHour * 60;
        int nightStart = nightStartHour * 60;
        
        if (dayStart < nightStart)
            return t >= dayStart && t < nightStart;
        else
            return !(t >= nightStart && t < dayStart);
    }

    void ApplySet(bool day)
    {
        _isDayActive = day;
        
        if (dayRoot) dayRoot.SetActive(day);
        if (nightRoot) nightRoot.SetActive(!day);

        if (globalLight != null)
        {
            globalLight.color = day ? dayLightColor : nightLightColor;
            globalLight.intensity = day ? dayLightIntensity : nightLightIntensity;
        }

        Debug.Log($"[SkySwitcher] Applied: Day={day}, DayRoot={dayRoot?.activeSelf}, NightRoot={nightRoot?.activeSelf}");
    }
}