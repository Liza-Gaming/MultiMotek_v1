using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Player.Sugarcontrol.InsulinPump
{
    public class CarbReportManager : MonoBehaviour
    {
        public static CarbReportManager Instance { get; private set; }

        [Header("Enable from scene")]
        [SerializeField] private int enableFromBuildIndex = 5;

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider slider;
        [SerializeField] private Text valueText;
        [SerializeField] private Text errorText;
        [SerializeField] private Button confirmButton;

        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;
        
        [Header("Item Image")]
        [SerializeField] private Image itemIconImage;

        [Header("Optional lock")]
        [SerializeField] private PlayerMover playerMover;
        [SerializeField] private Pause pauseManager;

        [Header("Time buttons")]
        [SerializeField] private Button nowButton;
        [SerializeField] private Button min15Button;
        [SerializeField] private Button min30Button;
        [SerializeField] private Button min45Button;

        // NEW: Skip insulin
        [Header("Skip insulin")]
        [SerializeField] private Button skipInsulinButton;         // כפתור "דלג על הזרקה"
        [SerializeField] private Text skipInsulinButtonText;       // אופציונלי: טקסט שעל הכפתור
        [Header("Text")]
        [SerializeField] private string titleString = "תומימחפ חוויד";

        // ---- internal report state ----
        private int _expected;
        private bool _active;
        private Action<int> _onCorrectInt;

        // What the time buttons control
        private float _selectedInsulinDelayGameMin = 0f;

        // Data the pump needs on confirm
        private float _expectedFoodRiseMgdl;
        private float _foodDurationGameMin;

        // NEW: should we skip pump injection entirely for this meal?
        private bool _skipInsulinThisMeal = false;

        private bool EnabledThisScene()
            => SceneManager.GetActiveScene().buildIndex >= enableFromBuildIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (panel) panel.SetActive(false);

            if (errorText)
            {
                errorText.gameObject.SetActive(true);
                errorText.color = Color.black;
                errorText.text = titleString;
            }

            if (confirmButton) confirmButton.onClick.AddListener(OnConfirm);

            if (plusButton) plusButton.onClick.AddListener(OnPlus);
            if (minusButton) minusButton.onClick.AddListener(OnMinus);

            if (slider) slider.onValueChanged.AddListener(_ => UpdateValueText());

            if (nowButton) nowButton.onClick.AddListener(() => SetInsulinDelay(0f));
            if (min15Button) min15Button.onClick.AddListener(() => SetInsulinDelay(15f));
            if (min30Button) min30Button.onClick.AddListener(() => SetInsulinDelay(30f));
            if (min45Button) min45Button.onClick.AddListener(() => SetInsulinDelay(45f));

            // NEW
            if (skipInsulinButton) skipInsulinButton.onClick.AddListener(ToggleSkipInsulin);
        }

        private void SetInsulinDelay(float delayGameMin)
        {
            _selectedInsulinDelayGameMin = delayGameMin;
        }

        // NEW: toggle skip
        private void ToggleSkipInsulin()
        {
            SetSkipInsulin(!_skipInsulinThisMeal);
        }

        // NEW: set skip + update label
        private void SetSkipInsulin(bool skip)
        {
            _skipInsulinThisMeal = skip;
            
            // אם את רוצה גם לשנות צבע/הדגשה — אפשר פה.
        }

        /// <summary>
        /// פותח פאנל דיווח.
        /// expectedFoodRiseMgdl: כמה mg/dL האוכל צפוי להעלות סה"כ (המודל שלך).
        /// foodDurationGameMin: כמה זמן העלייה נמשכת.
        /// </summary>
        public bool RequestReport(
            int expectedCarbs,
            float expectedFoodRiseMgdl,
            float foodDurationGameMin,
            Sprite itemSprite,
            Action<int> onCorrect
        )
        {
            if (!EnabledThisScene())
            {
                onCorrect?.Invoke(expectedCarbs);
                return false;
            }

            if (_active) return true;

            _active = true;
            _expected = expectedCarbs;
            _onCorrectInt = onCorrect;

            _expectedFoodRiseMgdl = expectedFoodRiseMgdl;
            _foodDurationGameMin = foodDurationGameMin;

            // NEW: reset per open
            _selectedInsulinDelayGameMin = 0f;
            SetSkipInsulin(false);
            
            if (itemIconImage != null)
            {
                if (itemSprite != null)
                {
                    itemIconImage.sprite = itemSprite;
                    itemIconImage.gameObject.SetActive(true);
                }
                else
                {
                    itemIconImage.gameObject.SetActive(false);
                }
            }

            if (playerMover == null) playerMover = FindObjectOfType<PlayerMover>();
            playerMover?.SetInputLocked(true);

            pauseManager?.SoftPauseFor("CarbReport");

            if (errorText)
            {
                errorText.gameObject.SetActive(true);
                errorText.color = Color.white;
                errorText.text = titleString;
            }

            if (slider)
            {
                slider.minValue = 0;
                slider.maxValue = 60;
                slider.wholeNumbers = true;

                slider.value = Mathf.Clamp(0, (int)slider.minValue, (int)slider.maxValue);
                UpdateValueText();
            }

            if (plusButton) plusButton.interactable = (slider != null);
            if (minusButton) minusButton.interactable = (slider != null);

            if (panel) panel.SetActive(true);
            return true;
        }

        private void UpdateValueText()
        {
            if (valueText && slider)
                valueText.text = ((int)slider.value).ToString();
        }

        private void OnPlus()
        {
            if (!_active || slider == null) return;
            slider.value = Mathf.Clamp(slider.value + 1, slider.minValue, slider.maxValue);
        }

        private void OnMinus()
        {
            if (!_active || slider == null) return;
            slider.value = Mathf.Clamp(slider.value - 1, slider.minValue, slider.maxValue);
        }

        private void OnConfirm()
        {
            if (!_active) return;

            int reported = slider ? (int)slider.value : 0;

            if (reported == _expected)
            {
                // BEFORE close: trigger pump only if NOT skipped
                if (!_skipInsulinThisMeal && PumpLogic.Instance != null)
                {
                    PumpLogic.Instance.OnMealReportedPID(
                        carbsGrams: reported,
                        expectedFoodRiseMgdl: _expectedFoodRiseMgdl,
                        foodDelayGameMin: _selectedInsulinDelayGameMin,
                        foodDurationGameMin: _foodDurationGameMin
                    );
                }

                Close();

                _onCorrectInt?.Invoke(reported);
                _onCorrectInt = null;
            }
            else
            {
                if (errorText)
                {
                    errorText.text = "ארקמל ונפ ,ןוכנ אל";
                    errorText.color = Color.red;
                    errorText.gameObject.SetActive(true);
                }
            }
        }

        private void Close()
        {
            if (panel) panel.SetActive(false);
            pauseManager?.SoftResumeFor("CarbReport");
            playerMover?.SetInputLocked(false);
            _active = false;
        }
    }
}
