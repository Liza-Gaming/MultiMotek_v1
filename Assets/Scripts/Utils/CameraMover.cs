using UnityEditor;
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
        ApplyBoundsForScene(SceneManager.GetActiveScene());
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m) {
        TryResolvePlayer();
        ApplyBoundsForScene(s);
    }

    private void TryResolvePlayer() {
        if (player != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        player = go ? go.transform : null;
    }
    
    private void ApplyBoundsForScene(Scene s)
    {
        // אופציה א: אם יש קומפוננטה בסצנה שמחזיקה גבולות – נעדיף אותה (נחמד למעצבים)
        var marker = FindFirstObjectByType<LevelCameraBounds>(FindObjectsInactive.Include);
        if (marker != null) {
            minX = marker.minX;
            maxX = marker.maxX;
            return;
        }

        // אופציה ב: לפי שם/אינדקס סצנה (מה שבא לך)
        switch (s.name)
        {
            case "Level 1":
                minX = 0f;
                maxX = 120f;
                break;
            case "Level 2":
                minX = 0f;
                maxX = 200f;
                break;
            case "Level 3":
                minX = 0f;
                maxX = 200f;
                break;
            case "Level 4":
                minX = 0f;
                maxX = 190f;
                break;
            default:
                minX = -999f;
                maxX =  999f;
                break;
        }
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

public class LevelCameraBounds : MonoBehaviour
{
    public float minX = -999f;
    public float maxX =  999f;
}