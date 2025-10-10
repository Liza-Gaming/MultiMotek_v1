using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class GrillMasterShooter : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Hit";
    [SerializeField] private float detectRadius = 8f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Auto Resolve")]
    [SerializeField] private bool autoFindPlayerOnEnable = true;
    [SerializeField] private float refetchInterval = 0.5f;
    [SerializeField] private int refetchTriesOnLoad = 20;

    [Header("Shoot")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private HamburgerProjectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float shootInterval = 2.0f;
    [SerializeField] private Vector2 shootIntervalJitter = new Vector2(0.0f, 0.4f);

    [Header("Facing")]
    [SerializeField] private bool flipByScaleX = true;
    [SerializeField] private float faceDeadZone = 0.05f;

    [Header("Misc")]
    [SerializeField] private bool requireLineOfSight = false;

    private float _cooldown;
    private Coroutine _refetchLoop;

    private void Awake()
    {
        if (!firePoint) firePoint = transform; // fallback
        ResetCooldown();
        
        if (!player) TryResolvePlayerOnce();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (autoFindPlayerOnEnable && !player)
            _refetchLoop = StartCoroutine(RefetchLoop());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_refetchLoop != null) StopCoroutine(_refetchLoop);
        _refetchLoop = null;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (_refetchLoop != null) StopCoroutine(_refetchLoop);
        _refetchLoop = StartCoroutine(RefetchBurst(refetchTriesOnLoad, refetchInterval));
    }

    private IEnumerator RefetchLoop()
    {
        while (!player)
        {
            TryResolvePlayerOnce();
            if (player) yield break;
            yield return new WaitForSeconds(refetchInterval);
        }
    }

    private IEnumerator RefetchBurst(int tries, float dt)
    {
        for (int i = 0; i < tries && !player; i++)
        {
            TryResolvePlayerOnce();
            if (player) yield break;
            yield return new WaitForSeconds(dt);
        }
        
        if (!player)
        {
            if (_refetchLoop != null) StopCoroutine(_refetchLoop);
            _refetchLoop = StartCoroutine(RefetchLoop());
        }
    }

    private void TryResolvePlayerOnce()
    {
        
        if (!player)
        {
            var tagged = GameObject.FindGameObjectWithTag(playerTag);
            if (tagged) player = tagged.transform;
        }
        if (!player)
        {
            var pm = FindObjectOfType<PlayerMover>();
            if (pm) player = pm.transform;
        }
    }
    
    public void SetPlayer(Transform t)
    {
        player = t;
    }

    private void Update()
    {
        
        if (player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag(playerTag);
            if (tagged != null)
                player = tagged.transform;
            else
                return; 
        }
        
        Vector2 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist > detectRadius) return;
        if (requireLineOfSight && !HasLineOfSight(toPlayer.normalized, dist)) return;

        FaceTowards(toPlayer.x);

        _cooldown -= Time.deltaTime;
        if (_cooldown <= 0f)
        {
            Shoot(toPlayer.normalized);
            ResetCooldown();
        }
    }


    private void FaceTowards(float dx)
    {
        if (!flipByScaleX) return;
        if (Mathf.Abs(dx) < faceDeadZone) return;

        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
        transform.localScale = s;
    }

    private bool HasLineOfSight(Vector2 dir, float dist)
    {
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir, dist, lineOfSightMask);
        if (!hit.collider) return true;
        return hit.collider.transform == player;
    }

    private void Shoot(Vector2 dirNorm)
    {
        if (!projectilePrefab) return;

        var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.Launch(dirNorm, projectileSpeed, this);
    }

    private void ResetCooldown()
    {
        float jitter = Random.Range(shootIntervalJitter.x, shootIntervalJitter.y);
        _cooldown = shootInterval + jitter;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        if (firePoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.07f);
        }
    }
}
