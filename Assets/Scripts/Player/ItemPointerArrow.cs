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
        // 🔧 וודא שהחץ לא נהרס בין סצנות
        if (transform.parent != null)
        {
            // אם החץ הוא ילד של השחקן, והשחקן DontDestroyOnLoad,
            // הילד יישמר אוטומטית
            // אבל אם יש בעיה, אפשר להוסיף:
            // DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        InitializeArrow();
    }

    void OnEnable()
    {
        // 🔧 אתחול מחדש כשעוברים לסצנה חדשה
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
        // 🔧 איפוס סטטוס כשנכנסים לסצנה חדשה
        timer = 0f;
        itemCollectedOrMissing = false;
        
        if (arrowVisuals != null)
            arrowVisuals.SetActive(false);

        // 🔧 חיפוש מחדש של הפריט בסצנה החדשה
        if (targetItem == null)
        {
            // ננסה למצוא את הפריט לפי Tag או שם
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
        // 1. בדיקה אם הפריט נאסף / נעלם
        if (!itemCollectedOrMissing && targetItem == null)
        {
            itemCollectedOrMissing = true;
        }

        // 2. אם נאסף → להסתיר חץ ולצאת
        if (itemCollectedOrMissing)
        {
            if (arrowVisuals != null && arrowVisuals.activeSelf)
                arrowVisuals.SetActive(false);

            return;
        }

        // 3. טיימר
        timer += Time.deltaTime;
        if (timer < delayBeforeShowing)
            return;

        // 4. הצגה וכיוון חץ
        if (arrowVisuals != null)
        {
            if (!arrowVisuals.activeSelf)
                arrowVisuals.SetActive(true);

            Vector3 direction = targetItem.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 🔧 פיצוי על flip של השחקן (scale.x שלילי)
            if (transform.lossyScale.x < 0f)
            {
                angle += 180f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }
    }

    // 🔧 API חיצוני להגדרת מטרה חדשה
    public void SetTarget(Transform newTarget)
    {
        targetItem = newTarget;
        itemCollectedOrMissing = (newTarget == null);
        timer = 0f;
        
        if (arrowVisuals != null)
            arrowVisuals.SetActive(false);
    }
}