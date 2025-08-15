using System.Collections;
using UnityEngine;

public class DoorLevelEnd : MonoBehaviour
{
    [Header("Door animation")]
    public Animator doorAnimator;
    public string openTrigger = "Open";
    public string openStateName = "Open";
    public float fallbackOpenDuration = 0.8f;

    [Header("Summary UI")]
    public SugarSummaryUI summaryUI;
    public string nextSceneName = "Level2";
    public int nextSceneBuildIndex = -1;

    [Header("Player control")]
    public bool lockPlayerDuringOpen = true;

    private bool _used = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
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

        StartCoroutine(OpenThenSummary());
    }

    private IEnumerator OpenThenSummary()
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
        
        if (summaryUI == null) summaryUI = FindObjectOfType<SugarSummaryUI>(true);
        if (summaryUI != null)
        {
            summaryUI.nextSceneName = nextSceneName;
            summaryUI.nextSceneBuildIndex = nextSceneBuildIndex;
            summaryUI.ShowSummary();
        }
    }
}
