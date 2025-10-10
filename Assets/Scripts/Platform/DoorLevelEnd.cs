using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorLevelEnd : MonoBehaviour
{
    [Header("Door animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string openStateName = "Open";
    [SerializeField] private float fallbackOpenDuration = 0.8f;

    [Header("Summary UI")]
    [SerializeField] private SugarSummaryUI summaryUI;

    [Header("Next Scene (override)")]
    [SerializeField] private int nextSceneBuildIndex = -1;
    [SerializeField] private string nextSceneName = "";

    [Header("Player control")]
    [SerializeField] private bool lockPlayerDuringOpen = true;
    [SerializeField] private bool unlockPlayerAfterOpen = false;

    private bool _used = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col is Collider2D) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used || !other.CompareTag("Player")) return;
        _used = true;

        var mover = other.GetComponent<PlayerMover>();
        if (lockPlayerDuringOpen && mover != null)
        {
            mover.SetInputLocked(true);
            var rb = other.attachedRigidbody;
            if (rb) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        UnlockNewLevel();
        StartCoroutine(OpenThenSummary(other));
    }

    private IEnumerator OpenThenSummary(Collider2D player)
    {
        if (doorAnimator != null)
        {
            if (!string.IsNullOrEmpty(openTrigger))
                doorAnimator.SetTrigger(openTrigger);
            else if (!string.IsNullOrEmpty(openStateName))
                doorAnimator.Play(openStateName, 0, 0f);
        }
        
        if (doorAnimator != null && !string.IsNullOrEmpty(openStateName))
        {
            int layer = 0;
            float t = 0f;

            var info = doorAnimator.GetCurrentAnimatorStateInfo(layer);

            while (!info.IsName(openStateName) && t < 2f)
            {
                yield return null;
                t += Time.deltaTime;
                info = doorAnimator.GetCurrentAnimatorStateInfo(layer);
            }

            t = 0f;
            while (info.IsName(openStateName) && info.normalizedTime < 0.99f && t < 5f)
            {
                yield return null;
                t += Time.deltaTime;
                info = doorAnimator.GetCurrentAnimatorStateInfo(layer);
            }
        }
        else
        {
            yield return new WaitForSeconds(fallbackOpenDuration);
        }

        if (unlockPlayerAfterOpen && player != null)
        {
            var mover = player.GetComponent<PlayerMover>();
            if (mover != null) mover.SetInputLocked(false);
        }
        
        if (summaryUI == null) summaryUI = FindObjectOfType<SugarSummaryUI>(true);
        
        ResolveNextScene(out int resolvedIndex, out string resolvedName);

        if (summaryUI != null)
        {
            summaryUI.nextSceneBuildIndex = resolvedIndex;
            summaryUI.nextSceneName = resolvedName;
            summaryUI.ShowSummary();
        }
        else
        {
            if (TryHasScene(resolvedIndex, resolvedName))
            {
                if (resolvedIndex >= 0)
                    SceneManager.LoadScene(resolvedIndex);
                else if (!string.IsNullOrEmpty(resolvedName))
                    SceneManager.LoadScene(resolvedName);
            }
            else
            {
                Debug.LogWarning("[DoorLevelEnd] No SummaryUI and no valid next scene. Staying on current scene.");
            }
        }
    }
    private void ResolveNextScene(out int buildIndex, out string name)
    {
        buildIndex = -1;
        name = "";
        
        if (nextSceneBuildIndex >= 0 && nextSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            buildIndex = nextSceneBuildIndex;
            name = "";
            return;
        }
        
        if (!string.IsNullOrEmpty(nextSceneName) && SceneExistsByName(nextSceneName))
        {
            buildIndex = -1;
            name = nextSceneName;
            return;
        }
        
        int current = SceneManager.GetActiveScene().buildIndex;
        int candidate = current + 1;
        if (candidate >= 0 && candidate < SceneManager.sceneCountInBuildSettings)
        {
            buildIndex = candidate;
            name = "";
            return;
        }
        
        buildIndex = -1;
        name = "";
    }

    private bool TryHasScene(int buildIndex, string name)
    {
        if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings) return true;
        if (!string.IsNullOrEmpty(name) && SceneExistsByName(name)) return true;
        return false;
    }

    private bool SceneExistsByName(string sceneName)
    {

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName == sceneName) return true;
        }
        return false;
    }

    private void UnlockNewLevel()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int reached = PlayerPrefs.GetInt("ReachedIndex", 0);
        if (current >= reached)
        {
            PlayerPrefs.SetInt("ReachedIndex", current + 1);
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 1) + 1);
            PlayerPrefs.Save();
        }
    }
}
