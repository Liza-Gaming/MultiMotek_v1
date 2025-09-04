using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

public class SugarBlurController : MonoBehaviour
{
    [Header("Refs")]
    public PostProcessVolume volume;      // אפשר להשאיר ריק – נמלא אוטומטית
    public SugarMeter sugarMeter;         // גם את זה נחפש אם ריק

    [Header("Blur Control")]
    public float blurStartSugar = 90f;    // מעל ערך זה – חד
    public float blurEndSugar   = 60f;    // מתחת ערך זה – מטושטש
    public float minFocusDistance = 0.1f; // יותר קטן = יותר טשטוש
    public float maxFocusDistance = 3f;   // יותר גדול = פחות טשטוש

    private DepthOfField dof;
    private float _nextRetryTime;

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRebindAll(force:true);
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) {
        TryRebindAll(force:true);
    }

    void TryRebindAll(bool force=false) {
        if (force || sugarMeter == null)
            sugarMeter = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>(FindObjectsInactive.Include);
        
        if (force || volume == null) {
            volume = GetComponent<PostProcessVolume>();
            if (volume == null) {
                foreach (var v in FindObjectsByType<PostProcessVolume>(FindObjectsSortMode.None)) {
                    if (v != null && (v.isGlobal || v.GetComponentInParent<Camera>() != null)) {
                        volume = v; break;
                    }
                }
            }
        }
        
        dof = null;
        if (volume != null && volume.profile != null) {
            volume.profile.TryGetSettings(out dof);
        }
    }

    void Update()
    {
        if (dof == null || sugarMeter == null || volume == null) {
            if (Time.unscaledTime >= _nextRetryTime) {
                TryRebindAll();
                _nextRetryTime = Time.unscaledTime + 0.5f;
            }
            return;
        }

        float sugar = sugarMeter.GetSugarLevel();
        
        if (Mathf.Approximately(blurStartSugar, blurEndSugar)) {
            dof.focusDistance.value = (sugar >= blurStartSugar) ? maxFocusDistance : minFocusDistance;
            return;
        }
        
        float t = Mathf.InverseLerp(blurEndSugar, blurStartSugar, sugar);
        float targetFocus = Mathf.Lerp(minFocusDistance, maxFocusDistance, t);
        dof.focusDistance.value = targetFocus;
    }
}
