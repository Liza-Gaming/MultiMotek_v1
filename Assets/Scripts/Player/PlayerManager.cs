using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{

    private Transform checkpoint;
    public GameObject explosionPrefab;
    public Animator animator;
    private Inventory inventory;

    public float enemyHitCooldown = 1.0f;
    private float lastEnemyHitTime = -999f;

    [SerializeField] private UI_inventory uiInventory;

    private void Awake()
    {
        inventory = new Inventory();
        uiInventory.SetInventory(inventory);

        //ItemWorld.SpawnItemWorld(new Vector3(5f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(4f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(3f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(2f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(1f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(0f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });

    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        checkpoint = newCheckpoint;
    }

    public void Respawn()
    {
        if (checkpoint != null)
        {
            transform.position = checkpoint.position;
            Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
            animator.SetTrigger("Respawn");
            GetComponent<SpriteRenderer>().enabled = true;
            if (rb2d != null)
                rb2d.linearVelocity = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("No checkpoint set!");
        }
    }

    public void ExplodeAndRespawn()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        StartCoroutine(ExplodeAndRespawnCoroutine());
    }

    private IEnumerator ExplodeAndRespawnCoroutine()
    {
        GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(0.4f);
        Respawn();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag.Equals("NextLevel"))
        {
            SceneManager.LoadScene("Level2");
        }
        
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();
        if (itemWorld != null)
        {
            inventory.AddItem(itemWorld.GetItem());
            itemWorld.DestroySelf();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastEnemyHitTime >= enemyHitCooldown)
        {
            var enemyEffect = collision.gameObject.GetComponent<IEnemyEffect>();
            if (enemyEffect != null)
            {
                enemyEffect.ApplyEffect(this.gameObject);
                lastEnemyHitTime = Time.time;
            }
        }
    }
    







}
