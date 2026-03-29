using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimerUIBinder : MonoBehaviour
{
    [SerializeField] private Text clockText;
    [SerializeField] private Image background;

    private IEnumerator Start()
    {
        yield return null;

        if (Timer.Instance != null)
        {
            Timer.Instance.BindUI(clockText, background);
            Debug.Log("[TimerUIBinder] Successfully bound UI in the new scene.");
        }
        else
        {
            Debug.LogWarning("[TimerUIBinder] Timer.Instance is null! Is the Timer persistent?");
        }
    }
}