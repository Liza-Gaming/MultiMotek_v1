using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform posA, posB;
    
    public float tripTime = 2f; 

    public Vector2 Velocity { get; private set; }

    private Rigidbody2D rb;
    private float progress = 0f;
    private int direction = 1;

    void Awake() 
    { 
        rb = GetComponent<Rigidbody2D>(); 
    }

    void FixedUpdate()
    {
        Vector2 current = rb.position;
        
        progress += direction * (Time.fixedDeltaTime / tripTime);
        
        if (progress >= 1f)
        {
            progress = 1f;
            direction = -1;
        }
        else if (progress <= 0f)
        {
            progress = 0f;
            direction = 1;
        }

        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        
        Vector2 next = Vector2.Lerp(posA.position, posB.position, smoothProgress);

        Velocity = (next - current) / Time.fixedDeltaTime;
        rb.MovePosition(next);
    }
}