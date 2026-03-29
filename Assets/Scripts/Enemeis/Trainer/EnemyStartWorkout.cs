using UnityEngine;
using System;
using System.Collections;

public class EnemyStartWorkout : MonoBehaviour
{
    public event Action<float> WorkoutStartedGameSeconds;
    public event Action WorkoutEnded;
    public event Action<float> CooldownStartedRealSeconds;

    [Header("Workout")]
    [SerializeField] private float durationGameMinutes = 30f;
    [SerializeField] private float cooldownRealSeconds = 5f;
    [SerializeField] private string workoutBool = "IsTraining";
    
    [Header("Optional positioning")]
    [SerializeField] private Transform standPoint;

    private bool isRunning = false;
    private bool inCooldown = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isRunning || inCooldown) return;

        StartCoroutine(RunWorkoutRoutine(other.gameObject));
    }

    private IEnumerator RunWorkoutRoutine(GameObject playerObj)
    {
        isRunning = true;

        var mover = playerObj.GetComponent<PlayerMover>();
        var animator = playerObj.GetComponent<Animator>();

        if (mover) mover.SetInputLocked(true);

        if (standPoint != null)
            playerObj.transform.position = standPoint.position;

        var p = playerObj.transform;
        p.localScale = new Vector3(Mathf.Abs(p.localScale.x), p.localScale.y, p.localScale.z);

        if (!string.IsNullOrEmpty(workoutBool) && animator)
            animator.SetBool(workoutBool, true);

        float totalGameSeconds = durationGameMinutes * 60f;
        WorkoutStartedGameSeconds?.Invoke(totalGameSeconds);
        
        ApplyRandomSugarOutcome();

        float waitRealSeconds = GameTime.GameMinutesToRealSeconds(durationGameMinutes);
        yield return new WaitForSeconds(waitRealSeconds);

        if (!string.IsNullOrEmpty(workoutBool) && animator)
            animator.SetBool(workoutBool, false);

        if (mover) mover.SetInputLocked(false);

        WorkoutEnded?.Invoke();

        inCooldown = true;
        CooldownStartedRealSeconds?.Invoke(cooldownRealSeconds);

        yield return new WaitForSeconds(cooldownRealSeconds);

        inCooldown = false;
        isRunning = false;
    }

    private void ApplyRandomSugarOutcome()
    {
        var sm = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        if (sm == null) return;
        
        int scenario = UnityEngine.Random.Range(0, 3);

        switch (scenario)
        {
            case 0:
                sm.ScheduleEffectGame(-60f, durationGameMin: 90f, entryGameMin: 20f);
                break;

            case 1:
                sm.ScheduleEffectGame(150f, durationGameMin: 200f, entryGameMin: 20f);
                break;

            case 2:
                sm.ScheduleEffectGame(150f, durationGameMin: 200f, entryGameMin: 20f);
                sm.ScheduleEffectGame(-60f, durationGameMin: 270f, entryGameMin: 200f);
                break;
        }
    }
}