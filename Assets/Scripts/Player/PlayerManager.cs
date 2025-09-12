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
    
    [SerializeField] private PlayerFeedback playerFeedback;
    
    [SerializeField] private SugarSummaryUI summaryUI;

    [SerializeField] private UI_inventory uiInventory;

    [SerializeField] private SugarMeter sugarMeter;
    
    [SerializeField] private Color insulinFlashColor = new Color(0.35f, 0.7f, 1f);
    [SerializeField] private Color sugarFlashColor   = new Color(1f, 0.85f, 0.3f); 
    [SerializeField] private Color EnemyFlashColor   = new Color(1f, 0.85f, 0.3f);
    
    [SerializeField] private LayerMask enemyLayers;
    
    [SerializeField] private SugarBlinkers sugarArrow;


    private void Awake()
    {
        if (sugarMeter == null) sugarMeter = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        inventory = new Inventory(UseItemAction);
        uiInventory.SetInventory(inventory);

        //ItemWorld.SpawnItemWorld(new Vector3(5f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(4f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(3f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(2f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(1f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
        //ItemWorld.SpawnItemWorld(new Vector3(0f, 0.5f), new Item { itemType = Item.ItemType.SugarBag, amount = 1 });

    }
    
    
    private void OnEnable()
    {
        var sm = SugarMeter.Instance ?? sugarMeter;
        if (sm != null) sm.TimedChangeStarted += OnTimedChangeStarted;
    }

    private void OnDisable()
    {
        var sm = SugarMeter.Instance ?? sugarMeter;
        if (sm != null) sm.TimedChangeStarted -= OnTimedChangeStarted;
    }

    private void OnTimedChangeStarted(bool isIncrease, float durationSec)
    {
        if (isIncrease) sugarArrow?.ShowUp(durationSec);
        else            sugarArrow?.ShowDown(durationSec);
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
            playerFeedback?.ForceEyesOpen();
            var mover = GetComponent<PlayerMover>();
            if (mover != null) mover.SetInputLocked(false);
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
        float savedSugar = sugarMeter ? sugarMeter.GetSugarLevel() : 0f;
        
        //sugarArrow?.SuppressForSeconds(1.0f);

        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        StartCoroutine(ExplodeAndRespawnCoroutine(savedSugar));
    }

    private IEnumerator ExplodeAndRespawnCoroutine(float savedSugar)
    {
        GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(0.4f);

        Respawn();
        
        if (sugarMeter) sugarMeter.SetSugarInstant(savedSugar);
    }
    
    private static bool InMask(GameObject go, LayerMask mask) => (mask.value & (1 << go.layer)) != 0;

    private void TryEnemyHit(Component hitSource)
    {
        bool looksLikeEnemy = InMask(hitSource.gameObject, enemyLayers);
        var effect = hitSource.GetComponent<IEnemyEffect>()
                     ?? hitSource.GetComponentInParent<IEnemyEffect>()
                     ?? hitSource.GetComponentInChildren<IEnemyEffect>();

        if (!looksLikeEnemy && effect == null) return;

        if (Time.time - lastEnemyHitTime < enemyHitCooldown) return;
        
        playerFeedback?.PlayUseItemFX(EnemyFlashColor, withEyesClosed: true);
        
        effect?.ApplyEffect(this.gameObject);
        
        sugarArrow?.ShowUp(3f);

        lastEnemyHitTime = Time.time;
    }


    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryEnemyHit(collider);
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();
        if (itemWorld != null)
        {
            inventory.AddItem(itemWorld.GetItem());
            itemWorld.DestroySelf();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryEnemyHit(collision.collider);
    }
    
    public void SuppressSugarArrowRealSeconds(float realSeconds)
    {
        sugarArrow?.SuppressForSeconds(realSeconds);
    }


    private void UseItemAction(Item item)
    {
        switch (item.itemType)
        {
            case Item.ItemType.Insulin:
                sugarMeter.DecreaseSugarGame(30f, durationGameMin: 180f, delayGameMin: 0f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Insulin, amount = 1 });
                playerFeedback?.PlayUseItemFX(insulinFlashColor);
                break;

            case Item.ItemType.SugarBag:
                sugarMeter.AddSugarGame(4f, durationGameMin: 60f, delayGameMin: 15f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;

            case Item.ItemType.Banana:
                sugarMeter.AddSugarGame(25f, durationGameMin: 120f, delayGameMin: 15f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Banana, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;

            case Item.ItemType.WaterMelon:
                sugarMeter.AddSugarGame(11f, durationGameMin: 120f, delayGameMin:15f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.WaterMelon, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.ChickenLeg:
                sugarMeter.AddSugarGame(0f, durationGameMin: 0f, delayGameMin:0f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.ChickenLeg, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.Bamba:
                sugarMeter.AddSugarGame(12f, durationGameMin:120f, delayGameMin:20f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Bamba, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.Apple:
                sugarMeter.AddSugarGame(15f, durationGameMin:120f, delayGameMin:15f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Apple, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.Bread:
                sugarMeter.AddSugarGame(15f, durationGameMin:120f, delayGameMin:30f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Bread, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.Fish:
                sugarMeter.AddSugarGame(0f, durationGameMin:0f, delayGameMin:0f, suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Fish, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.Sausages:
                sugarMeter.AddSugarGame(0f, durationGameMin:0f, delayGameMin:0f,suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Sausages, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.EnergyDrink:
                sugarMeter.AddSugarGame(28f, durationGameMin:60f, delayGameMin:15f,suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.EnergyDrink, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.BeanBag:
                sugarMeter.AddSugarGame(14f, durationGameMin:120f, delayGameMin:15f,suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.BeanBag, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.ChocolateCup:
                sugarMeter.AddSugarGame(17f, durationGameMin:60f, delayGameMin:15f,suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.ChocolateCup, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
            
            case Item.ItemType.Icecream:
                sugarMeter.AddSugarGame(12f, durationGameMin:60f, delayGameMin:15f,suppressBaselineDuring: true);
                inventory.RemoveItem(new Item { itemType = Item.ItemType.Icecream, amount = 1 });
                playerFeedback?.PlayUseItemFX(sugarFlashColor);
                break;
        }
    }

    }

