using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TypewriterEffect : MonoBehaviour
{
    [Header("הגדרות טקסט")]
    public Text uiText;
    
    [TextArea(3, 5)]
    public string fullText = "טקסט לדוגמה...";

    [Header("הגדרות זמן")]
    public float delayBeforeStart = 0.5f;
    public float timePerCharacter = 0.05f;

    private void Awake()
    {
        // מרוקנים את הטקסט מיד כשהאובייקט נוצר, כדי שלא יראו אותו לפני האנימציה
        if (uiText == null) uiText = GetComponent<Text>();
        uiText.text = ""; 
    }

    // פונקציה פומבית שמנהל הפסילות יכול להפעיל ולהמתין לה
    public IEnumerator PlayTypewriter()
    {
        uiText.text = ""; // איפוס למקרה של הפעלה חוזרת
        yield return new WaitForSeconds(delayBeforeStart);

        string currentText = "";
        for (int i = fullText.Length - 1; i >= 0; i--)
        {
            currentText = fullText[i] + currentText;
            uiText.text = currentText;
            yield return new WaitForSeconds(timePerCharacter);
        }
    }
}