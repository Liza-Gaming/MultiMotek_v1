using UnityEngine;
using UnityEngine.UI;

public class SugarMeter : MonoBehaviour
{
    public static SugarMeter Instance; // Singleton

    [Tooltip("Initial sugar level")]
    public float startSugar = 100f;

    [Tooltip("decending rate")]
    public float sugarDecreaseRate = 1f;
    
    public int maxHearts = 3;
    private int currentHearts;
    
    [Header("Sugar safe range")]
    public float minSugar = 70f;
    public float maxSugar = 180f;
    
    [Header("Heart lose timing")]
    public float timeOutsideRangeToLoseHeart = 20f;
    
    private float timeOutsideSafeRange = 0f;

    public Image[] heartImages;

    public Text sugarText;

    private float sugarLevel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        sugarLevel = startSugar;
        currentHearts = maxHearts;
        UpdateSugarUI();
        UpdateHeartsUI();
    }

    void Update()
    {
        sugarLevel -= sugarDecreaseRate * Time.deltaTime;
        sugarLevel = Mathf.Max(sugarLevel, 0f);
        UpdateSugarUI();

        // בדוק אם מחוץ לטווח
        if (sugarLevel < minSugar || sugarLevel > maxSugar)
        {
            timeOutsideSafeRange += Time.deltaTime;
            if (timeOutsideSafeRange >= timeOutsideRangeToLoseHeart)
            {
                LoseHeart();
                timeOutsideSafeRange = 0f; // לאפס את הטיימר
            }
        }
        else
        {
            timeOutsideSafeRange = 0f; // חזר לטווח - לאפס טיימר
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

    public void AddSugar(float amount)
    {
        sugarLevel += amount;
        UpdateSugarUI();
    }

    public float GetSugarLevel()
    {
        return sugarLevel;
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
}
