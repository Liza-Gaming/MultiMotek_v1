using UnityEngine;
using System;
using System.Collections;

public class EnemyStartTreadmill : MonoBehaviour
{
    // ← אירועים פר מופע (instance) כדי שטיימר צמוד למאמן ישמע רק את המאמן שלו
    public event Action<float> TreadmillStartedGameSeconds; // param: total game-seconds (לרוב 20*60)
    public event Action TreadmillEnded;

    [SerializeField] private GameObject treadmillPrefab;
    [SerializeField] private Transform treadmillSpawnPoint;
    [SerializeField] private float durationGameMinutes = 20f;
    [SerializeField] private float cooldownRealSeconds = 5f;
    [SerializeField] private string treadmillRunBool = "IsTreadmillRunning";
    
    public event Action<float> CooldownStartedRealSeconds; 

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

        // ---- מודיעים לטיימר להתחיל לרדת מ-20:00 לפי זמן-משחק ----
        float totalGameSeconds = durationGameMinutes * 60f;
        TreadmillStartedGameSeconds?.Invoke(totalGameSeconds);

        // המתנה 20 דקות "משחק" (המרה לשניות אמת)
        float waitRealSeconds = GameTime.GameMinutesToRealSeconds(durationGameMinutes);
        yield return new WaitForSeconds(waitRealSeconds);

        // סיום
        if (!string.IsNullOrEmpty(treadmillRunBool) && animator)
            animator.SetBool(treadmillRunBool, false);

        if (spawnedTreadmill) Destroy(spawnedTreadmill);
        if (mover) mover.SetInputLocked(false);

        // אפקט סוכר (כמו שהיה)
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.DecreaseSugarGame(20f, durationGameMin: 120f, delayGameMin: 0f, suppressBaselineDuring: true);
        }
        
        TreadmillEnded?.Invoke();

        // ---- START COOLDOWN EVENT ----
        inCooldown = true;
        CooldownStartedRealSeconds?.Invoke(cooldownRealSeconds);

        // אם את רוצה שהקירור יתקתק גם כשהמשחק בפאוז:
        // yield return new WaitForSecondsRealtime(cooldownRealSeconds);

        // אם את מעדיפה שהקירור יעצור בפאוז (כמו שהיה):
        yield return new WaitForSeconds(cooldownRealSeconds);

        inCooldown = false;
        isRunning = false;
    }
}
