namespace Player.Sugarcontrol.InsulinPump
{
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.UI;

    public class LowSugarWarningUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SugarMeter sugarMeter;
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

        private FieldInfo _fiSugarLevel;
        private float _timer;

        private void Awake()
        {
            // מציאת הרפרנס ל-SugarMeter
            if (sugarMeter == null) 
            {
                sugarMeter = FindFirstObjectByType<SugarMeter>();
            }

            // ודא שההתראה מוסתרת בהתחלה
            if (warningPanel != null) 
            {
                warningPanel.SetActive(false);
            }

            // שמירת ה-Reflection רק לשדה הספציפי שאנחנו צריכים
            _fiSugarLevel = typeof(SugarMeter).GetField("sugarLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void Update()
        {
            if (sugarMeter == null || _fiSugarLevel == null) return;

            // טיימר קצר כדי לא להשתמש ב-Reflection כל פריים (טוב לביצועים)
            _timer += Time.unscaledDeltaTime;
            if (_timer < checkEveryRealSeconds) return;
            _timer = 0f;

            // קריאת הערך הנוכחי של הסוכר
            float currentSugar = (float)_fiSugarLevel.GetValue(sugarMeter);
            
            // האם אנחנו מתחת או שווים לסף?
            bool shouldShowAlert = currentSugar <= threshold;

            // עדכון ה-UI רק אם יש שינוי במצב (מונע SetActive מיותר)
            if (warningPanel != null && warningPanel.activeSelf != shouldShowAlert)
            {
                warningPanel.SetActive(shouldShowAlert);

                if (shouldShowAlert && warningText != null)
                {
                    // במידה ויש בעיות כיווניות ביוניטי (RTL), אפשר לשנות את הטקסט הזה ישר דרך ה-Inspector במקום בקוד
                    warningText.text = alertMessage;
                }
            }
        }
    }
}