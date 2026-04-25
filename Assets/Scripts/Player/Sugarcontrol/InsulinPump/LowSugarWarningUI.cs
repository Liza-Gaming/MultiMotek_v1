namespace Player.Sugarcontrol.InsulinPump
{
    using UnityEngine;
    using UnityEngine.UI;

    public class LowSugarWarningUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private Text warningText;

        [Header("Settings")]
        [Tooltip("סף סוכר להקפצת התראה")]
        [SerializeField] private float threshold = 70f;

        [Tooltip("כל כמה שניות אמיתיות לבדוק (חוסך ביצועים מקריאה כל פריים)")]
        [SerializeField] private float checkEveryRealSeconds = 0.25f;

        [Header("UI Text")]
        [TextArea(3, 5)]
        [SerializeField] private string alertMessage = ":הבאשמ תארתה\n.70 ל תחתמ רכוסה ךרע\n.םדקהב הימקילגופיהב לפטל שי";

        private float _timer;

        private void Awake()
        {
            // ודא שההתראה מוסתרת בהתחלה
            if (warningPanel != null) 
            {
                warningPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // גישה בטוחה לסינגלטון - פותר את בעיית ה-Null במעברי סצנות
            if (SugarMeter.Instance == null) return;

            // טיימר קצר כדי לא לבדוק כל פריים (טוב לביצועים)
            _timer += Time.unscaledDeltaTime;
            if (_timer < checkEveryRealSeconds) return;
            _timer = 0f;

            // קריאת הערך הנוכחי של הסוכר בצורה ישירה וללא Reflection
            float currentSugar = SugarMeter.Instance.GetSugarLevel();
            
            // האם אנחנו מתחת או שווים לסף?
            bool shouldShowAlert = currentSugar <= threshold;

            // עדכון ה-UI רק אם יש שינוי במצב (מונע SetActive מיותר)
            if (warningPanel != null && warningPanel.activeSelf != shouldShowAlert)
            {
                warningPanel.SetActive(shouldShowAlert);

                if (shouldShowAlert && warningText != null)
                {
                    warningText.text = alertMessage;
                }
            }
        }
    }
}