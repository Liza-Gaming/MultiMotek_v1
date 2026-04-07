using UnityEngine;
using UnityEngine.EventSystems;

public class AudioToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    [Tooltip("האובייקט של קו הביטול / האיקס")]
    [SerializeField] private GameObject muteIcon;

    [Header("Hover Settings")]
    [Tooltip("בכמה להגדיל את הכפתור כשמרחפים (1.1 = 10% יותר גדול)")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f;
    [Tooltip("מהירות האנימציה של הגדילה")]
    [SerializeField] private float scaleSpeed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isMuted = false;

    private void Start()
    {
        // שומרים את הגודל המקורי של הכפתור כדי שנוכל לחזור אליו תמיד
        originalScale = transform.localScale;
        targetScale = originalScale;

        // בודקים מה מצב השמע הכללי של המשחק כרגע ומעדכנים את הכפתור בהתאם
        isMuted = AudioListener.volume == 0f;
        
        if (muteIcon != null)
        {
            muteIcon.SetActive(isMuted);
        }
    }

    private void Update()
    {
        // Vector3.Lerp עושה מעבר חלק ונעים לעין מהגודל הנוכחי לגודל המטרה
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    // הפונקציה הזו מופעלת אוטומטית ברגע שהעכבר נכנס לאזור של התמונה
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    // הפונקציה הזו מופעלת אוטומטית ברגע שהעכבר יוצא מהאזור של התמונה
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    // הפונקציה הזו מופעלת אוטומטית כשיש קליק על התמונה
    public void OnPointerClick(PointerEventData eventData)
    {
        isMuted = !isMuted; // הופכים את מצב ההשתקה (אם היה דלוק עכשיו מכובה ולהפך)
        
        // מדליקים או מכבים את סימן הביטול
        if (muteIcon != null)
        {
            muteIcon.SetActive(isMuted);
        }

        // קסם! הפקודה הזו מכבה או מדליקה את השמע של כל המשחק בבת אחת
        AudioListener.volume = isMuted ? 0f : 1f;
    }
}