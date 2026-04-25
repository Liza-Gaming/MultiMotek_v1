using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictorySequenceTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string victoryTrigger = "VictoeyEnd";
    public float delayBeforeFade = 3f;
    public float fadeDuration = 2f;

    // הגדרנו אותם כפרטיים כי הסקריפט ימצא אותם לבד
    private Animator animator;
    private Image blackPanel;
    private bool hasTriggered = false;

    private void Start()
    {

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Lose")
                {
                    blackPanel = obj.GetComponent<Image>();
                    return;
                }
            }
        
        if (blackPanel != null)
        {
            blackPanel = blackPanel.GetComponent<Image>();
            
            var c = blackPanel.color;
            c.a = 0f;
            blackPanel.color = c;
            blackPanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("VictorySequenceTrigger: לא נמצא אובייקט בשם 'BlackPanel' בסצנה!");
        }
    }
    
    private void FindBlackPanel()
    {

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Lose")
            {
                blackPanel = obj.GetComponent<Image>();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            animator = other.GetComponent<Animator>();

            if (animator == null) animator = other.GetComponentInChildren<Animator>();

            if (animator != null)
            {
                animator.SetTrigger(victoryTrigger);
            }

            var mover = other.GetComponent<PlayerMover>();
            if (mover != null)
            {
                mover.SetInputLocked(true);
            }

            StartCoroutine(VictoryRoutine());
        }
    }

    private IEnumerator VictoryRoutine()
    {
        yield return new WaitForSeconds(delayBeforeFade);

        if (blackPanel != null)
        {
            blackPanel.gameObject.SetActive(true);
            var c = blackPanel.color;
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / fadeDuration);
                c.a = Mathf.Lerp(0f, 1f, normalized);
                blackPanel.color = c;
                yield return null;
            }
            
            c.a = 1f;
            blackPanel.color = c;
        }
        
        SceneManager.LoadScene(0);
    }
}