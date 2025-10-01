using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class LevelBootstrap : MonoBehaviour
{
    [Header("Run only on Standalone (menu)")]
    [SerializeField] private bool applyOnStandaloneOnly = true;

    [Header("Sugar Init")]
    [SerializeField] private bool overrideSugar = true;
    [SerializeField] private float startSugar = 80f;
    [SerializeField] private bool clearSugarTrends = true;
    [SerializeField] private bool resetHeartsOnStart = false;
    [SerializeField] private int  startHearts = 3;

    [Header("Clock Init")]
    [SerializeField] private bool overrideClock = true;
    [SerializeField, Range(0,23)] private int startHour = 7;
    [SerializeField, Range(0,59)] private int startMinute = 0;
    [SerializeField] private bool overrideClockRate = false;
    [SerializeField] private float gameMinutesPerRealMinute = 30f;
    [SerializeField] private bool autoRunClock = true;

    private bool _shouldApply;

    private void Awake()
    {
        // אם applyOnStandaloneOnly כבוי—תמיד נפעיל; אחרת, נשתמש בדגל החד-פעמי
        _shouldApply = !applyOnStandaloneOnly || AppFlow.ConsumeStandaloneInitFlag();

        if (!_shouldApply) return;

        // ← איפוס שכבות סטטיות רק כשבאמת נכנסנו מהתפריט
        SugarMeter.RequestSkipRestoreOnNextStart();
        SugarMeter.ClearSavedState(clearTrends: true);
        Timer.ClearSavedState();
    }

    private void Start()
    {
        if (!_shouldApply) return;

        InitClock();
        InitSugar();
        InitInventory();
        InitSugarBlur();
    }

    private void InitClock()
    {
        var timer = Timer.Instance ? Timer.Instance : FindFirstObjectByType<Timer>();
        if (!timer) return;

        if (overrideClockRate)
            timer.SetRate_GameMinutesPerRealMinute(gameMinutesPerRealMinute);

        if (overrideClock)
            timer.SetTime(startHour, startMinute);

        timer.PauseClock(!autoRunClock);
    }

    private void InitSugar()
    {
        if (!overrideSugar) return;

        var sm = SugarMeter.Instance ? SugarMeter.Instance : FindFirstObjectByType<SugarMeter>();
        if (!sm) return;

        sm.ForceSetForLevel(startSugar, clearTrends: clearSugarTrends);

        if (resetHeartsOnStart)
            sm.ResetHearts(startHearts);
    }

    private void InitInventory()
    {
        var inv = FindFirstObjectByType<Inventory>();
        if (inv) inv.ClearAll();

        var ui = FindFirstObjectByType<UI_inventory>();
        if (ui && inv) ui.SetInventory(inv);
    }

    private void InitSugarBlur()
    {
        var blur = FindFirstObjectByType<SugarBlurController>(FindObjectsInactive.Include);
        if (blur) blur.ResetToDefault();
    }
}
