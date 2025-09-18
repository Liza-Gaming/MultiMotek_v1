using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSkinSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController baseController;       // המקורי (אם ריק – יילקח אוטומטית מה-Animator)
    [SerializeField] private AnimatorOverrideController stage3Override;      // ה-AOC שיצרת

    [Header("Config")]
    [SerializeField] private int stageBuildIndexToSwitch = 3;                // אינדקס בנייה של "שלב 3"
    [SerializeField] private string stateToReenterAfterSwap = "Idle";        // שם state שקיים בשני הקונטרולרים

    private bool hasSwitchedThisScene = false;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!baseController) baseController = animator.runtimeAnimatorController;

        // נוודא שמכל טעינת סצנה אנחנו במצב הנכון
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

    // לקריאה גם מתוך לוגיקה של "הגעתי לשלב 3" במהלך משחק רציף
    public void SwitchToStage3Now()
    {
        if (hasSwitchedThisScene || stage3Override == null) return;

        // שומרות רציפות בזמן האנימציה כדי שלא יהיה "קפיצה" חזקה
        var st = animator.GetCurrentAnimatorStateInfo(0);
        float t = st.normalizedTime % 1f;

        animator.runtimeAnimatorController = stage3Override;
        hasSwitchedThisScene = true;

        // כניסה מחדש ל-Idle (או לכל state ששמו זהה בשני הקונטרולרים)
        if (!string.IsNullOrEmpty(stateToReenterAfterSwap))
            animator.Play(stateToReenterAfterSwap, 0, t);
    }
}