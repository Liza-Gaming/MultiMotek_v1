using UnityEngine;

public class EnemyAddSugar : MonoBehaviour, IEnemyEffect
{
    public float sugarAmount = 10f;

    public void ApplyEffect(GameObject playerObj)
    {
        if (SugarMeter.Instance != null)
        {
            SugarMeter.Instance.AddSugar(sugarAmount);
        }
    }

}
