using UnityEngine;
using UnityEngine.UI;
using System;

public class DisclaimerManager : MonoBehaviour
{
    [SerializeField] private GameObject disclaimerPanel;
    [SerializeField] private Toggle agreeToggle;
    [SerializeField] private Button continueButton;
    
    private bool _hasAgreedInThisSession = false;
    
    private Action _onDisclaimerFinished;

    void Start()
    {
        if (disclaimerPanel) disclaimerPanel.SetActive(false);
        
        if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);
        if (agreeToggle) agreeToggle.onValueChanged.AddListener(OnToggleChanged);
    }
    
    public void CheckDisclaimer(Action onCompleteCallback)
    {
        if (_hasAgreedInThisSession)
        {
            onCompleteCallback?.Invoke();
        }
        else
        {
            _onDisclaimerFinished = onCompleteCallback;
            ShowPanel();
        }
    }

    private void ShowPanel()
    {
        disclaimerPanel.SetActive(true);
        agreeToggle.isOn = false;
        continueButton.interactable = false;
    }

    private void OnToggleChanged(bool isChecked)
    {
        continueButton.interactable = isChecked;
    }

    private void OnContinueClicked()
    {

        _hasAgreedInThisSession = true;
        
        disclaimerPanel.SetActive(false);

        _onDisclaimerFinished?.Invoke();
        _onDisclaimerFinished = null;
    }
}