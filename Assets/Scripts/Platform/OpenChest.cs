using UnityEngine;

public class OpenChest : MonoBehaviour
{
    public GameObject popupPanel;

    private bool isPopUpOpened = false;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isPopUpOpened)
        {
            if (collider.CompareTag("Player"))
            {
                popupPanel.SetActive(true);
                isPopUpOpened = true;
            }
        }
    }

    public void ClosePopUp()
    {
        popupPanel.SetActive(false);
    }

}
