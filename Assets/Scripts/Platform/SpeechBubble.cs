using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class SpeechBubble : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("הקנבס גרופ שיושב על הבועה כדי לשלוט בשקיפות שלה")]
    [SerializeField] private CanvasGroup bubbleCanvasGroup;
    
    [Tooltip("טקסט הבועה. אם את משתמשת ב-TextMeshPro, שרשמי TMP_Text במקום")]
    [SerializeField] private Text bubbleText; 

    [Header("Bubble Settings")]
    [TextArea(2, 4)]
    [SerializeField] private string sentenceToShow; // כאן תכתבי את המשפט לכל בועה בנפרד באינספקטור
    [SerializeField] private float fadeDuration = 1.5f;

    private bool isTriggered = false;

    private void Start()
    {
        // מוודאים שהבועה שקופה לחלוטין בתחילת המשחק
        if (bubbleCanvasGroup != null)
        {
            bubbleCanvasGroup.alpha = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // בודקים אם השחקן נכנס לטריגר ושהבועה עדיין לא הופעלה
        if (!isTriggered && other.CompareTag("Player"))
        {
            Debug.Log("השחקן זוהה! מתחיל להציג את הבועה.");
            isTriggered = true;
            
            // מעדכנים את הטקסט למשפט שמוגדר לאובייקט הזה
            if (bubbleText != null)
            {
                bubbleText.text = sentenceToShow;
            }
            
            StartCoroutine(FadeInBubble());
        }
    }

    private IEnumerator FadeInBubble()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            bubbleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        bubbleCanvasGroup.alpha = 1f; // מוודאים שקיבלנו אטימות מלאה בסוף
    }
}