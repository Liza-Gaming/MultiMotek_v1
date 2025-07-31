using UnityEngine;
using UnityEngine.UI;

public class SugarMeter : MonoBehaviour
{
    public static SugarMeter Instance; // Singleton

    [Tooltip("Initial sugar level")]
    public float startSugar = 100f;

    [Tooltip("decending rate")]
    public float sugarDecreaseRate = 1f;

    public Text sugarText;

    private float sugarLevel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        sugarLevel = startSugar;
        UpdateSugarUI();
    }

    void Update()
    {
        sugarLevel -= sugarDecreaseRate * Time.deltaTime;
        sugarLevel = Mathf.Max(sugarLevel, 0f);
        UpdateSugarUI();
    }

    void UpdateSugarUI()
    {
        if (sugarText != null)
            sugarText.text = Mathf.RoundToInt(sugarLevel).ToString();
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
}
