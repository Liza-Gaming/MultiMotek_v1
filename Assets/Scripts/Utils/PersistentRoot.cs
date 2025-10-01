using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PersistentRoot : MonoBehaviour
{
    private static readonly HashSet<GameObject> _roots = new HashSet<GameObject>();

    void Awake()
    {
        _roots.Add(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        _roots.Remove(gameObject);
    }

    public static void DestroyAll()
    {

        foreach (var go in _roots.ToList())
        {
            if (go) Destroy(go);
        }
        _roots.Clear();
    }
}