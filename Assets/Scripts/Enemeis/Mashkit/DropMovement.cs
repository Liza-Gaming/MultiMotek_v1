using UnityEngine;

public class DropMovement : MonoBehaviour
{
    public float speed = 3f;
    private Rigidbody2D rb;
    private Animator animator;
    private bool hasExploded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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
        if (!hasExploded && collision.gameObject.CompareTag("ground"))
        {
            hasExploded = true;
            animator.SetTrigger("Explode");
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
