using UnityEngine;

public class ItemWorld : MonoBehaviour
{
    private Item item;
    private SpriteRenderer spriteRenderer;

    [Header("Hover (local)")]
    [SerializeField] private Transform visual;
    [SerializeField] private float hoverAmplitude = 0.15f;
    [SerializeField] private float hoverSpeed = 1.8f;
    [SerializeField] private bool randomizePhase = true;

    private Vector3 baseLocalPos;
    private Vector3 baseLocalScale;
    private float phase;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    public static ItemWorld SpawnItemWorld(Vector3 position, Item item)
    {
        Transform transform = Instantiate(ItemAssets.Instance.pfItemWorld, position, Quaternion.identity);
        ItemWorld itemWorld = transform.GetComponent<ItemWorld>();
        itemWorld.SetItem(item);
        return itemWorld;
    }
    
    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (visual == null)
            visual = spriteRenderer != null ? spriteRenderer.transform : transform;
    }

    private void OnEnable()
    {
        baseLocalPos = visual.localPosition;
        baseLocalScale = visual.localScale;
        phase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    public void SetItem(Item item)
    {
        this.item = item;
        if (spriteRenderer != null)
            spriteRenderer.sprite = item.GetSprite();
    }

    void Update()
    {
        float t = Time.time + phase;
        
        float y = baseLocalPos.y + Mathf.Sin(t * hoverSpeed) * hoverAmplitude;
        Vector3 lp = visual.localPosition;
        lp.y = y;
        visual.localPosition = lp;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (item == null) return;
        
        PlaySound2D(other.gameObject);
    
        var inventory = other.GetComponent<Inventory>();
        if (inventory != null) inventory.AddItem(item);
    
        DestroySelf();
    }


    private void PlaySound2D(GameObject player)
    {
        if (pickupSound == null) return;
        
        AudioSource playerSource = player.GetComponent<AudioSource>();
    
        if (playerSource != null)
        {
            playerSource.PlayOneShot(pickupSound, pickupVolume);
        }
    }

    public Item GetItem() => item;

    public void DestroySelf() => Destroy(gameObject);
}