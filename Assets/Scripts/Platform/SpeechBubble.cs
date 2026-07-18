using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class SpeechBubble : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup bubbleCanvasGroup;
    
    [SerializeField] private Text bubbleText; 

    [Header("Bubble Settings")]
    [TextArea(2, 4)]
    [SerializeField] private string sentenceToShow;
    [SerializeField] private float fadeDuration = 1.5f;

    private bool isTriggered = false;

    private void Start()
    {
        if (bubbleCanvasGroup != null)
        {
            bubbleCanvasGroup.alpha = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isTriggered && other.CompareTag("Player"))
        {
            isTriggered = true;
            
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

        bubbleCanvasGroup.alpha = 1f;
    }
}