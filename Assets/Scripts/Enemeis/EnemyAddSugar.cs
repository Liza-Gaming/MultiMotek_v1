using System;
using UnityEngine;

public class EnemyAddSugar : MonoBehaviour, IEnemyEffect
{
    public float sugarAmount = 10f;
    
    [SerializeField] private float EnemyAmount = 16f;
    [SerializeField] private float EnemyDurationGameMin = 180f;
    [SerializeField] private float EnemyDelayGameMin    = 15f;

    public void ApplyEffect(GameObject playerObj)
    {
        if (this.CompareTag("Buffe"))
        {
    
            float totalGameMin = EnemyDelayGameMin + EnemyDurationGameMin;
            float totalRealSec = GameTime.GameMinutesToRealSeconds(totalGameMin);

            var pm = playerObj.GetComponent<PlayerManager>();
            pm?.SuppressSugarArrowRealSeconds(2f);
            
            SugarMeter.Instance?.ScheduleEffectGame(
                EnemyAmount,
                durationGameMin: EnemyDurationGameMin,
                entryGameMin: EnemyDelayGameMin
            );
            return;
        }
        
        if (SugarMeter.Instance != null)
        {
            SugarMeter.Instance.AddSugarGame(sugarAmount);
            GetComponentInChildren<SugarBlinkers>()?.ShowUp(2f);
        }
    }
}