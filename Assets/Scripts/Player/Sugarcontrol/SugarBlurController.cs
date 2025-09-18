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
    [Tooltip("סוכר מעל זה = חד לחלוטין")]
    public float blurStartSugar = 60f;
    
    public float maxBlurAtSugar = 30f; 
    
    [Tooltip("סוכר מתחת לזה = מטושטש לחלוטין")]
    public float blurEndSugar = 20f;
    
    [Range(0f, 1f)]
    public float overallStrength = 1f;

    [Header("Blur Levels Configuration")]
    [Tooltip("בחר סוג המעבר בין רמות הטשטוש")]
    public BlurTransitionType transitionType = BlurTransitionType.Smooth;
    
    [Tooltip("מספר רמות דיסקרטיות (עבור Step Levels)")]
    [Range(2, 10)]
    public int discreteSteps = 4;
    
    [Tooltip("עוצמת העקומה - ערכים גבוהים = טשטוש מהיר יותר בהתחלה")]
    [Range(0.5f, 3f)]
    public float blurCurvePower = 1.5f;

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
    
    public enum BlurTransitionType
    {
        Smooth,      // מעבר חלק
        StepLevels,  // רמות דיסקרטיות
        Curved,      // עקומה (מהיר בהתחלה, איטי בסוף)
        InverseCurved // איטי בהתחלה, מהיר בסוף
    }

    private DepthOfField dof;
    private float _nextRetryTime;

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

    private void TryRebindAll(bool force = false)
    {
        // 1) ודאי שיש SugarMeter
        if (force || sugarMeter == null)
            sugarMeter = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>(FindObjectsInactive.Include);

        // 2) ודאי שלמצלמה מופעל Post Processing ושה-VolumeMask נכון
        EnsureCameraPostFX();

        // 3) השתמשי תמיד ב-Volume פרטי על המצלמה (ניצור אם אין)
        EnsureLocalVolumeOnCamera();

        // 4) ודאי שיש DoF בפרופיל
        dof = null;
        if (volume != null)
        {
            if (volume.profile == null)
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            if (!volume.profile.TryGet(out dof))
                dof = volume.profile.Add<DepthOfField>(true);
        }
    }


    private void Update()
    {
        if (dof == null || sugarMeter == null || volume == null) {
            if (Time.unscaledTime >= _nextRetryTime) {
                TryRebindAll();
                _nextRetryTime = Time.unscaledTime + 0.5f;
            }
            return;
        }

        float sugar = sugarMeter.GetSugarLevel();
        
        if (sugar >= blurStartSugar) {
            dof.active = false;
            if (showDebugInfo) {
                Debug.Log($"Sugar {sugar:F1} - DOF OFF (Sharp)");
            }
            return;
        }

        dof.active = true;
        dof.mode.value = DepthOfFieldMode.Bokeh;

        // חישוב עוצמת הטשטוש בהתבסס על סוג המעבר
        float t = Mathf.InverseLerp(maxBlurAtSugar, blurStartSugar, sugar);
        t = Mathf.Clamp01(t);

// אם את משתמשת ב-Step/Curved וכו'
        float blurIntensity = CalculateBlurIntensity(t);

// אופציונלי: להחליש את ההשפעה הכללית כדי שלא "יקפוץ" מהר
        blurIntensity = Mathf.Lerp(0f, blurIntensity, overallStrength);

// מיפוי הערכים (אותו דבר כמו אצלך)
        float focusDist = Mathf.Lerp(blurredFocusDistance, sharpFocusDistance, blurIntensity);
        float aperture  = Mathf.Lerp(blurredAperture,      sharpAperture,      blurIntensity);
        float focalLen  = Mathf.Lerp(blurredFocalLength,    sharpFocalLength,   blurIntensity);

        dof.focusDistance.value = focusDist;
        dof.aperture.value      = aperture;
        dof.focalLength.value   = focalLen;

        // Blades
        if (dof.bladeCount != null) dof.bladeCount.value = bladeCount;
        if (dof.bladeCurvature != null) dof.bladeCurvature.value = bladeCurvature;
        if (dof.bladeRotation != null) dof.bladeRotation.value = bladeRotation;
        if (dof.highQualitySampling != null) dof.highQualitySampling.value = true;
    }
    

    private float CalculateBlurIntensity(float normalizedSugar)
    {
        switch (transitionType)
        {
            case BlurTransitionType.Smooth:
                return normalizedSugar;

            case BlurTransitionType.StepLevels:
                // יוצר רמות דיסקרטיות
                float stepSize = 1f / discreteSteps;
                float stepLevel = Mathf.Floor(normalizedSugar / stepSize) * stepSize;
                return stepLevel;

            case BlurTransitionType.Curved:
                // עקומה - טשטוש מהיר בהתחלה
                return Mathf.Pow(normalizedSugar, 1f / blurCurvePower);

            case BlurTransitionType.InverseCurved:
                // עקומה הפוכה - טשטוש איטי בהתחלה
                return Mathf.Pow(normalizedSugar, blurCurvePower);

            default:
                return normalizedSugar;
        }
    }

    // פונקציה לבדיקה בעורך
    private void OnValidate()
    {
        // blurStartSugar > maxBlurAtSugar >= blurEndSugar
        if (maxBlurAtSugar >= blurStartSugar) maxBlurAtSugar = blurStartSugar - 1f;
        if (blurEndSugar   >= maxBlurAtSugar) blurEndSugar   = maxBlurAtSugar - 1f;

        if (overallStrength < 0f) overallStrength = 0f;
        if (overallStrength > 1f) overallStrength = 1f;
    }


    // גם אפשר להוסיף פונקציה ציבורית להגדרת רמות מותאמות אישית
    [System.Serializable]
    public struct BlurLevel
    {
        public float sugarThreshold;
        public float focusDistance;
        public float aperture;
        public float focalLength;
    }

    [Header("Custom Blur Levels (Optional)")]
    public bool useCustomLevels = false;
    public BlurLevel[] customBlurLevels;

    private float CalculateCustomBlurIntensity(float sugar)
    {
        if (!useCustomLevels || customBlurLevels == null || customBlurLevels.Length == 0)
            return CalculateBlurIntensity(Mathf.InverseLerp(blurEndSugar, blurStartSugar, sugar));

        // מצא את הרמה המתאימה
        for (int i = 0; i < customBlurLevels.Length; i++)
        {
            if (sugar >= customBlurLevels[i].sugarThreshold)
            {
                // אם זה הרמה הראשונה או האחרונה, השתמש בערכים הישירים
                if (i == 0 || i == customBlurLevels.Length - 1)
                {
                    SetCustomBlurValues(customBlurLevels[i]);
                    return 0; // לא משנה כי אנחנו מגדירים ישירות
                }
                
                // אחרת, עשה אינטרפולציה בין שתי הרמות
                var currentLevel = customBlurLevels[i];
                var nextLevel = customBlurLevels[i - 1];
                
                float t = Mathf.InverseLerp(currentLevel.sugarThreshold, nextLevel.sugarThreshold, sugar);
                
                dof.focusDistance.value = Mathf.Lerp(currentLevel.focusDistance, nextLevel.focusDistance, t);
                dof.aperture.value = Mathf.Lerp(currentLevel.aperture, nextLevel.aperture, t);
                dof.focalLength.value = Mathf.Lerp(currentLevel.focalLength, nextLevel.focalLength, t);
                
                return 0;
            }
        }
        
        // אם לא מצאנו רמה מתאימה, השתמש ברמה האחרונה
        SetCustomBlurValues(customBlurLevels[customBlurLevels.Length - 1]);
        return 0;
    }
    
    private void EnsureCameraPostFX()
    {
        var cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) return;

        var uac = cam.GetComponent<UniversalAdditionalCameraData>();
        if (!uac) uac = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

        // קריטי ב-URP כדי לראות אפקטים
        uac.renderPostProcessing = true;

        // תני למסכה לכלול את כל הליירים (או התאימי ללייר של ה-Volume שלך)
        uac.volumeLayerMask = ~0;        // Everything
        uac.volumeTrigger   = cam.transform;
    }

    private void EnsureLocalVolumeOnCamera()
    {
        // אם כבר הוגדר ידנית בסריאלייזד שדה – השתמשי בו
        if (volume != null) return;

        // נסי למצוא ילד בשם קבוע
        var t = transform.Find("SugarBlurVolume");
        if (t) volume = t.GetComponent<Volume>();
        if (volume != null) return;

        // צרי Volume גלובלי פרטי על המצלמה (נשמר כי המצלמה נשמרת)
        var go = new GameObject("SugarBlurVolume");
        go.transform.SetParent(transform, false);
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 999f;
        volume.profile  = ScriptableObject.CreateInstance<VolumeProfile>();
    }

    private void SetCustomBlurValues(BlurLevel level)
    {
        dof.focusDistance.value = level.focusDistance;
        dof.aperture.value = level.aperture;
        dof.focalLength.value = level.focalLength;
    }
}