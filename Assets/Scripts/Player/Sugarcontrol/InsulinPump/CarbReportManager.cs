using UnityEngine.Timeline;

namespace Player.Sugarcontrol.InsulinPump
{
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Optional lock")]
    [SerializeField] private PlayerMover playerMover;

    [SerializeField] private Pause pauseManager;

    private int _expected;
    private Action _onCorrect;
    private bool _active;
    private System.Action<int> _onCorrectInt;
    [SerializeField] private string titleString = "תומימחפ חוויד";

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
    }

    private bool EnabledThisScene()
        => SceneManager.GetActiveScene().buildIndex >= enableFromBuildIndex;

    public bool RequestReport(int expectedCarbs, System.Action<int> onCorrect)
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

        if (playerMover == null) playerMover = FindObjectOfType<PlayerMover>();
        playerMover?.SetInputLocked(true);

        pauseManager.SoftPauseFor("CarbReport");

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
        if (valueText && slider) valueText.text = ((int)slider.value).ToString();
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
            Close();
            _onCorrectInt?.Invoke(reported);
            _onCorrectInt = null;
        }
        else
        {
            if (errorText)
            {
                errorText.text = "בוש וסנ ,ןוכנ אל";
                errorText.color = Color.red;
                errorText.gameObject.SetActive(true);
            }
        }
    }

    private void Close()
    {
        if (panel) panel.SetActive(false);
        pauseManager.SoftResumeFor("CarbReport");
        playerMover?.SetInputLocked(false);
        _active = false;
    }
}
}
