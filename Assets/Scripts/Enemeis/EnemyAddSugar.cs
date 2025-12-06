using System;
using UnityEngine;

public class EnemyAddSugar : MonoBehaviour, IEnemyEffect
{
    [Header("Simple enemy hit (instant or short effect)")]
    public float sugarAmount = 10f;
    [SerializeField] private float simpleDurationGameMin = 1f;
    [SerializeField] private Color simpleFloatingColor = Color.yellow;

    [Header("Buffet enemy (delayed + long effect)")]
    [SerializeField] private float EnemyAmount = 16f;
    [SerializeField] private float EnemyDurationGameMin = 180f;
    [SerializeField] private float EnemyDelayGameMin    = 15f;
    [SerializeField] private Color buffetFloatingColor = Color.yellow;

    public void ApplyEffect(GameObject playerObj)
    {
        var pm = playerObj.GetComponent<PlayerManager>();
        if (pm == null)
        {
            Debug.LogWarning("EnemyAddSugar: PlayerManager not found on playerObj.");
            return;
        }
        
        if (this.CompareTag("Buffe"))
        {
          pm.SuppressSugarArrowRealSeconds(0.3f);

            pm.ApplyEnemySugarEffect(
                amountSigned: EnemyAmount,
                durationGameMin: EnemyDurationGameMin,
                floatingColor: buffetFloatingColor,
                entryGameMin: EnemyDelayGameMin,
                floatingDisplayValue: EnemyAmount/4
            );

            return;
        }
        
        pm.ApplyEnemySugarEffect(
            amountSigned: sugarAmount,
            durationGameMin: simpleDurationGameMin,
            floatingColor: simpleFloatingColor,
            floatingDisplayValue: sugarAmount/4,
            entryGameMin: 0f
        );
    }
}