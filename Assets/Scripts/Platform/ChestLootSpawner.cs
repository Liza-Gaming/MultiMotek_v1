using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChestLootSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private Transform spawnPoint;

    [Header("Drops")]
    [SerializeField] private Item[] fixedDrops;
    [SerializeField] private Item[] randomDropPool;
    [SerializeField] private int randomCountMin = 2;
    [SerializeField] private int randomCountMax = 4;

    [Header("Throw Mode")]
    [SerializeField] private float spawnJitterRadius = 0.1f;

    [Header("Arc Throw (no physics)")]
    [SerializeField] private float arcDistanceMin = 2.0f;
    [SerializeField] private float arcDistanceMax = 3.5f;
    [SerializeField] private float arcHeight = 1.0f;
    [SerializeField] private float arcDuration = 0.45f;
    [SerializeField] private AnimationCurve arcEase = null;
    [SerializeField] private Vector2 arcAngleRangeDeg = new Vector2(0f, 360f);
    
    [Header("Final Position")]
    [SerializeField] private float groundHoverHeight = 0.3f;
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private float groundCheckDistance = 5f;
    
    [Header("Physics Throw (gravity off)")]
    [SerializeField] private float impulseMin = 3.0f;
    [SerializeField] private float impulseMax = 5.0f;
    [SerializeField] private Vector2 impulseAngleRangeDeg = new Vector2(60f, 120f);
    [SerializeField] private float settleSeconds = 0.5f;
    [SerializeField] private float physicsDrag = 4f;
    [SerializeField] private bool freezeRotation = true;
    [SerializeField] private float gravityScale = 0f;

    [Header("Behaviour")]
    [SerializeField] private bool openOnce = true;
    [SerializeField] private bool closeOnExit = true;
    [SerializeField] private bool triggerByAnimEvent = false;

    private bool hasDropped = false;
    
    
    [Header("Question Mark (session-only)")]
    [SerializeField] private GameObject questionMarkPrefab;
    [SerializeField] private GameObject questionMarkInstance;
    [SerializeField] private Vector3 qmarkLocalOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float qmarkBobAmplitude = 0.08f;
    [SerializeField] private float qmarkBobSpeed = 2.0f;

    private bool _openedOnceThisSession = false;
    private Vector3 _qmarkBaseLocalPos;


    private void Awake()
    {
        if (!chestAnimator) chestAnimator = GetComponentInChildren<Animator>();
        if (!spawnPoint)    spawnPoint    = transform;

        var col = GetComponent<Collider2D>();
        if (col && !col.isTrigger) col.isTrigger = true;

        if (arcEase == null) arcEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        SetupQuestionMark();
        ApplyQuestionMarkVisibility(!_openedOnceThisSession);

    }
    
    private void Update()
    {
        if (questionMarkInstance && questionMarkInstance.activeSelf)
        {
            float y = Mathf.Sin(Time.time * qmarkBobSpeed) * qmarkBobAmplitude;
            questionMarkInstance.transform.localPosition = _qmarkBaseLocalPos + new Vector3(0f, y, 0f);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (chestAnimator) chestAnimator.SetBool("IsOpened", true);
        HideQuestionMarkOnce();

        if (!triggerByAnimEvent && (!openOnce || !hasDropped))
            StartCoroutine(DropNow());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (closeOnExit && chestAnimator) chestAnimator.SetBool("IsOpened", false);
    }
    
    public void OnOpenAnimationEvent()
    {
        if (!openOnce || !hasDropped)
            StartCoroutine(DropNow());
        HideQuestionMarkOnce();
    }

    private IEnumerator DropNow()
    {
        hasDropped = true;

        var itemsToSpawn = BuildDropList();

        foreach (var it in itemsToSpawn)
        {
            var iw = SpawnItemProper(it);
            StartCoroutine(ArcThrow(iw));
            yield return new WaitForSeconds(0.05f);
        }

        if (!openOnce)
        {
            yield return new WaitForSeconds(1f);
            hasDropped = false;
        }
    }

    private List<Item> BuildDropList()
    {
        var list = new List<Item>();
        if (fixedDrops != null && fixedDrops.Length > 0)
        {
            list.AddRange(fixedDrops);
        }
        else if (randomDropPool != null && randomDropPool.Length > 0)
        {
            int count = Random.Range(randomCountMin, randomCountMax + 1);
            for (int i = 0; i < count; i++)
                list.Add(randomDropPool[Random.Range(0, randomDropPool.Length)]);
        }
        return list;
    }
    
    private ItemWorld SpawnItemProper(Item item)
    {
        Vector3 basePos = spawnPoint ? spawnPoint.position : transform.position;
        Vector2 jitter = Random.insideUnitCircle * spawnJitterRadius;
        Vector3 spawnPos = basePos + new Vector3(jitter.x, jitter.y, 0f);

        ItemWorld iw = ItemWorld.SpawnItemWorld(spawnPos, item);
        
        var col = iw.GetComponent<Collider2D>();
        if (!col) col = iw.gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        

        return iw;
    }
    
    private IEnumerator ArcThrow(ItemWorld iw)
    {
        if (!iw) yield break;
        var tr = iw.transform;
        if (!tr) yield break;

        Vector3 start = tr.position;
        
        float dist   = Random.Range(arcDistanceMin, arcDistanceMax);
        float angRad = Random.Range(arcAngleRangeDeg.x, arcAngleRangeDeg.y) * Mathf.Deg2Rad;
        Vector2 dir  = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad)).normalized;
        Vector3 rawTarget = start + (Vector3)(dir * dist);
        
        Vector3 target = SnapAboveGround(rawTarget, groundHoverHeight, groundLayerMask, groundCheckDistance, groundCheckDistance);

        float t = 0f;
        while (t < arcDuration)
        {
            if (!tr) yield break;

            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / arcDuration);

            Vector3 pos = Vector3.Lerp(start, target, arcEase.Evaluate(u));
            pos.y += Mathf.Sin(u * Mathf.PI) * arcHeight;

            tr.position = pos;
            yield return null;
        }
        
        if (tr) tr.position = target;
    }
    
    private Vector3 SnapAboveGround(Vector3 pos, float hoverHeight, LayerMask mask, float upDistance, float downDistance)
    {
        Vector2 origin = (Vector2)pos + Vector2.up * upDistance;
        float   totalDistance = upDistance + downDistance;

        var hits = Physics2D.RaycastAll(origin, Vector2.down, totalDistance, mask);
        RaycastHit2D bestHit = default;
        float bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.collider) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.normal.y <= 0f) continue;
            
            float distance = Vector2.Distance(origin, hit.point);
            if (distance < bestDistance)
            {
                bestHit = hit;
                bestDistance = distance;
            }
        }

        if (bestHit.collider != null)
        {
            return new Vector3(pos.x, bestHit.point.y + hoverHeight, pos.z);
        }
        
        return new Vector3(pos.x, pos.y + hoverHeight, pos.z);
    }

    private IEnumerator StopRbSoon(Rigidbody2D rb, float afterSeconds)
    {
        yield return new WaitForSeconds(afterSeconds);
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    private void SetupQuestionMark()
    {
        if (!questionMarkInstance && questionMarkPrefab)
        {
            questionMarkInstance = Instantiate(questionMarkPrefab, transform);
            questionMarkInstance.transform.localPosition = qmarkLocalOffset;
        }
        else if (questionMarkInstance)
        {
            if (!questionMarkInstance.scene.IsValid())
            {
                var prefabAsset = questionMarkInstance;
                questionMarkInstance = Instantiate(prefabAsset, transform);
                questionMarkInstance.transform.localPosition = qmarkLocalOffset;
            }
            else
            {

                if (questionMarkInstance.transform.parent != transform)
                    questionMarkInstance.transform.SetParent(transform, worldPositionStays: false);


                questionMarkInstance.transform.localPosition = qmarkLocalOffset;
            }
        }

        if (questionMarkInstance)
            _qmarkBaseLocalPos = questionMarkInstance.transform.localPosition;
    }


    private void ApplyQuestionMarkVisibility(bool visible)
    {
        if (questionMarkInstance)
            questionMarkInstance.SetActive(visible);
    }

    private void HideQuestionMarkOnce()
    {
        if (_openedOnceThisSession) return;
        _openedOnceThisSession = true;
        ApplyQuestionMarkVisibility(false);
    }

}