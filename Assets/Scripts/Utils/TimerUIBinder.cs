using UnityEngine;
using UnityEngine.UI;

public class TimerUIBinder : MonoBehaviour
{
    [SerializeField] private Text clockText;
    [SerializeField] private Image background;

    void OnEnable()
    {
        if (Timer.Instance != null)
            Timer.Instance.BindUI(clockText, background);
    }
}