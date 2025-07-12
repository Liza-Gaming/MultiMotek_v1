using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public Transform posA, posB;
    public float speed;
    Vector2 targetPos;

    void Start()
    {
        targetPos = posB.position;
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, posA.position) < 0.1f)
            targetPos = posB.position;

        if (Vector2.Distance(transform.position, posB.position) < 0.1f)
            targetPos = posA.position;

        transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
    }
}
