using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public GameObject background;
    public GameObject itemSlotContainer;
    public GameObject openInventoryButton;

    void Start()
    {
        CloseInventory();
        openInventoryButton.SetActive(true);
    }

    public void OpenInventory()
    {
        background.SetActive(true);
        itemSlotContainer.SetActive(true);
        openInventoryButton.SetActive(false);
    }

    public void CloseInventory()
    {
        background.SetActive(false);
        itemSlotContainer.SetActive(false);
        openInventoryButton.SetActive(true);
    }
}
