using UnityEngine;

public class MenuBootstrap : MonoBehaviour
{
    [SerializeField] private bool resetPlayerPrefs = false;

    private void Awake()
    {
        PersistentRoot.DestroyAll();

        if (resetPlayerPrefs)
            PlayerPrefs.DeleteAll();
    }
}