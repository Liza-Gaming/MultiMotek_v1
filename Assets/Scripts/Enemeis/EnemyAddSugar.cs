using System;
using UnityEngine;

public class EnemyAddSugar : MonoBehaviour, IEnemyEffect
{
    public float sugarAmount = 10f;

    // ערכים בשביל הבופה (כמו שהגדרת)
    [SerializeField] private float EnemyAmount = 16f;
    [SerializeField] private float EnemyDurationGameMin = 180f; // שעתיים
    [SerializeField] private float EnemyDelayGameMin    = 15f;

    public void ApplyEffect(GameObject playerObj)
    {
        if (this.CompareTag("Buffe") || this.CompareTag("GrillMaster") )
        {
    
            float totalGameMin = EnemyDelayGameMin + EnemyDurationGameMin;
            float totalRealSec = GameTime.GameMinutesToRealSeconds(totalGameMin);

            var pm = playerObj.GetComponent<PlayerManager>();
            pm?.SuppressSugarArrowRealSeconds(2f); // בופר קטן

            // מפעילים את האפקט המדורג (ללא חץ)
            SugarMeter.Instance?.AddSugarGame(
                EnemyAmount,
                durationGameMin: EnemyDurationGameMin,
                delayGameMin: EnemyDelayGameMin,
                suppressBaselineDuring: true
            );
            return;
        }

        // מקרה רגיל (לא בופה) – נשאר כמו שהיה
        if (SugarMeter.Instance != null)
        {
            SugarMeter.Instance.AddSugarGame(sugarAmount);
            GetComponentInChildren<SugarBlinkers>()?.ShowUp(2f);
        }
    }
}