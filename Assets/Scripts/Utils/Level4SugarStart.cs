using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class Level4SugarStart : MonoBehaviour
{
    [SerializeField] private float levelStartSugar = 80f;
    [SerializeField] private float uiGraceSeconds  = 1.5f;

    private void Awake()
    {
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.StopAllCoroutines();
            
            // אנחנו כופים את איפוס הסוכר והמגמות תמיד כשנכנסים לשלב הזה
            sm.ForceSetForLevel(levelStartSugar, clearTrends: true);
            Debug.Log($"Level4SugarStart: Set sugar to {levelStartSugar} and cleared all trends.");
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