using UnityEngine;
public class SceneItemTarget : MonoBehaviour
{
    public Transform targetItem;
    
    public bool autoFindByTag = false;
    public string itemTag = "RequiredItem";

    private void Awake()
    {
        if (targetItem == null && autoFindByTag)
        {
            GameObject found = GameObject.FindGameObjectWithTag(itemTag);
            if (found != null)
            {
                targetItem = found.transform;
            }
        }
    }

    private void Start()
    {
        // מצא את השחקן והחץ שלו
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            ItemPointerArrow arrow = player.GetComponentInChildren<ItemPointerArrow>(true);
            
            if (arrow != null && targetItem != null)
            {
                arrow.SetTarget(targetItem);
                Debug.Log($"[SceneItemTarget] הוגדרה מטרה חדשה לחץ: {targetItem.name}");
            }
            else if (arrow == null)
            {
                Debug.LogWarning("[SceneItemTarget] לא נמצא ItemPointerArrow על השחקן!");
            }
        }
        else
        {
            Debug.LogWarning("[SceneItemTarget] לא נמצא אובייקט עם Tag 'Player'!");
        }
    }
    
}