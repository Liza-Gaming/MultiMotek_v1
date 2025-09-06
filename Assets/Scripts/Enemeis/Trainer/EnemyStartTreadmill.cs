using UnityEngine;
using System.Collections;

public class EnemyStartTreadmill : MonoBehaviour
{
    [SerializeField] private GameObject treadmillPrefab;
    [SerializeField] private Transform treadmillSpawnPoint;
    [SerializeField] private float durationGameMinutes = 20f;
    [SerializeField] private float cooldownRealSeconds = 5f;
    [SerializeField] private string treadmillRunBool = "IsTreadmillRunning";

    private bool isRunning = false;
    private bool inCooldown = false;
    private GameObject spawnedTreadmill;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isRunning || inCooldown) return;

        StartCoroutine(RunTreadmillRoutine(other.gameObject));
    }

    private IEnumerator RunTreadmillRoutine(GameObject playerObj)
    {
        isRunning = true;

        // יצירת ההליכון
        spawnedTreadmill = Instantiate(treadmillPrefab,
                                       treadmillSpawnPoint.position,
                                       treadmillSpawnPoint.rotation);

        // נקודת ריצה
        var station  = spawnedTreadmill.GetComponent<TreadmillStation>();
        Transform runPoint = (station && station.runPoint) ? station.runPoint : spawnedTreadmill.transform;

        // השחקן
        var mover    = playerObj.GetComponent<PlayerMover>();
        var animator = playerObj.GetComponent<Animator>();

        if (mover) mover.SetInputLocked(true);
        playerObj.transform.position = runPoint.position;

        // פונה שמאלה
        var p = playerObj.transform;
        p.localScale = new Vector3(Mathf.Abs(p.localScale.x), p.localScale.y, p.localScale.z);

        // אנימציה
        if (!string.IsNullOrEmpty(treadmillRunBool) && animator)
            animator.SetBool(treadmillRunBool, true);

        // המתנה 20 דקות במשחק
        float waitRealSeconds = GameTime.GameMinutesToRealSeconds(durationGameMinutes);
        yield return new WaitForSeconds(waitRealSeconds);

        // סיום
        if (!string.IsNullOrEmpty(treadmillRunBool) && animator)
            animator.SetBool(treadmillRunBool, false);

        if (spawnedTreadmill) Destroy(spawnedTreadmill);
        if (mover) mover.SetInputLocked(false);

        // ↓ אפקט הסוכר ↓
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.DecreaseSugarGame(20f, durationGameMin: 120f, delayGameMin: 0f, suppressBaselineDuring: true);
        }

        // cooldown
        inCooldown = true;
        yield return new WaitForSeconds(cooldownRealSeconds);
        inCooldown = false;
        isRunning = false;
    }
}
