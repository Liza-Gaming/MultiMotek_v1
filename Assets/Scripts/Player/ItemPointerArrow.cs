using UnityEngine;

public class ItemPointerArrow : MonoBehaviour
{
    [Header("רפרנסים (חובה)")]
    [Tooltip("האוביקט של הפריט שהשחקן חייב לאסוף. יש לגרור לכאן מההיררכיה.")]
    public Transform targetItem; 

    [Tooltip("האוביקט הויזואלי של החץ (ה-Sprite) שיוסתר ויוצג")]
    public GameObject arrowVisuals;

    [Header("הגדרות תזמון")]
    [Tooltip("כמה שניות (בזמן אמת) לחכות לפני שהחץ יופיע")]
    public float delayBeforeShowing = 15.0f;
    
    [Header("הגדרות כיוון")]
    [Tooltip("תיקון זווית אם החץ לא פונה ימינה (0) כברירת מחדל. אם פונה למעלה, נסה 90-")]
    public float rotationOffset = 0f;

    private float timer = 0f;
    private bool itemCollectedOrMissing = false;
    private bool isInitialized = false;

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
        
        if (arrowVisuals != null)
            arrowVisuals.SetActive(false);

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
        
        if (arrowVisuals != null)
            arrowVisuals.SetActive(false);
        
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
        }


        if (itemCollectedOrMissing)
        {
            if (arrowVisuals != null && arrowVisuals.activeSelf)
                arrowVisuals.SetActive(false);

            return;
        }
        
        timer += Time.deltaTime;
        if (timer < delayBeforeShowing)
            return;
        
        if (arrowVisuals != null)
        {
            if (!arrowVisuals.activeSelf)
                arrowVisuals.SetActive(true);

            Vector3 direction = targetItem.position - transform.position;
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
        timer = 0f;
        
        if (arrowVisuals != null)
            arrowVisuals.SetActive(false);
    }
}