using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class SugarMeter : MonoBehaviour
{
    public static SugarMeter Instance;

    [Tooltip("Initial sugar level")]
    public float startSugar = 100f;

    [Tooltip("decending rate")]
    public float sugarDecreaseRate = 1f;
    
    public int maxHearts = 3;
    private int currentHearts;
    
    [Header("Sugar safe range")]
    public float minSugar = 70f;
    public float maxSugar = 180f;
    
    public float minSugarClamp = 0f;
    public float maxSugarClamp = 400f;
    
    public float timeOutsideRangeToLoseHeart = 20f;
    public float timeInsideRangeToGainHeart = 20f;
    
    private float timeOutsideSafeRange = 0f;
    private float timeInsideSafeRange = 0f;

    public Image[] heartImages;

    public Text sugarText;
    
    [SerializeField] private WeatherManager weatherManager;

    private float sugarLevel;
    
    private class TimedRate { public float ratePerSec; public float remaining; }
    private readonly List<TimedRate> activeRates = new List<TimedRate>();

    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        sugarLevel = startSugar;
        currentHearts = 0;
        UpdateSugarUI();
        UpdateHeartsUI();
    }

    void Update()
    {
        float totalRate = -sugarDecreaseRate;
        
        for (int i = 0; i < activeRates.Count; i++)
            totalRate += activeRates[i].ratePerSec;
        
        sugarLevel += totalRate * Time.deltaTime;
        
        sugarLevel = Mathf.Clamp(sugarLevel, minSugarClamp, maxSugarClamp);

        UpdateSugarUI();
        UpdateHeartsLogic();
    }

    private void UpdateHeartsLogic()
    {
        if (sugarLevel >= minSugar && sugarLevel <= maxSugar)
        {
            timeInsideSafeRange += Time.deltaTime;
            timeOutsideSafeRange = 0f;

            if (timeInsideSafeRange >= timeInsideRangeToGainHeart)
            {
                GainHeart();
                timeInsideSafeRange = 0f;
            }
        }
        else
        {
            timeOutsideSafeRange += Time.deltaTime;
            timeInsideSafeRange = 0f;

            if (timeOutsideSafeRange >= timeOutsideRangeToLoseHeart)
            {
                LoseHeart();
                timeOutsideSafeRange = 0f;
            }
        }
    }


    void UpdateSugarUI()
    {
        if (sugarText != null)
            sugarText.text = Mathf.RoundToInt(sugarLevel).ToString();
    }
    
    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].enabled = i < currentHearts;
        }
    }
    

    public float GetSugarLevel()
    {
        return sugarLevel;
    }

    void GainHeart()
    {
        if (currentHearts < maxHearts)
        {
            currentHearts++;
            UpdateHeartsUI();
        }
    }
    
    void LoseHeart()
    {
        if (currentHearts > 0)
        {
            currentHearts--;
            UpdateHeartsUI();
            
            if (currentHearts == 0)
            {
                Debug.Log("Game Over!");
            }
        }
    }
    public int CurrentHearts => currentHearts;
    
    public void AddSugar(float amount, float baseDurationSec = 0f, bool affectByWeather = true)
    {
        if (baseDurationSec <= 0f)
        {
            sugarLevel = Mathf.Clamp(sugarLevel + amount, minSugarClamp, maxSugarClamp);
            UpdateSugarUI();
            return;
        }

        ApplyTimedChange(+Mathf.Abs(amount), baseDurationSec, affectByWeather);
    }

    public void DecreaseSugar(float amount, float baseDurationSec = 0f, bool affectByWeather = true)
    {
        if (baseDurationSec <= 0f)
        {
            sugarLevel = Mathf.Clamp(sugarLevel - Mathf.Abs(amount), minSugarClamp, maxSugarClamp);
            UpdateSugarUI();
            return;
        }

        ApplyTimedChange(-Mathf.Abs(amount), baseDurationSec, affectByWeather);
    }

    private void ApplyTimedChange(float deltaTotal, float baseDurationSec, bool affectByWeather)
    {
        float mult = 1f;
        if (affectByWeather && weatherManager != null)
            mult = weatherManager.GetSpeedMultiplier();
        
        float actualDuration = baseDurationSec / mult;
        
        float ratePerSec = deltaTotal / Mathf.Max(0.0001f, actualDuration);

        StartCoroutine(ApplyRateCoroutine(ratePerSec, actualDuration));
    }

    private IEnumerator ApplyRateCoroutine(float ratePerSec, float duration)
    {
        var r = new TimedRate { ratePerSec = ratePerSec, remaining = duration };
        activeRates.Add(r);

        while (r.remaining > 0f)
        {
            r.remaining -= Time.deltaTime;
            yield return null;
        }

        activeRates.Remove(r);
    }
    
}
