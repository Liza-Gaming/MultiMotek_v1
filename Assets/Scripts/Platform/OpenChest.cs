using UnityEngine;

public class OpenChest : MonoBehaviour
{
    public GameObject popupPanel;
    public Animator instructionsAnimator;
    public InventoryUIController inventoryUI;

    private bool popupOpened = false;
    private bool chestConsumed = false;
    
    private PlayerMover playerMover;

    void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (chestConsumed) return;

        if (!popupOpened && collider.CompareTag("Player"))
        {
            if (playerMover == null)
                playerMover = collider.GetComponent<PlayerMover>();

            if (popupPanel != null) popupPanel.SetActive(true);
            if (instructionsAnimator != null) instructionsAnimator.SetTrigger("Show");
            popupOpened = true;
            
            if (playerMover != null) playerMover.SetInputLocked(true);
        }
    }
    
    public void OnLootConfirmed()
    {
        if (inventoryUI != null) inventoryUI.UnlockInventoryButton();

        ClosePopUp();
        chestConsumed = true;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    public void ClosePopUp()
    {
        if (instructionsAnimator != null) instructionsAnimator.SetTrigger("Hide");
        if (popupPanel != null) popupPanel.SetActive(false);
        popupOpened = false;
        
        if (playerMover != null) playerMover.SetInputLocked(false);
    }
}