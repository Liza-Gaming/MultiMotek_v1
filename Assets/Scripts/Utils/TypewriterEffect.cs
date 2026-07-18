using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TypewriterEffect : MonoBehaviour
{
    public Text uiText;
    
    [TextArea(3, 5)]
    public string fullText = "טקסט לדוגמה...";
    
    public float delayBeforeStart = 0.5f;
    public float timePerCharacter = 0.05f;

    private void Awake()
    {
        if (uiText == null) uiText = GetComponent<Text>();
        uiText.text = ""; 
    }
    
    public IEnumerator PlayTypewriter()
    {
        uiText.text = "";
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