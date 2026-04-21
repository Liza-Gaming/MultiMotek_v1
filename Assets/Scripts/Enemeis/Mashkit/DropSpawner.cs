using UnityEngine;

public class DropSpawner : MonoBehaviour
{
    [SerializeField]
    public GameObject dropPrefab;

    [SerializeField]
    public Transform spawnPoint;

    [SerializeField]
    [Tooltip("Time between each spawn in seconds")]
    public float spawnInterval = 5f;

    private float timer; // To countdown
    [SerializeField] private bool isSpawnning;

    void Start()
    {
        // Initialize the timer
        timer = spawnInterval;
    }

    void Update()
    {
        // Countdown the timer
        timer -= Time.deltaTime;

        // Check if it's time to spawn
        if (timer <= 0f && isSpawnning)
        {
            SpawnDrop();
            timer = spawnInterval; // Reset the timer
        }
    }

    void SpawnDrop()
    {
        // Instantiates a new customer at the spawn point
        Instantiate(dropPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}