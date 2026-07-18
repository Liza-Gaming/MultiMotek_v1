using UnityEngine;
using UnityEngine.SceneManagement;

public class InventoryUIController : MonoBehaviour
{
    public GameObject background;
    public GameObject itemSlotContainer;
    public GameObject openInventoryButton;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject blurVolumeObject;
    [SerializeField] private string blurObjectName = "PauseBlur";
    
    private Pause pauseManager;
    private PlayerManager playerManager;
    private bool isInventoryOpen = false;

    void Awake()
    {
        pauseManager = FindFirstObjectByType<Pause>(); 
        playerManager = FindFirstObjectByType<PlayerManager>();
        background.SetActive(false);
        itemSlotContainer.SetActive(false);
        openInventoryButton.SetActive(true);
        isInventoryOpen = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isInventoryOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }
    }

    private void EnsureBlurVolumeExists()
    {
        if (blurVolumeObject == null)
        {
            blurVolumeObject = GameObject.Find(blurObjectName);
            
            if (blurVolumeObject == null)
            {
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (GameObject go in allObjects)
                {
                    if (go.scene.isLoaded && go.name == blurObjectName)
                    {
                        blurVolumeObject = go;
                        break;
                    }
                }
            }
        }
    }

    public void OpenInventory()
    {
        if (pauseManager == null) pauseManager = FindFirstObjectByType<Pause>();
        if (playerManager == null) playerManager = FindFirstObjectByType<PlayerManager>();

        if (pauseManager != null)
        {
            pauseManager.SoftPauseFor("Inventory");
        }

        // עדכון השחקנית שהתיק נפתח (מתחיל לאסוף נתונים)
        if (playerManager != null)
        {
            playerManager.SetInventoryOpenState(true);
        }

        EnsureBlurVolumeExists();

        if (blurVolumeObject) blurVolumeObject.SetActive(true);
        background.SetActive(true);
        itemSlotContainer.SetActive(true);
        openInventoryButton.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        isInventoryOpen = true; 
    }

    public void CloseInventory()
    {
        EnsureBlurVolumeExists();

        if (blurVolumeObject) blurVolumeObject.SetActive(false);
        background.SetActive(false);
        itemSlotContainer.SetActive(false);
        openInventoryButton.SetActive(true);
        
        if (pauseManager == null) pauseManager = FindFirstObjectByType<Pause>();

        if (pauseManager != null)
        {
            pauseManager.SoftResumeFor("Inventory");
        }

        if (playerManager != null)
        {
            playerManager.SetInventoryOpenState(false);
        }

        isInventoryOpen = false; 
    }
}