using UnityEngine;
using UnityEngine.SceneManagement; 

public class ItemPointerArrow : MonoBehaviour
{
    [Header("רפרנסים לפריט (חובה)")]
    public Transform targetItem; 
    
    public GameObject arrowVisuals;
    
    public string exitDoorTag = "ExitDoorLv7";
    
    public GameObject exitArrowVisuals;
    
    public string exitLevelName = "Level 7";
    
    public float delayBeforeShowing = 15.0f;
    
    public float rotationOffset = 0f;

    private float timer = 0f;
    private bool itemCollectedOrMissing = false;
    private bool isHeadingToExit = false;
    private bool isInitialized = false;
    
    private Transform dynamicExitDoor; 

    void Awake()
    {
        if (transform.parent != null)
        {
            // DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        InitializeArrow();
    }

    void OnEnable()
    {
        if (isInitialized)
        {
            ResetArrow();
        }
    }

    private void InitializeArrow()
    {
        isInitialized = true;
        isHeadingToExit = false;
        dynamicExitDoor = null;
        
        if (arrowVisuals != null) arrowVisuals.SetActive(false);
        if (exitArrowVisuals != null) exitArrowVisuals.SetActive(false);

        if (targetItem == null)
        {
            Debug.LogWarning("ItemPointerArrow: לא שויכה מטרת פריט (targetItem). החץ לא יפעל.");
            itemCollectedOrMissing = true;
        }
    }

    private void ResetArrow()
    {
        timer = 0f;
        itemCollectedOrMissing = false;
        isHeadingToExit = false;
        dynamicExitDoor = null;
        
        if (arrowVisuals != null) arrowVisuals.SetActive(false);
        if (exitArrowVisuals != null) exitArrowVisuals.SetActive(false);
        
        if (targetItem == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("RequiredItem");
            if (found != null)
            {
                targetItem = found.transform;
                itemCollectedOrMissing = false;
            }
        }
    }

    void Update()
    {
        if (!itemCollectedOrMissing && targetItem == null)
        {
            itemCollectedOrMissing = true;
            
            if (SceneManager.GetActiveScene().name == exitLevelName)
            {
                GameObject doorObj = GameObject.FindGameObjectWithTag(exitDoorTag);
                if (doorObj != null)
                {
                    dynamicExitDoor = doorObj.transform;
                    isHeadingToExit = true;
                }
            }
        }
        
        if (itemCollectedOrMissing && !isHeadingToExit)
        {
            if (arrowVisuals != null && arrowVisuals.activeSelf) arrowVisuals.SetActive(false);
            if (exitArrowVisuals != null && exitArrowVisuals.activeSelf) exitArrowVisuals.SetActive(false);
            return;
        }
        
        if (!isHeadingToExit)
        {
            timer += Time.deltaTime;
            if (timer < delayBeforeShowing) return;
        }
        
        Transform currentTarget = isHeadingToExit ? dynamicExitDoor : targetItem;
        GameObject currentVisuals = isHeadingToExit ? exitArrowVisuals : arrowVisuals;
        GameObject otherVisuals = isHeadingToExit ? arrowVisuals : exitArrowVisuals;

        if (currentTarget != null && currentVisuals != null)
        {

            if (otherVisuals != null && otherVisuals.activeSelf) 
                otherVisuals.SetActive(false);
            

            if (!currentVisuals.activeSelf) 
                currentVisuals.SetActive(true);


            Vector3 direction = currentTarget.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            if (transform.lossyScale.x < 0f)
            {
                angle += 180f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        targetItem = newTarget;
        itemCollectedOrMissing = (newTarget == null);
        isHeadingToExit = false;
        dynamicExitDoor = null;
        timer = 0f;
        
        if (arrowVisuals != null) arrowVisuals.SetActive(false);
        if (exitArrowVisuals != null) exitArrowVisuals.SetActive(false);
    }
}