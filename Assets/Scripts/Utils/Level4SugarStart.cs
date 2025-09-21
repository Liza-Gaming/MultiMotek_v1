using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Level4SugarStart : MonoBehaviour
{
    [SerializeField] private float levelStartSugar = 80f;
    [SerializeField] private float uiGraceSeconds  = 1.5f; // כמה זמן להשתיק חיצים

    private void Awake()
    {
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.StopAllCoroutines();
            
            sm.ForceSetForLevel(levelStartSugar, clearTrends: true);
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