using UnityEngine;
using UnityEngine.UI;

public class RequiredItem : MonoBehaviour
{

    [SerializeField] Image[] itemImage;
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
            PlayerManager collector = other.GetComponent<PlayerManager>();
            
            if (collector != null)
            {
                collector.hasRequiredItem = true;
                for (int i = 0; i < itemImage.Length; i++)
                {
                    itemImage[i].color = Color.white;
                }
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Player touched RequiredItem but has no PlayerItemCollector script!");
            }
        }
    }
}