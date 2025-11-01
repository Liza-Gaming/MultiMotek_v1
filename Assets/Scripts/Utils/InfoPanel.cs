using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    private Pause pauseManager;

    void Awake()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        
        pauseManager = FindObjectOfType<Pause>();
        
    }
    
    public void OnClick()
    {
        if (!popupPanel || !pauseManager) return; 
        
        bool willBeActive = !popupPanel.activeSelf;
        
        popupPanel.SetActive(willBeActive);

        if (willBeActive)
        {
            pauseManager.PausGeame(); 
        }
        else
        {
            pauseManager.Resume();
        }
    }
    
    public void Close()
    {
        if (!popupPanel || !pauseManager) return;
        
        if (popupPanel.activeSelf) 
        {
            popupPanel.SetActive(false);
            pauseManager.Resume();
        }
    }
}