using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SugarUIManager : MonoBehaviour
{
    public GameObject sugarPanel;
    public GameObject sugarCheckButton;
    public float displayDuration = 3f;

    void Start()
    {
        sugarPanel.SetActive(false);
        sugarCheckButton.SetActive(true);
    }

    public void OnSugarCheckButtonClicked()
    {
        sugarPanel.SetActive(true);
        sugarCheckButton.SetActive(false);
        StartCoroutine(HideSugarPanelAfterDelay());
    }

    private IEnumerator HideSugarPanelAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        sugarPanel.SetActive(false);
        sugarCheckButton.SetActive(true);
    }
}