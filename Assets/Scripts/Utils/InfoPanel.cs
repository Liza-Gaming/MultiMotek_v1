using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;

    void Awake()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }
    
    public void OnClick()
    {
        if (!popupPanel) return;
        popupPanel.SetActive(!popupPanel.activeSelf);
    }
    
    public void Close()
    {
        if (!popupPanel) return;
        popupPanel.SetActive(false);
    }
}