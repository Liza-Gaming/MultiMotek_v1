using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class Level4SugarStart : MonoBehaviour
{
    [SerializeField] private float levelStartSugar = 80f;
    [SerializeField] private float uiGraceSeconds  = 1.5f;
    
    // נוסיף משתנה שמגדיר האם אנחנו רוצים לכפות את הסוכר בכל מקרה
    [Tooltip("אם דלוק - הסוכר תמיד יאופס לערך ההתחלתי. אם כבוי - יאופס רק אם זו הסצנה הראשונה שרצה.")]
    [SerializeField] private bool forceSugarEvenIfContinued = false;

    private void Awake()
    {
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.StopAllCoroutines();
            
            // כאן הבדיקה החשובה: האם הגענו לסצנה הזו במעבר טבעי, או התחלנו אותה מהעורך?
            // נניח לצורך העניין שאם הגענו לכאן ומד הסוכר כבר קיים בזיכרון (יש לו ערך שמור), אנחנו לא נאפס
            bool shouldSetSugar = forceSugarEvenIfContinued;
            
            // אם זו הסצנה הראשונה שנטענת בפרויקט, או אם אין עדיין Instance חי (מה שאומר שרק עכשיו הוא נוצר)
            if (SugarMeter.Instance == null || Time.timeSinceLevelLoad == Time.unscaledTime)
            {
               shouldSetSugar = true;
            }

            if (shouldSetSugar)
            {
                sm.ForceSetForLevel(levelStartSugar, clearTrends: true);
                Debug.Log($"Level4SugarStart: Set sugar to {levelStartSugar}.");
            }
            else
            {
                Debug.Log($"Level4SugarStart: Continued with existing sugar level: {sm.GetSugarLevel()}");
                // אם רוצים עדיין לנקות טרנדים קודמים אבל לשמור על הערך:
                // sm.ForceSetForLevel(sm.GetSugarLevel(), clearTrends: true); 
            }
        }
        else
        {
            Debug.LogWarning("Level4SugarStart: SugarMeter not found in scene.");
        }
        
        var blink = SugarBlinkers.Instance ?? FindFirstObjectByType<SugarBlinkers>();
        if (blink != null)
        {
            blink.HideImmediate();
            blink.SuppressForSeconds(uiGraceSeconds);
        }
    }
}