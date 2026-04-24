using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Level8SugarLock : MonoBehaviour
{
    [SerializeField] private float targetSugar = 105f;

    private void Awake()
    {
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.ForceSetForLevel(targetSugar, clearTrends: true);
            sm.enabled = false; 
        }
        
        var blink = SugarBlinkers.Instance ?? FindFirstObjectByType<SugarBlinkers>();
        if (blink != null) blink.HideImmediate();
        
#if UNITY_2023_1_OR_NEWER
        var player = Object.FindFirstObjectByType<PlayerMover>();
#else
        var player = FindObjectOfType<PlayerMover>();
#endif
        if (player != null)
        {
            player.disableMovementSugarDrain = true;
        }

        // ----- התוספת: נעילת השמיים ליום -----
#if UNITY_2023_1_OR_NEWER
        var sky = Object.FindFirstObjectByType<SkySwitcher>();
#else
        var sky = FindObjectOfType<SkySwitcher>();
#endif
        if (sky != null)
        {
            sky.forceDayOverride = true;
        }
        
        Debug.Log($"Level 8: Sugar locked at {targetSugar}, player movement drain disabled, and Sky locked to DAY.");
    }

    private void OnDestroy()
    {
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null) sm.enabled = true;

#if UNITY_2023_1_OR_NEWER
        var player = Object.FindFirstObjectByType<PlayerMover>();
#else
        var player = FindObjectOfType<PlayerMover>();
#endif
        if (player != null)
        {
            player.disableMovementSugarDrain = false;
        }
        
#if UNITY_2023_1_OR_NEWER
        var sky = Object.FindFirstObjectByType<SkySwitcher>();
#else
        var sky = FindObjectOfType<SkySwitcher>();
#endif
        if (sky != null)
        {
            sky.forceDayOverride = false;
        }
    }
}