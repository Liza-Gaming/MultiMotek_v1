using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class Level4SugarStart : MonoBehaviour
{
    [SerializeField] private float levelStartSugar = 80f;

    private IEnumerator Start()
    {
        // להמתין פריים כדי ש-SugarMeter יסיים את ה-Start שלו
        yield return null;

        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.ForceSetForLevel(levelStartSugar, clearTrends: true);
        }
        else
        {
            Debug.LogWarning("Level4SugarStart: SugarMeter not found in scene.");
        }
    }
}