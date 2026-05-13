using UnityEngine;
using UnityEngine.UI; // חובה כדי להשתמש ב-Button

public class FailUIProvider : MonoBehaviour
{
    [Header("UI - General")]
    public Image blackPanel;
    public Button returnToMenuButton; // כפתור אחד משותף מסוג Button

    [Header("UI - LOSELOW Setup")]
    public GameObject loseLowPanel;
    public TypewriterEffect lowText1;
    public TypewriterEffect lowText2;

    [Header("UI - LOSEHIGH Setup")]
    public GameObject loseHighPanel;
    public TypewriterEffect highText1;
    public TypewriterEffect highText2;

    private void Start()
    {
        // 1. חיבור הרפרנסים למנהל
        if (SugarFailManager.Instance != null)
        {
            SugarFailManager.Instance.SetupUI(this);
        }

        // 2. חיבור הכפתור לפונקציה במנהל דרך הקוד!
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners(); // מנקה חיבורים ישנים למקרה הצורך
            returnToMenuButton.onClick.AddListener(() => SugarFailManager.Instance.ReturnToMenu());
        }
    }
}