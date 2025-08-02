using UnityEngine;

public class OpenChest : MonoBehaviour
{
    public GameObject popupPanel;

    private bool isPopUpOpened = false;
    public Animator instructionsAnimator;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isPopUpOpened)
        {
            if (collider.CompareTag("Player"))
            {
                popupPanel.SetActive(true);
                instructionsAnimator.SetTrigger("Show");
                isPopUpOpened = true;
            }
        }
    }

    public void ClosePopUp()
    {
        popupPanel.SetActive(false);
        instructionsAnimator.SetTrigger("Hide");
    }

}
