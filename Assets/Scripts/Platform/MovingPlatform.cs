using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform posA, posB;
    public float speed = 2f;

    public Vector2 Velocity { get; private set; }

    private Rigidbody2D rb;
    private Vector2 targetPos;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void Start() { targetPos = posB.position; }

    void FixedUpdate()
    {
        Vector2 current = rb.position;
        Vector2 next = Vector2.MoveTowards(current, targetPos, speed * Time.fixedDeltaTime);
        Velocity = (next - current) / Time.fixedDeltaTime;
        rb.MovePosition(next);

        if (Vector2.Distance(next, (Vector2)posA.position) < 0.05f) targetPos = posB.position;
        else if (Vector2.Distance(next, (Vector2)posB.position) < 0.05f) targetPos = posA.position;
    }
}