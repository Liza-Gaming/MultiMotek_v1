using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float aheadDistance = 2f;
    [SerializeField] private float cameraSpeed = 5f;
    private float lookAhead;

    [Header("Camera Bounds")]
    [SerializeField] private float minX = -999f;
    [SerializeField] private float maxX =  999f;

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryResolvePlayer();
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m) {
        TryResolvePlayer();
    }

    private void TryResolvePlayer() {
        if (player != null) return;
        var go = GameObject.FindGameObjectWithTag("Player"); // וודאי שלשחקן יש Tag=Player
        player = go ? go.transform : null;
    }

    private void Update()
    {
        if (!player) {            // ב-Unity Destroyed אכן מתנהג כמו null
            TryResolvePlayer();   // נסי להתחבר שוב
            return;               // אל תתזוז כשאין שחקן
        }

        float targetX = player.position.x + lookAhead;
        targetX = Mathf.Clamp(targetX, minX, maxX);

        transform.position = new Vector3(targetX, player.position.y + 1f, transform.position.z);
        lookAhead = Mathf.Lerp(lookAhead, aheadDistance * player.localScale.x, Time.deltaTime * cameraSpeed);
    }
}