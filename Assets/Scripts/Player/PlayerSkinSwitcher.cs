using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSkinSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController baseController;
    [SerializeField] private AnimatorOverrideController stage3Override;

    [Header("Config")]
    [SerializeField] private int stageBuildIndexToSwitch = 3;
    [SerializeField] private string stateToReenterAfterSwap = "Idle";

    private bool hasSwitchedThisScene = false;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!baseController) baseController = animator.runtimeAnimatorController;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hasSwitchedThisScene = false;

        if (scene.buildIndex >= stageBuildIndexToSwitch)
            SwitchToStage3Now();
        else
            animator.runtimeAnimatorController = baseController;
    }
    
    public void SwitchToStage3Now()
    {
        if (hasSwitchedThisScene || stage3Override == null) return;
        
        var st = animator.GetCurrentAnimatorStateInfo(0);
        float t = st.normalizedTime % 1f;

        animator.runtimeAnimatorController = stage3Override;
        hasSwitchedThisScene = true;
        
        if (!string.IsNullOrEmpty(stateToReenterAfterSwap))
            animator.Play(stateToReenterAfterSwap, 0, t);
    }
}