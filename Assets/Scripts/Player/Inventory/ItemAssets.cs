using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemAssets : MonoBehaviour
{
    public static ItemAssets Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Transform pfItemWorld;
    public Sprite Insulin;
    public Sprite SugarBag;
}
