using UnityEngine;

public class BonusLevelSetup : MonoBehaviour
{
    void Start()
    {

        GameObject persistentCanvas = GameObject.Find("GlobalCanvas");
        if (persistentCanvas != null)
        {
            Destroy(persistentCanvas);
            Debug.Log("Global Canvas Destroyed for Bonus Level");
        }
        
        GameObject hudCanvas = GameObject.Find("MainHUD");
        if (hudCanvas != null)
        {
            Transform partToHide = hudCanvas.transform.Find("DestructibleOnBonus");
            if (partToHide != null)
            {
                partToHide.gameObject.SetActive(false);
            }
            
            Transform partToDestroy = hudCanvas.transform.Find("DestructibleOnBonus");
            if (partToDestroy != null)
            {
                Destroy(partToDestroy.gameObject);
            }
        }

    }
}