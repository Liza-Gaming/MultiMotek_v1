using UnityEngine;

public class LinkManager : MonoBehaviour
{

    public void OpenLink(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        Application.OpenURL(url);
    }
    
    public void OpenEmail(string emailAddress)
    {
        string mailto = "mailto:" + emailAddress;
        Application.OpenURL(mailto);
    }
}