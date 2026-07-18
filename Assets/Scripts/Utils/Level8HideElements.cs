using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(100000)]
public class Level8HideElements : MonoBehaviour
{

    [SerializeField] private GameObject timerUiRootOverride;



    void Start()
    {
        StartCoroutine(ForceHideRoutine());
    }

    IEnumerator ForceHideRoutine()
    {

        for (int i = 0; i < 5; i++)
        {
            ApplyUIState(false);
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        ApplyUIState(false);
    }

    void ApplyUIState(bool show)
    {

#if UNITY_2023_1_OR_NEWER
        var binders = Object.FindObjectsByType<TimerUIBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var binders = Resources.FindObjectsOfTypeAll<TimerUIBinder>();
#endif
        foreach (var b in binders)
        {
            if (!b) continue;
            var root = timerUiRootOverride ? timerUiRootOverride : b.gameObject;
            ToggleElement(root, show);
            b.enabled = show;
        }


        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            ToggleElement(sm.gameObject, show);
        }


#if UNITY_2023_1_OR_NEWER
        var extraTargets = Object.FindObjectsByType<UIHideTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var extraTargets = Resources.FindObjectsOfTypeAll<UIHideTarget>();
#endif
        foreach (var target in extraTargets)
        {
            if (target != null) ToggleElement(target.gameObject, show);
        }
    }
    
    void ToggleElement(GameObject obj, bool show)
    {
        var cg = obj.GetComponent<CanvasGroup>();
        if (!cg && !show) cg = obj.AddComponent<CanvasGroup>();
        
        if (cg)
        {
            cg.alpha = show ? 1f : 0f;
            cg.blocksRaycasts = show;
            cg.interactable = show;
        }

        foreach (var g in obj.GetComponentsInChildren<Graphic>(true)) g.enabled = show;
    }

    void OnDestroy()
    {
        ApplyUIState(true);
    }
}