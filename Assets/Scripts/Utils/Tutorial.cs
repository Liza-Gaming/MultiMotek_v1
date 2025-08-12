using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Tutorial : MonoBehaviour
{
    public Image instructionImage;
    public Sprite[] instructionSprites;
    private int currentIndex = 0;

    void Start()
    {
        // if (instructionSprites.Length > 0)
        //     instructionImage.sprite = instructionSprites[currentIndex];
    }

    public void NextImage()
    {
        if (currentIndex < instructionSprites.Length - 1)
        {
            StartCoroutine(FadeImage());
        }
    }

    IEnumerator FadeImage()
    {
        // Fade out
        for (float i = 1; i >= 0; i -= Time.deltaTime)
        {
            instructionImage.color = new Color(1, 1, 1, i);
            yield return null;
        }

        // Change the image
        currentIndex++;
        instructionImage.sprite = instructionSprites[currentIndex];

        // Fade in
        for (float i = 0; i <= 1; i += Time.deltaTime)
        {
            instructionImage.color = new Color(1, 1, 1, i);
            yield return null;
        }
    }
}
