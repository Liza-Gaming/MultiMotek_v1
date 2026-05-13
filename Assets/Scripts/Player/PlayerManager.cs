using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Player.Sugarcontrol.InsulinPump;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    private Transform checkpoint;
    public GameObject explosionPrefab;
    public Animator animator;
    private Inventory inventory;

    public float enemyHitCooldown = 1.0f;
    private float lastEnemyHitTime = -999f;
    
    [Header("Item Image")]
    [SerializeField] private Image itemIconImage;
    
    
    [SerializeField] private PlayerFeedback playerFeedback;
    
    [SerializeField] private SugarSummaryUI summaryUI;

    [SerializeField] private UI_inventory uiInventory;

    [SerializeField] private SugarMeter sugarMeter;
    
    [SerializeField] private Color insulinFlashColor = new Color(0.35f, 0.7f, 1f);
    [SerializeField] private Color sugarFlashColor   = new Color(1f, 0.85f, 0.3f); 
    [SerializeField] private Color EnemyFlashColor   = new Color(1f, 0.85f, 0.3f);
    
    [SerializeField] private LayerMask enemyLayers;
    
    [SerializeField] private SugarBlinkers sugarArrow;

    public bool hasRequiredItem = false;
    
    [SerializeField] private ItemPointerArrow itemPointer;
    
    [SerializeField] private Color waterFxColor = new Color(0.35f, 0.7f, 1f);

    private readonly Queue<float> _waterPressTimesReal = new Queue<float>();

    private const float WATER_WINDOW_GAME_HOURS = 1f;
    private const float WATER_EFFECT_GAME_MIN   = 90f;
    private const float WATER_SUGAR_DROP        = 2f;
    
    
    [Header("Audio")]
    [SerializeField] private AudioClip collisionAudio;
    private AudioSource audioSource;
    
    
    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Vector3 floatingTextOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private Canvas floatingTextCanvas;

    private void Awake()
    {
        if (sugarMeter == null) sugarMeter = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        inventory = new Inventory(UseItemAction);
        uiInventory.SetInventory(inventory);
        audioSource = GetComponent<AudioSource>();
    }
    
    private void OnEnable()
    {
        var sm = SugarMeter.Instance ?? sugarMeter;
        if (sm != null) sm.TimedChangeStarted += OnTimedChangeStarted;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        var sm = SugarMeter.Instance ?? sugarMeter;
        if (sm != null) sm.TimedChangeStarted -= OnTimedChangeStarted;
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hasRequiredItem = false;
    
        if (itemPointer != null)
        {
            GameObject requiredItem = GameObject.FindGameObjectWithTag("RequiredItem");
        
            if (requiredItem != null)
            {
                itemPointer.SetTarget(requiredItem.transform);
            }
            else
            {
                itemPointer.SetTarget(null);
            }
        }
    }

    public void ResetArrowForNewScene(Transform newTarget)
    {
        if (itemPointer != null)
        {
            itemPointer.SetTarget(newTarget);
        }
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
        
        audioSource.PlayOneShot(this.collisionAudio);
        
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
    
    public void ShowFloatingSugarText(float sugarValue, Color color)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("Floating Text Prefab is not assigned in PlayerManager.");
            return;
        }

        if (floatingTextCanvas == null)
        {
            Debug.LogWarning("Floating Text Canvas is not assigned in PlayerManager.");
            return;
        }
        
        Vector3 worldPos = transform.position + floatingTextOffset;
        
        GameObject textInstance = Instantiate(floatingTextPrefab, floatingTextCanvas.transform);
        
        textInstance.transform.position = worldPos;

        FloatingText ft = textInstance.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Initialize(sugarValue, worldPos, color);
        }
        else
        {
            Debug.LogWarning("FloatingText component not found on prefab instance.");
            Destroy(textInstance);
        }
    }
    
private void ApplyItemSugarEffect(
    float amountSigned,
    float durationGameMin,
    float entryGameMin,
    bool showFloatingText,
    float floatingDisplayValue,
    Color floatingColor,
    Color fxColor,
    Sprite itemSprite = null,
    bool canShowPump = true)
{
    bool isFoodRise = amountSigned > 0f;
    int expectedCarbsForQuiz = isFoodRise ? Mathf.RoundToInt(floatingDisplayValue) : 0;
    
    if (isFoodRise && CarbReportManager.Instance != null && entryGameMin > 0f && canShowPump)
    {
        CarbReportManager.Instance.RequestReport(
            expectedCarbsForQuiz,
            expectedFoodRiseMgdl: Mathf.Max(0f, amountSigned),
            foodDurationGameMin: durationGameMin,
            itemSprite: itemSprite,
            onCorrect: (reportedCarbs) =>
            {
                if (sugarMeter != null)
                    sugarMeter.ScheduleEffectGame(amountSigned, durationGameMin, entryGameMin);

                if (showFloatingText)
                    ShowFloatingSugarText(floatingDisplayValue, floatingColor);

                playerFeedback?.PlayUseItemFX(fxColor);
            }
        );

        return; 
    }

    // אם הגענו לכאן, זה או ירידת סוכר, או אויב (ששלח false), או פריט שלא דורש משאבה
    if (sugarMeter != null)
        sugarMeter.ScheduleEffectGame(amountSigned, durationGameMin, entryGameMin);

    if (showFloatingText)
        ShowFloatingSugarText(floatingDisplayValue, floatingColor);

    playerFeedback?.PlayUseItemFX(fxColor);
}

public void ApplyEnemySugarEffect(
    float amountSigned,
    float durationGameMin,
    Color floatingColor,
    float floatingDisplayValue,
    float entryGameMin)
{
    ApplyItemSugarEffect(
        amountSigned: amountSigned,
        durationGameMin: durationGameMin,
        entryGameMin: entryGameMin,
        showFloatingText: true,
        floatingDisplayValue: floatingDisplayValue,
        floatingColor: floatingColor,
        fxColor: EnemyFlashColor,
        itemSprite: null,
        canShowPump: false
    );
}

    public void UseItemAction(Item item)
    {
        switch (item.itemType)
        {
            case Item.ItemType.Insulin:
            {
                float amount = -30f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 180f,
                    entryGameMin: 0f,
                    showFloatingText: true,
                    floatingDisplayValue: -30f,
                    floatingColor: Color.red,
                    fxColor: insulinFlashColor,
                    itemSprite: item.GetSprite() // <--- העברת התמונה
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Insulin, amount = 1 });
                break;
            }

            case Item.ItemType.SugarBag:
            {
                float amount = +4f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 60f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 4f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.SugarBag, amount = 1 });
                break;
            }

            case Item.ItemType.Banana:
            {
                float amount = +25f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 20f,
                    showFloatingText: true,
                    floatingDisplayValue: 25f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Banana, amount = 1 });
                break;
            }

            case Item.ItemType.WaterMelon:
            {
                float amount = +11f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 11f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.WaterMelon, amount = 1 });
                break;
            }

            case Item.ItemType.ChickenLeg:
            {
                float amount = 0f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 0f,
                    entryGameMin: 0f,
                    showFloatingText: false,
                    floatingDisplayValue: 0f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.ChickenLeg, amount = 1 });
                break;
            }

            case Item.ItemType.Bamba:
            {
                float amount = +12f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 20f,
                    showFloatingText: true,
                    floatingDisplayValue: 12f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Bamba, amount = 1 });
                break;
            }

            case Item.ItemType.Apple:
            {
                float amount = +15f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 15f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Apple, amount = 1 });
                break;
            }

            case Item.ItemType.Bread:
            {
                float amount = +15f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 30f,
                    showFloatingText: true,
                    floatingDisplayValue: 15f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Bread, amount = 1 });
                break;
            }

            case Item.ItemType.Fish:
            {
                float amount = 0f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 0f,
                    entryGameMin: 0f,
                    showFloatingText: false,
                    floatingDisplayValue: 0f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Fish, amount = 1 });
                break;
            }

            case Item.ItemType.Sausages:
            {
                float amount = 0f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 0f,
                    entryGameMin: 0f,
                    showFloatingText: false,
                    floatingDisplayValue: 0f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Sausages, amount = 1 });
                break;
            }

            case Item.ItemType.EnergyDrink:
            {
                float amount = +28f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 60f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 28f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.EnergyDrink, amount = 1 });
                break;
            }

            case Item.ItemType.BeanBag:
            {
                float amount = +14f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 14f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.BeanBag, amount = 1 });
                break;
            }

            case Item.ItemType.ChocolateCup:
            {
                float amount = +17f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 60f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 17f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.ChocolateCup, amount = 1 });
                break;
            }

            case Item.ItemType.Icecream:
            {
                float amount = +12f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 60f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 12f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Icecream, amount = 1 });
                break;
            }

            case Item.ItemType.LineChocolate:
            {
                float amount = +17f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 20f,
                    showFloatingText: true,
                    floatingDisplayValue: 17f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.LineChocolate, amount = 1 });
                break;
            }

            case Item.ItemType.Candy:
            {
                float amount = +15f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 60f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 15f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Candy, amount = 1 });
                break;
            }

            case Item.ItemType.DietYogurt:
            {
                float amount = +8f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 8f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.DietYogurt, amount = 1 });
                break;
            }

            case Item.ItemType.Baigale:
            {
                float amount = +23f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 120f,
                    entryGameMin: 20f,
                    showFloatingText: true,
                    floatingDisplayValue: 23f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Baigale, amount = 1 });
                break;
            }

            case Item.ItemType.Cucumber:
            {
                float amount = 0f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 0f,
                    entryGameMin: 0f,
                    showFloatingText: false,
                    floatingDisplayValue: 0f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Cucumber, amount = 1 });
                break;
            }

            case Item.ItemType.Carrot:
            {
                float amount = 0f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 0f,
                    entryGameMin: 0f,
                    showFloatingText: false,
                    floatingDisplayValue: 0f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Carrot, amount = 1 });
                break;
            }

            case Item.ItemType.Tomato:
            {
                float amount = 0f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 0f,
                    entryGameMin: 0f,
                    showFloatingText: false,
                    floatingDisplayValue: 0f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.Tomato, amount = 1 });
                break;
            }

            case Item.ItemType.CokeCup:
            {
                float amount = +26f * 4f;
                ApplyItemSugarEffect(
                    amountSigned: amount,
                    durationGameMin: 60f,
                    entryGameMin: 15f,
                    showFloatingText: true,
                    floatingDisplayValue: 26f,
                    floatingColor: Color.yellow,
                    fxColor: sugarFlashColor,
                    itemSprite: item.GetSprite()
                );

                inventory.RemoveItem(new Item { itemType = Item.ItemType.CokeCup, amount = 1 });
                break;
            }
        }
    }
    
    public void HandleWaterBottlePress()
    {
        float windowRealSec = GameTime.GameHoursToRealSeconds(WATER_WINDOW_GAME_HOURS);
        float now = Time.time;
        
        while (_waterPressTimesReal.Count > 0 && (now - _waterPressTimesReal.Peek()) > windowRealSec)
            _waterPressTimesReal.Dequeue();
        
        _waterPressTimesReal.Enqueue(now);
        
        if (_waterPressTimesReal.Count < 3)
            return;
        
        _waterPressTimesReal.Clear();

        // קריאה בלי תמונה
        ApplyItemSugarEffect(
            amountSigned: -WATER_SUGAR_DROP,
            durationGameMin: WATER_EFFECT_GAME_MIN,
            entryGameMin: 15f,
            showFloatingText: true,
            floatingDisplayValue: -WATER_SUGAR_DROP,
            floatingColor: waterFxColor,
            fxColor: waterFxColor
        );
    }
}