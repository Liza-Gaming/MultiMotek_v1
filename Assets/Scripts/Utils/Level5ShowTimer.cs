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
        // המתן פריים כדי שהכל ייטען
        yield return null;
        yield return new WaitForEndOfFrame();

        // מצא את ה-TimerUIBinder (על GameObject "Timer")
        var binder = FindObjectOfType<TimerUIBinder>(true); // true = גם אם כבוי
        if (!binder)
        {
            Debug.LogWarning("[Level5ShowTimer] לא נמצא TimerUIBinder!");
            yield break;
        }

        GameObject timerRoot = binder.gameObject;

        // 1. הדלק את האב
        timerRoot.SetActive(true);

        // 2. הדלק את כל הצאצאים
        foreach (Transform child in timerRoot.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.SetActive(true);
        }

        // 3. הדלק את כל ה-Graphics (Image, Text וכו')
        foreach (var graphic in timerRoot.GetComponentsInChildren<Graphic>(true))
        {
            graphic.enabled = true;
        }

        // 4. הדלק Renderers (אם יש)
        foreach (var renderer in timerRoot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }

        // 5. תקן CanvasGroup אם יש
        var cg = timerRoot.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        // 6. הדלק את הבינדר עצמו
        binder.enabled = true;

        // 7. אלץ עדכון של הטיימר
        if (Timer.Instance != null)
        {
            var clockText  = timerRoot.GetComponentInChildren<Text>();
            var background = timerRoot.GetComponentInChildren<Image>();
            Timer.Instance.BindUI(clockText, background);

            // עצירה רגעית אופציונלית כדי למנוע "דליפות זמן" תוך כדי פריימי הטעינה
            Timer.Instance.PauseClock(true);
            Timer.Instance.SetTime(7, 0);    // ← תמיד מתחיל 07:00 בשלב 5
            //Timer.Instance.PauseClock(false);
        }

        Debug.Log("[Level5ShowTimer] Timer UI restored successfully!");
    }
}