using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Level5ShowTimer : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(RestoreTimerUI());
    }

    IEnumerator RestoreTimerUI()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        
        var binder = FindObjectOfType<TimerUIBinder>(true);
        if (!binder)
        {
            Debug.LogWarning("[Level5ShowTimer] לא נמצא TimerUIBinder!");
            yield break;
        }

        GameObject timerRoot = binder.gameObject;
        
        timerRoot.SetActive(true);

        foreach (Transform child in timerRoot.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.SetActive(true);
        }
        
        foreach (var graphic in timerRoot.GetComponentsInChildren<Graphic>(true))
        {
            graphic.enabled = true;
        }
        
        foreach (var renderer in timerRoot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }
        
        var cg = timerRoot.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
        
        binder.enabled = true;
        
        if (Timer.Instance != null)
        {
            var clockText  = timerRoot.GetComponentInChildren<Text>();
            var background = timerRoot.GetComponentInChildren<Image>();
            Timer.Instance.BindUI(clockText, background);
            
            Timer.Instance.PauseClock(true);
            Timer.Instance.SetTime(7, 0);
            //Timer.Instance.PauseClock(false);
        }

        Debug.Log("[Level5ShowTimer] Timer UI restored successfully!");
    }
}