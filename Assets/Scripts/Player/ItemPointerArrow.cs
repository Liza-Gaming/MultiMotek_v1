using UnityEngine;
using UnityEngine.SceneManagement; 

public class ItemPointerArrow : MonoBehaviour
{
    [Header("רפרנסים לפריט (חובה)")]
    [Tooltip("האוביקט של הפריט שהשחקן חייב לאסוף. יש לגרור לכאן מההיררכיה.")]
    public Transform targetItem; 

    [Tooltip("האוביקט הויזואלי של החץ הרגיל (ה-Sprite) שיוסתר ויוצג")]
    public GameObject arrowVisuals;

    [Header("הגדרות שלב 7 (יציאה)")]
    [Tooltip("התגית של דלת היציאה בשלב 7 (הקוד יחפש אובייקט עם תגית זו אוטומטית)")]
    public string exitDoorTag = "ExitDoorLv7";
    
    [Tooltip("האוביקט הויזואלי של חץ היציאה (למשל, חץ בצבע אחר)")]
    public GameObject exitArrowVisuals;
    
    [Tooltip("השם המדויק של שלב 7 כפי שמופיע ב-Build Settings")]
    public string exitLevelName = "Level 7";

    [Header("הגדרות תזמון")]
    [Tooltip("כמה שניות (בזמן אמת) לחכות לפני שהחץ יופיע")]
    public float delayBeforeShowing = 15.0f;
    
    [Header("הגדרות כיוון")]
    [Tooltip("תיקון זווית אם החץ לא פונה ימינה (0) כברירת מחדל. אם פונה למעלה, נסה 90-")]
    public float rotationOffset = 0f;

    private float timer = 0f;
    private bool itemCollectedOrMissing = false;
    private bool isHeadingToExit = false;
    private bool isInitialized = false;
    
    // משתנה פרטי שישמור את הדלת ברגע שהקוד ימצא אותה
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
        // אם הפריט המקורי נעלם (נאסף)
        if (!itemCollectedOrMissing && targetItem == null)
        {
            itemCollectedOrMissing = true;
            
            // בודקים אם אנחנו בשלב 7
            if (SceneManager.GetActiveScene().name == exitLevelName)
            {
                // מחפשים את הדלת בעזרת התגית רק בשלב הזה
                GameObject doorObj = GameObject.FindGameObjectWithTag(exitDoorTag);
                if (doorObj != null)
                {
                    dynamicExitDoor = doorObj.transform;
                    isHeadingToExit = true;
                }
            }
        }

        // אם הפריט נאסף ואנחנו לא בדרך ליציאה (או שאין דלת עם התגית) - פשוט מכבים הכל
        if (itemCollectedOrMissing && !isHeadingToExit)
        {
            if (arrowVisuals != null && arrowVisuals.activeSelf) arrowVisuals.SetActive(false);
            if (exitArrowVisuals != null && exitArrowVisuals.activeSelf) exitArrowVisuals.SetActive(false);
            return;
        }
        
        // הטיימר עובד רק לחץ הראשון (חץ היציאה יופיע מיד לאחר האיסוף)
        if (!isHeadingToExit)
        {
            timer += Time.deltaTime;
            if (timer < delayBeforeShowing) return;
        }
        
        // קביעת המטרה והגרפיקה הנוכחית על בסיס המצב
        Transform currentTarget = isHeadingToExit ? dynamicExitDoor : targetItem;
        GameObject currentVisuals = isHeadingToExit ? exitArrowVisuals : arrowVisuals;
        GameObject otherVisuals = isHeadingToExit ? arrowVisuals : exitArrowVisuals;

        if (currentTarget != null && currentVisuals != null)
        {
            // כיבוי החץ הלא רלוונטי
            if (otherVisuals != null && otherVisuals.activeSelf) 
                otherVisuals.SetActive(false);
            
            // הדלקת החץ הרלוונטי
            if (!currentVisuals.activeSelf) 
                currentVisuals.SetActive(true);

            // חישוב הזווית
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