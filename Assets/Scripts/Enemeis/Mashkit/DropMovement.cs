using UnityEngine;

public class DropMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    private Rigidbody2D rb;
    private Animator animator;
    private bool hasExploded = false;

    [Header("Lifetime")]
    public float lifeTime = 5f; // כמה זמן עד שהטיפה נהרסת אוטומטית

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // השמדה אוטומטית אחרי זמן מוגדר
        Invoke(nameof(DestroySelf), lifeTime);
    }

    void Update()
    {
        if (!hasExploded)
        {
            rb.linearVelocity = new Vector2(-speed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.CompareTag("ground"))
        {
            Explode();
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            // אפשר להוסיף כאן קוד שיפגע בשחקן (נזק/סוכר/חיים)
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;
        animator.SetTrigger("Explode");
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}