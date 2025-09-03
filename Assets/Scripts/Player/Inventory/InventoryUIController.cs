using UnityEngine;
using UnityEngine.SceneManagement;


public class InventoryUIController : MonoBehaviour
{
    public GameObject background;
    public GameObject itemSlotContainer;
    public GameObject openInventoryButton;

    private bool inventoryButtonUnlocked = false;
    void Awake()
    {
        background.SetActive(false);
        itemSlotContainer.SetActive(false);
        openInventoryButton.SetActive(true);
    }
    
    void Start()
    {
        var scene = SceneManager.GetActiveScene();
        bool isFirstLevel = scene.buildIndex == 0 || scene.name == "SampleScene"; 
        
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
