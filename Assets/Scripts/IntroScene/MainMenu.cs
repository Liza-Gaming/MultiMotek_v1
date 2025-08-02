using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public GameObject instructionsPanel;
    public Animator instructionsAnimator;

    private bool isClosing = false;

    public void OnInstructionsButton()
    {
        if (!instructionsPanel.activeSelf)
        {
            instructionsPanel.SetActive(true);
           // instructionsAnimator.ResetTrigger("Hide");
            instructionsAnimator.SetTrigger("Show");
        }
    }

    public void ClosePopUp()
    {
        if (!isClosing)
        {
            isClosing = true;
        //    instructionsAnimator.ResetTrigger("Show");
            instructionsAnimator.SetTrigger("Hide");
            StartCoroutine(DisablePanelAfterDelay(0.01f));
        }
    }

    private IEnumerator DisablePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        instructionsPanel.SetActive(false);
        isClosing = false;
    }

    public void OnPlayButton()
    {
        SceneManager.LoadScene("SampleScene");
    }
}