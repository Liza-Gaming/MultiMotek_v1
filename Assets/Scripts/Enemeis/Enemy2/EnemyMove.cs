using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public Transform posA, posB, posC, posD;
    public float speed = 2f;

    private Transform[] points;
    private int currentIndex = 0;
    private int direction = 1;

    [SerializeField]
    private float size = 1;

    void Start()
    {
        points = new Transform[] { posA, posB, posC, posD };
        transform.position = points[0].position;
    }

    void Update()
    {
        Transform targetPoint = points[currentIndex];

        // Flip sprite if needed
        Vector3 moveDirection = targetPoint.position - transform.position;
        if (moveDirection.x > 0.01f)
            transform.localScale = new Vector3(size, size, size);
        else if (moveDirection.x < -0.01f)
            transform.localScale = new Vector3(-size, size, size);

        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.05f)
        {
            currentIndex += direction;

            if (currentIndex >= points.Length)
            {
                currentIndex = points.Length - 2;
                direction = -1;
            }
            else if (currentIndex < 0)
            {
                currentIndex = 1;
                direction = 1;
            }
        }
    }
}
