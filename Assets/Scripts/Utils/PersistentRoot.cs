using System.Collections.Generic;
using UnityEngine;

public class PersistentRoot : MonoBehaviour
{
    private static readonly HashSet<PersistentRoot> registry = new();

    protected virtual void Awake()
    {
        // דואג שאובייקט זה יעבור בין סצנות
        if (transform.parent != null) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        registry.Add(this);
    }

    protected virtual void OnDestroy()
    {
        registry.Remove(this);
    }

    public static void DestroyAll()
    {
        // מעתיקים לרשימה כדי לא להרוס תוך כדי איטרציה
        var arr = new List<PersistentRoot>(registry);
        registry.Clear();
        foreach (var r in arr)
        {
            if (r) Destroy(r.gameObject);
        }
    }
}