using UnityEngine;
using UnityEngine.SceneManagement;

public class InventoryUIController : MonoBehaviour
{
    public GameObject background;
    public GameObject itemSlotContainer;
    public GameObject openInventoryButton;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject blurVolumeObject;
    
    [Tooltip("הכניסי כאן את השם המדויק של אובייקט הבלור כפי שהוא מופיע בהיררכיה")]
    [SerializeField] private string blurObjectName = "PauseBlur"; // תשני את זה לשם האמיתי של האובייקט שלך
    
    private Pause pauseManager;

    void Awake()
    {
        // החלפתי ל-FindFirstObjectByType כי זה יעיל יותר
        pauseManager = FindFirstObjectByType<Pause>(); 
        background.SetActive(false);
        itemSlotContainer.SetActive(false);
        openInventoryButton.SetActive(true);
    }

    private void EnsureBlurVolumeExists()
    {
        if (blurVolumeObject == null)
        {
            // ניסיון ראשון: חיפוש רגיל (מוצא רק אובייקטים דולקים)
            blurVolumeObject = GameObject.Find(blurObjectName);
            
            // ניסיון שני: אם האובייקט לא נמצא, הוא כנראה כבוי
            if (blurVolumeObject == null)
            {
                // פקודה זו שולפת את כל האובייקטים בזיכרון, כולל הכבויים
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (GameObject go in allObjects)
                {
                    // מוודאים שהאובייקט ממש קיים בסצנה הפעילה (ולא סתם פריפאב בתיקיות למטה) והשם תואם
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

        if (pauseManager != null)
        {
            pauseManager.SoftPauseFor("Inventory");
        }

        EnsureBlurVolumeExists(); // מוודאים שהבלור מחובר

        if (blurVolumeObject) blurVolumeObject.SetActive(true);
        background.SetActive(true);
        itemSlotContainer.SetActive(true);
        openInventoryButton.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseInventory()
    {
        EnsureBlurVolumeExists(); // מוודאים שהבלור מחובר

        if (blurVolumeObject) blurVolumeObject.SetActive(false);
        background.SetActive(false);
        itemSlotContainer.SetActive(false);
        openInventoryButton.SetActive(true);
        
        if (pauseManager == null) pauseManager = FindFirstObjectByType<Pause>();

        if (pauseManager != null)
        {
            pauseManager.SoftResumeFor("Inventory");
        }
    }
}