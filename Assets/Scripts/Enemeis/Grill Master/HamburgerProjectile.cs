using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HamburgerProjectile : MonoBehaviour
{
    [SerializeField] private float lifeSeconds = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool flipSpriteByVelocityX = true;

    private Rigidbody2D _rb;
    private Collider2D _col;
    private float _t;
    private Transform _owner;
    
    [SerializeField] private float EnemyAmount = 16f;
    [SerializeField] private float EnemyDurationGameMin = 180f;
    [SerializeField] private float EnemyDelayGameMin    = 15f;

    private PlayerManager manager;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _col.isTrigger = false;
    }

    public void Launch(Vector2 dirNormalized, float speed, MonoBehaviour owner)
    {
        _owner = owner ? owner.transform : null;
        
        if (_owner)
        {
            foreach (var c in _owner.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(_col, c, true);
        }

        _rb.linearVelocity = dirNormalized * speed;

        if (flipSpriteByVelocityX)
        {
            var s = transform.localScale;
            if (Mathf.Abs(_rb.linearVelocity.x) > 0.001f)
                s.x = Mathf.Abs(s.x) * (_rb.linearVelocity.x >= 0f ? 1f : -1f);
            transform.localScale = s;
        }
    }

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifeSeconds)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            ApplyEffect(collision.collider.gameObject);
        }
        
        if (((1 << collision.collider.gameObject.layer) & hitMask) != 0)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplyEffect(GameObject playerObj)
    {

            float totalGameMin = EnemyDelayGameMin + EnemyDurationGameMin;
            float totalRealSec = GameTime.GameMinutesToRealSeconds(totalGameMin);

            var pm = playerObj.GetComponent<PlayerManager>();
            pm?.SuppressSugarArrowRealSeconds(2f);
           // manager.ShowFloatingSugarText(EnemyAmount/4, Color.yellow);
          
            SugarMeter.Instance?.ScheduleEffectGame(
                EnemyAmount,
                durationGameMin: EnemyDurationGameMin,
                entryGameMin: EnemyDelayGameMin
            );
    }
}
