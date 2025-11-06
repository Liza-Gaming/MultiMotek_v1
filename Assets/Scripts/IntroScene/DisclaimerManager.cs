using UnityEngine;
using UnityEngine.UI;
using System;

public class DisclaimerManager : MonoBehaviour
{
    [SerializeField] private GameObject disclaimerPanel;
    [SerializeField] private Toggle agreeToggle;
    [SerializeField] private Button continueButton;
    
    private const string DISCLAIMER_KEY = "HasAgreedToDisclaimer";
    
    private Action _onDisclaimerFinished;

    void Start()
    {
        //Tests
        //PlayerPrefs.DeleteKey(DISCLAIMER_KEY);
        if (disclaimerPanel) disclaimerPanel.SetActive(false);

        // חבר את הפונקציות לכפתורים
        if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);
        if (agreeToggle) agreeToggle.onValueChanged.AddListener(OnToggleChanged);
    }
    
    public void CheckDisclaimer(Action onCompleteCallback)
    {

        int hasAgreed = PlayerPrefs.GetInt(DISCLAIMER_KEY, 0);

        if (hasAgreed == 1)
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

        PlayerPrefs.SetInt(DISCLAIMER_KEY, 1);
        PlayerPrefs.Save();
        
        disclaimerPanel.SetActive(false);


        _onDisclaimerFinished?.Invoke();
        _onDisclaimerFinished = null;
    }
}