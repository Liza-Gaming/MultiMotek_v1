using UnityEngine;
using UnityEngine.UI;

public class ItemEntry : MonoBehaviour
{
    [SerializeField] private Image icon;

    public void Setup(Sprite sprite)
    {
        if (icon) icon.sprite = sprite;
    }
}