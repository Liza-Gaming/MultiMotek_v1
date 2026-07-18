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
        [SerializeField] private float threshold = 70f;
        
        [SerializeField] private float checkEveryRealSeconds = 0.25f;

        [Header("UI Text")]
        [TextArea(3, 5)]
        [SerializeField] private string alertMessage = ":הבאשמ תארתה\n.70 ל תחתמ רכוסה ךרע\n.םדקהב הימקילגופיהב לפטל שי";

        private float _timer;

        private void Awake()
        {
            if (warningPanel != null) 
            {
                warningPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (SugarMeter.Instance == null) return;
            
            _timer += Time.unscaledDeltaTime;
            if (_timer < checkEveryRealSeconds) return;
            _timer = 0f;
            
            float currentSugar = SugarMeter.Instance.GetSugarLevel();
            
            bool shouldShowAlert = currentSugar <= threshold;
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