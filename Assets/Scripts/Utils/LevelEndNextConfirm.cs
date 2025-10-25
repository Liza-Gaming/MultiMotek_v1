using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelEndNextConfirm : MonoBehaviour
{
    [Header("Stats End UI")]
    [SerializeField] private Button nextLevelButton;          // הכפתור שעל פופאפ הסטטיסטיקות
    [SerializeField] private GameObject confirmPanel;         // פאנל אימות יחיד
    [SerializeField] private Button confirmContinueButton;    // "המשך" בתוך הפאנל
    [SerializeField] private GameObject statasPanel;
    
    [Header("Fade")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Next Level Target")]
    [Tooltip("אם true – נטען את הסצנה הבאה לפי Build Index. אם false – נשתמש בשם מפורש.")]
    [SerializeField] private bool loadNextByBuildIndex = true;
    [Tooltip("כאשר loadNextByBuildIndex=false – זה שם הסצנה לטעינה (למשל \"Level 3\").")]
    [SerializeField] private string explicitNextSceneName = "";

    [Header("Progress / PlayerPrefs")]
    [Tooltip("לעדכן את UnlockedLevel בסיום, בהתאם לשלב הבא")]
    [SerializeField] private bool updateUnlockedLevel = true;
    [Tooltip("אם את קוראת לשלבים שלך \"Level 1\", \"Level 2\"... אפשר לעדכן לפי Build Index+1")]
    [SerializeField] private bool unlockedLevelMatchesBuildNumbering = true;

    private CanvasGroup _cg;
    private bool _isFading;

    private void Awake()
    {
        // חיווט כפתורים
        if (nextLevelButton) {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OpenConfirmPanel);
        }
        if (confirmContinueButton) {
            confirmContinueButton.onClick.RemoveAllListeners();
            confirmContinueButton.onClick.AddListener(ConfirmAndLoad);
        }

        // הכנה לפאנל
        if (confirmPanel)
        {
            _cg = confirmPanel.GetComponent<CanvasGroup>();
            if (!_cg) _cg = confirmPanel.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _cg.interactable = false;
            _cg.blocksRaycasts = false;
            confirmPanel.SetActive(false);
        }
    }

    private void OpenConfirmPanel()
    {
        if (_isFading || !confirmPanel || !_cg) return;
        statasPanel.SetActive(false);
        confirmPanel.SetActive(true);
        StartCoroutine(FadeCanvasGroup(_cg, 0f, 1f, fadeInDuration, after: () =>
        {
            _cg.interactable = true;
            _cg.blocksRaycasts = true;
        }));
    }
    
    private void ConfirmAndLoad()
    {
        if (_isFading) return;

        // לנעול אינטראקציות ולפייד-אאוט
        if (_cg)
        {
            _cg.interactable = false;
            _cg.blocksRaycasts = false;
            StartCoroutine(FadeCanvasGroup(_cg, 1f, 0f, fadeOutDuration, after: () =>
            {
                if (confirmPanel) confirmPanel.SetActive(false);
                LoadNextLevel();
            }));
        }
        else
        {
            LoadNextLevel();
        }
    }

    private void LoadNextLevel()
    {
        string sceneToLoad;

        if (loadNextByBuildIndex)
        {
            int cur = SceneManager.GetActiveScene().buildIndex;
            int next = cur + 1;

            // עדכון UnlockedLevel אם נדרש
            if (updateUnlockedLevel && unlockedLevelMatchesBuildNumbering)
            {
                int prevUnlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);
                int nextAsLevelNumber = next; // בהנחה ש-Level 1 = BuildIndex 1 וכו'
                if (nextAsLevelNumber > prevUnlocked)
                {
                    PlayerPrefs.SetInt("UnlockedLevel", nextAsLevelNumber);
                    PlayerPrefs.Save();
                }
            }

            // אם אין עוד סצנה – אפשר לטפל כאן (חזרה לתפריט/קרדיטים)
            if (next >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("No next scene in build settings. Staying on current scene.");
                return;
            }

            SceneManager.LoadScene(next);
            return;
        }
        else
        {
            sceneToLoad = explicitNextSceneName.Trim();
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError("Explicit next scene name is empty.");
                return;
            }

            if (updateUnlockedLevel && unlockedLevelMatchesBuildNumbering)
            {
                int levelNum = ExtractLevelNumber(sceneToLoad);
                if (levelNum > 0)
                {
                    int prevUnlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);
                    if (levelNum > prevUnlocked)
                    {
                        PlayerPrefs.SetInt("UnlockedLevel", levelNum);
                        PlayerPrefs.Save();
                    }
                }
            }

            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private int ExtractLevelNumber(string name)
    {

        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsDigit(name[i]))
            {
                string digits = "";
                for (int j = i; j < name.Length; j++)
                {
                    if (char.IsDigit(name[j])) digits += name[j];
                    else break;
                }
                if (int.TryParse(digits, out int n)) return n;
                break;
            }
        }
        return -1;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, System.Action after = null)
    {
        _isFading = true;
        float t = 0f;
        cg.alpha = from;
        if (!cg.gameObject.activeSelf) cg.gameObject.SetActive(true);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // לא מושפע מפאוז
            float k = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        cg.alpha = to;
        _isFading = false;
        after?.Invoke();
    }
    
    void OnDisable()  { ReleaseGameplayLocks(); }
    void OnDestroy()  { ReleaseGameplayLocks(); }

    void ReleaseGameplayLocks() {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        var mover = FindFirstObjectByType<PlayerMover>(FindObjectsInactive.Exclude);
        if (mover) mover.SetInputLocked(false);
    }
}
