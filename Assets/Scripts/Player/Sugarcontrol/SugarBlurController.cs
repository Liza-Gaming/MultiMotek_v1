using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SugarBlurController : MonoBehaviour
{
    public PostProcessVolume volume;
    public SugarMeter sugarMeter;

    [Header("Blur Control")]
    public float blurStartSugar = 70f;
    public float blurEndSugar = 70f;
    public float minFocusDistance = 0.1f;
    public float maxFocusDistance = 3f;

    private DepthOfField dof;

    void Start()
    {
        if (volume.profile.TryGetSettings(out dof))
        {
            // Got the DOF effect
        }
        else
        {
            Debug.LogWarning("Depth of Field effect not found in the PostProcessVolume profile!");
        }
    }

    void Update()
    {
        if (dof == null || sugarMeter == null)
            return;

        float sugar = sugarMeter.GetSugarLevel();

        if (sugar >= blurStartSugar)
        {
            dof.focusDistance.value = maxFocusDistance;
        }
        else
        {
            float t = Mathf.InverseLerp(blurEndSugar, blurStartSugar, sugar);
            float targetFocus = Mathf.Lerp(minFocusDistance, maxFocusDistance, t);
            dof.focusDistance.value = targetFocus;
        }
    }


}