using UnityEngine;


public class KeepObjects : MonoBehaviour {
    public static KeepObjects Instance;
    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}