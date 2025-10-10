using UnityEngine;
using System;
using System.Collections;

public class EnemyStartTreadmill : MonoBehaviour
{

    public event Action<float> TreadmillStartedGameSeconds;
    public event Action TreadmillEnded;

    [SerializeField] private GameObject treadmillPrefab;
    [SerializeField] private Transform treadmillSpawnPoint;
    [SerializeField] private float durationGameMinutes = 20f;
    [SerializeField] private float cooldownRealSeconds = 5f;
    [SerializeField] private string treadmillRunBool = "IsTreadmillRunning";
    [SerializeField] private float sugarDecreaseanount = -20f;
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
        
        spawnedTreadmill = Instantiate(treadmillPrefab,
                                       treadmillSpawnPoint.position,
                                       treadmillSpawnPoint.rotation);

        var station  = spawnedTreadmill.GetComponent<TreadmillStation>();
        Transform runPoint = (station && station.runPoint) ? station.runPoint : spawnedTreadmill.transform;
        
        var mover    = playerObj.GetComponent<PlayerMover>();
        var animator = playerObj.GetComponent<Animator>();

        if (mover) mover.SetInputLocked(true);
        playerObj.transform.position = runPoint.position;
        
        var p = playerObj.transform;
        p.localScale = new Vector3(Mathf.Abs(p.localScale.x), p.localScale.y, p.localScale.z);
        
        if (!string.IsNullOrEmpty(treadmillRunBool) && animator)
            animator.SetBool(treadmillRunBool, true);
        
        float totalGameSeconds = durationGameMinutes * 60f;
        TreadmillStartedGameSeconds?.Invoke(totalGameSeconds);
        
        float waitRealSeconds = GameTime.GameMinutesToRealSeconds(durationGameMinutes);
        yield return new WaitForSeconds(waitRealSeconds);
        
        if (!string.IsNullOrEmpty(treadmillRunBool) && animator)
            animator.SetBool(treadmillRunBool, false);

        if (spawnedTreadmill) Destroy(spawnedTreadmill);
        if (mover) mover.SetInputLocked(false);
        
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm != null)
        {
            sm.ScheduleEffectGame(sugarDecreaseanount, durationGameMin: 120f, entryGameMin: 0f);
        }
        
        TreadmillEnded?.Invoke();

        // ---- START COOLDOWN EVENT ----
        inCooldown = true;
        CooldownStartedRealSeconds?.Invoke(cooldownRealSeconds);
        
        // yield return new WaitForSecondsRealtime(cooldownRealSeconds);
        
        yield return new WaitForSeconds(cooldownRealSeconds);

        inCooldown = false;
        isRunning = false;
    }
}
