using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SugarBlurController : MonoBehaviour
{
    private static SugarBlurController _instance;
    void Awake() {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Refs")]
    [SerializeField] private Volume volume;
    [SerializeField] private SugarMeter sugarMeter;

    [Header("Sugar → Focus mapping")]
    public float blurStartSugar = 60f;
    public float maxBlurAtSugar = 30f;
    public float blurEndSugar   = 20f;
    [Range(0f, 1f)] public float overallStrength = 1f;

    [Header("Blur Levels Configuration")]
    public BlurTransitionType transitionType = BlurTransitionType.Smooth;
    [Range(2, 10)] public int discreteSteps = 4;
    [Range(0.5f, 3f)] public float blurCurvePower = 1.5f;

    [Header("Bokeh Settings")]
    public float sharpFocusDistance = 5f;
    public float blurredFocusDistance = 0.3f;
    public float sharpAperture = 16f;
    public float blurredAperture = 1.2f;
    public float sharpFocalLength = 35f;
    public float blurredFocalLength = 85f;

    [Header("Bokeh Blades")]
    public int bladeCount = 5;
    [Range(0f, 1f)] public float bladeCurvature = 0.2f;
    [Range(0f, 180f)] public float bladeRotation = 0f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    public enum BlurTransitionType { Smooth, StepLevels, Curved, InverseCurved }

    private DepthOfField dof;
    private float _nextRetryTime;

    private Camera _activeCam;

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRebindAll(force:true);
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m) {
        TryRebindAll(force:true);
    }
    
    private void OnDestroy() {
        if (_instance == this) _instance = null;
    }


    private void TryRebindAll(bool force = false)
    {
        EnsureActiveCamera(force);
        
        if (force || sugarMeter == null)
            sugarMeter = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>(FindObjectsInactive.Include);
        
        EnsureCameraPostFX();
        
        EnsureLocalVolumeOnActiveCamera();
        
        dof = null;
        if (volume != null) {
            if (volume.profile == null)
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            if (!volume.profile.TryGet(out dof))
                dof = volume.profile.Add<DepthOfField>(true);
        }
    }

    private void Update()
    {
        if (_activeCam == null || volume == null || dof == null || sugarMeter == null) {
            if (Time.unscaledTime >= _nextRetryTime) {
                TryRebindAll();               // ריטרי עדין
                _nextRetryTime = Time.unscaledTime + 0.5f;
            }
            return;
        }

        float sugar = sugarMeter.GetSugarLevel();

        if (sugar >= blurStartSugar) {
            dof.active = false;
            if (showDebugInfo) Debug.Log($"Sugar {sugar:F1} - DOF OFF (Sharp)");
            return;
        }

        dof.active = true;
        dof.mode.value = DepthOfFieldMode.Bokeh;

        float t = Mathf.InverseLerp(maxBlurAtSugar, blurStartSugar, sugar);
        t = Mathf.Clamp01(t);

        float blurIntensity = CalculateBlurIntensity(t);
        blurIntensity = Mathf.Lerp(0f, blurIntensity, overallStrength);

        dof.focusDistance.value = Mathf.Lerp(blurredFocusDistance, sharpFocusDistance, blurIntensity);
        dof.aperture.value      = Mathf.Lerp(blurredAperture,      sharpAperture,      blurIntensity);
        dof.focalLength.value   = Mathf.Lerp(blurredFocalLength,   sharpFocalLength,   blurIntensity);

        if (dof.bladeCount != null)        dof.bladeCount.value = bladeCount;
        if (dof.bladeCurvature != null)    dof.bladeCurvature.value = bladeCurvature;
        if (dof.bladeRotation != null)     dof.bladeRotation.value = bladeRotation;
        if (dof.highQualitySampling != null) dof.highQualitySampling.value = true;
    }

    private float CalculateBlurIntensity(float normalizedSugar)
    {
        switch (transitionType)
        {
            case BlurTransitionType.Smooth:        return normalizedSugar;
            case BlurTransitionType.StepLevels:    return Mathf.Floor(normalizedSugar * discreteSteps) / discreteSteps;
            case BlurTransitionType.Curved:        return Mathf.Pow(normalizedSugar, 1f / blurCurvePower);
            case BlurTransitionType.InverseCurved: return Mathf.Pow(normalizedSugar, blurCurvePower);
            default: return normalizedSugar;
        }
    }
    
    private void EnsureActiveCamera(bool force)
    {
        if (!force && _activeCam != null && _activeCam.isActiveAndEnabled) return;
        
        Camera cam = Camera.main;
        if (cam == null) {
            var all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].isActiveAndEnabled) { cam = all[i]; break; }
        }

        _activeCam = cam;
    }

    private void EnsureCameraPostFX()
    {
        if (_activeCam == null) return;

        var uac = _activeCam.GetComponent<UniversalAdditionalCameraData>();
        if (!uac) uac = _activeCam.gameObject.AddComponent<UniversalAdditionalCameraData>();

        uac.renderPostProcessing = true;
        uac.volumeLayerMask = ~0; // Everything
        uac.volumeTrigger   = _activeCam.transform;
    }

    private void EnsureLocalVolumeOnActiveCamera()
    {
        if (volume == null)
        {
            var t = transform.Find("SugarBlurVolume");
            if (t) volume = t.GetComponent<Volume>();
        }
        
        if (volume == null)
        {
            var go = new GameObject("SugarBlurVolume");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 999f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
            Debug.Log("Created new Volume for SugarBlurController");
        }
    
        // ודא שה-Volume פעיל
        if (!volume.enabled) volume.enabled = true;
    }
    
    public void ResetToDefault()
    {
        dof = null;
        volume = null;
        sugarMeter = null;
        
        TryRebindAll(force: true);
    
        Debug.Log("SugarBlurController - Reset to default state");
    }

    private void OnValidate()
    {
        if (maxBlurAtSugar >= blurStartSugar) maxBlurAtSugar = blurStartSugar - 1f;
        if (blurEndSugar   >= maxBlurAtSugar) blurEndSugar   = maxBlurAtSugar - 1f;
        overallStrength = Mathf.Clamp01(overallStrength);
    }
}
