using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(100000)]
public class Level4HideOnlyTimerUI : MonoBehaviour
{
    [Tooltip("אם ריק – ניקח את ה-GameObject שעליו יושב TimerUIBinder")]
    [SerializeField] private GameObject timerUiRootOverride;

    void Start()
    {
        StartCoroutine(ForceHideTimerOnly());
    }

    IEnumerator ForceHideTimerOnly()
    {
        // כמה פריימים כדי לנצח בניות/הדלקות מאוחרות
        for (int i = 0; i < 5; i++)
        {
            ApplyOnce();
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        ApplyOnce();
    }

    void ApplyOnce()
    {
        // מוצאים את כל הביינדרים (גם אם אובייקט לא אקטיבי)
#if UNITY_2023_1_OR_NEWER
        var binders = Object.FindObjectsByType<TimerUIBinder>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var binders = Resources.FindObjectsOfTypeAll<TimerUIBinder>();
#endif
        foreach (var b in binders)
        {
            if (!b) continue;

            // נקודת השורש *רק של הטיימר*:
            var root = timerUiRootOverride ? timerUiRootOverride : b.gameObject;

            // לא מטפסים ל-Canvas למעלה! עובדים מקומית בלבד:
            var cg = root.GetComponent<CanvasGroup>();
            if (!cg) cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // מכבים תצוגה רק מתחת לשורש הטיימר
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                g.enabled = false;

            // אם יש לטיימר גם Renderers (נדיר ב-UI), נכבה מקומית
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            // שלא יחזיר את ה-UI ב-Start/OnEnable:
            b.enabled = false;
        }
    }
}