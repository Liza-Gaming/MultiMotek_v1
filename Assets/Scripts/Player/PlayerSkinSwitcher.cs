using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSkinSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController baseController;
    [SerializeField] private AnimatorOverrideController stage3Override;
    [SerializeField] private AnimatorOverrideController stage5Override;

    [Header("Config")]
    [SerializeField] private int stage3BuildIndexToSwitch = 3;
    [SerializeField] private int stage5BuildIndexToSwitch = 5;
    [SerializeField] private string stateToReenterAfterSwap = "Idle";

    private int lastAppliedKey = -1; // -1=base, 3=stage3, 5=stage5

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
        ApplyForBuildIndex(scene.buildIndex);
    }

    private void ApplyForBuildIndex(int buildIndex)
    {

        if (buildIndex >= stage5BuildIndexToSwitch && stage5Override != null)
        {
            SwitchToOverride(stage5Override, 5);
            return;
        }

        if (buildIndex >= stage3BuildIndexToSwitch && stage3Override != null)
        {
            SwitchToOverride(stage3Override, 3);
            return;
        }

        SwitchToBase();
    }

    private void SwitchToOverride(AnimatorOverrideController ov, int key)
    {
        if (ov == null) return;
        if (lastAppliedKey == key) return;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        float t = st.normalizedTime % 1f;

        animator.runtimeAnimatorController = ov;
        lastAppliedKey = key;

        if (!string.IsNullOrEmpty(stateToReenterAfterSwap))
            animator.Play(stateToReenterAfterSwap, 0, t);
    }

    private void SwitchToBase()
    {
        if (lastAppliedKey == -1) return;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        float t = st.normalizedTime % 1f;

        animator.runtimeAnimatorController = baseController;
        lastAppliedKey = -1;

        if (!string.IsNullOrEmpty(stateToReenterAfterSwap))
            animator.Play(stateToReenterAfterSwap, 0, t);
    }

}
