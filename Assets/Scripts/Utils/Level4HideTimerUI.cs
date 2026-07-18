using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(100000)]
public class Level4HideOnlyTimerUI : MonoBehaviour
{
    [SerializeField] private GameObject timerUiRootOverride;

    void Start()
    {
        StartCoroutine(ForceHideTimerOnly());
    }

    IEnumerator ForceHideTimerOnly()
    {
        for (int i = 0; i < 5; i++)
        {
            ApplyOnce(false);
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        ApplyOnce(false);
    }
    
    void ApplyOnce(bool show)
    {
#if UNITY_2023_1_OR_NEWER
        var binders = Object.FindObjectsByType<TimerUIBinder>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var binders = Resources.FindObjectsOfTypeAll<TimerUIBinder>();
#endif
        foreach (var b in binders)
        {
            if (!b) continue;
            
            var root = timerUiRootOverride ? timerUiRootOverride : b.gameObject;


            var cg = root.GetComponent<CanvasGroup>();
            if (!cg && !show) cg = root.AddComponent<CanvasGroup>();
            
            if (cg)
            {
                cg.alpha = show ? 1f : 0f;
                cg.blocksRaycasts = show;
                cg.interactable = show;
            }
            
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                g.enabled = show;
            
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.enabled = show;
            
            b.enabled = show;
        }
    }
    
    void OnDestroy()
    {
        ApplyOnce(true);
    }
}