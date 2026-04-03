using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
// https://github.com/Maraakis/ChristinaCreatesGames/blob/main/Toggle%20Switch%20System/ToggleSwitch.cs

namespace Christina.UI
{
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        [Header("Slider setup")] 
        [SerializeField, Range(0, 1f)]
        protected float sliderValue;
        public bool CurrentValue { get; private set; }
        
        private bool _previousValue;
        private Slider _slider;

        [Header("Target Texts")]
        [SerializeField] private Text _target_105;
        [SerializeField] private Text _target_140;

        [Header("Animation")] 
        [SerializeField, Range(0, 1f)] private float animationDuration = 0.5f;
        [SerializeField] private AnimationCurve slideEase =
            AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine _animateSliderCoroutine;

        [Header("Color Transition")]
        [SerializeField] private Image backgroundImage; // גרור לפה את ה-Background של הסליידר
        [SerializeField] private Color offColor = new Color(0.8f, 0.8f, 0.8f, 1f); // צבע כשהמתג כבוי (למשל אפור)
        [SerializeField] private Color onColor = Color.green; // צבע כשהמתג דלוק (ירוק)

        [Header("Events")] 
        [SerializeField] private UnityEvent onToggleOn;
        [SerializeField] private UnityEvent onToggleOff;

        private ToggleSwitchGroupManager _toggleSwitchGroupManager;
        
        protected Action transitionEffect;
        
        protected virtual void OnValidate()
        {
            SetupToggleComponents();

            _slider.value = sliderValue;
            UpdateColor(sliderValue); // עדכון הצבע גם בתוך העורך של יוניטי
            
            // עדכון חזותי של הטקסטים בעורך (מעל 0.5 נחשב מופעל לצורך תצוגה)
            UpdateTextVisibility(sliderValue >= 0.5f);
        }

        private void SetupToggleComponents()
        {
            if (_slider != null)
                return;

            SetupSliderComponent();
        }

        private void SetupSliderComponent()
        {
            _slider = GetComponent<Slider>();

            if (_slider == null)
            {
                Debug.Log("No slider found!", this);
                return;
            }

            // ניסיון אוטומטי למצוא את ה-Background אם שכחת לגרור אותו ב-Inspector
            if (backgroundImage == null)
            {
                Transform bgTransform = _slider.transform.Find("Background");
                if (bgTransform != null)
                {
                    backgroundImage = bgTransform.GetComponent<Image>();
                }
            }

            _slider.interactable = false;
            var sliderColors = _slider.colors;
            sliderColors.disabledColor = Color.white;
            _slider.colors = sliderColors;
            _slider.transition = Selectable.Transition.None;
        }
        
        public void SetupForManager(ToggleSwitchGroupManager manager)
        {
            _toggleSwitchGroupManager = manager;
        }

        protected virtual void Awake()
        {
            SetupSliderComponent();
            CurrentValue = sliderValue >= 0.5f; // הגדרת מצב התחלתי
            UpdateColor(_slider.value); // עדכון ראשוני של הצבע
            UpdateTextVisibility(CurrentValue); // עדכון ראשוני של הטקסטים
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }
        
        private void Toggle()
        {
            if (_toggleSwitchGroupManager != null)
                _toggleSwitchGroupManager.ToggleGroup(this);
            else
                SetStateAndStartAnimation(!CurrentValue);
        }

        public void ToggleByGroupManager(bool valueToSetTo)
        {
            SetStateAndStartAnimation(valueToSetTo);
        }
        
        private void SetStateAndStartAnimation(bool state)
        {
            _previousValue = CurrentValue;
            CurrentValue = state;

            // עדכון נראות הטקסטים מיד עם שינוי המצב
            UpdateTextVisibility(CurrentValue);

            if (_previousValue != CurrentValue)
            {
                if (CurrentValue)
                    onToggleOn?.Invoke();
                else
                    onToggleOff?.Invoke();
            }

            if (_animateSliderCoroutine != null)
                StopCoroutine(_animateSliderCoroutine);

            _animateSliderCoroutine = StartCoroutine(AnimateSlider());
        }

        private IEnumerator AnimateSlider()
        {
            float startValue = _slider.value;
            float endValue = CurrentValue ? 1 : 0;

            float time = 0;
            if (animationDuration > 0)
            {
                while (time < animationDuration)
                {
                    time += Time.deltaTime;

                    float lerpFactor = slideEase.Evaluate(time / animationDuration);
                    _slider.value = sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                    UpdateColor(_slider.value); // עדכון הצבע בהדרגה יחד עם תנועת הסליידר

                    transitionEffect?.Invoke();
                        
                    yield return null;
                }
            }

            _slider.value = endValue;
            UpdateColor(_slider.value); // וידוא שהצבע מגיע ליעד הסופי שלו בסיום האנימציה
        }

        // פונקציית עזר לשינוי הצבע לפי המיקום של הסליידר
        private void UpdateColor(float value)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = Color.Lerp(offColor, onColor, value);
            }
        }

        // פונקציית עזר להדלקה וכיבוי של הטקסטים לפי מצב הסוויץ'
        private void UpdateTextVisibility(bool isToggleOn)
        {
            // כשהסוויץ' כבוי (!isToggleOn), נרצה ש-105 יהיה דלוק. כשהוא דלוק - יכובה.
            if (_target_105 != null)
            {
                _target_105.gameObject.SetActive(!isToggleOn);
            }

            // כשהסוויץ' דלוק (isToggleOn), נרצה ש-140 יהיה דלוק. כשהוא כבוי - יכובה.
            if (_target_140 != null)
            {
                _target_140.gameObject.SetActive(isToggleOn);
            }
        }
    }
}