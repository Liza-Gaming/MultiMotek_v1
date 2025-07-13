using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Item
{
    public enum ItemType
    {
        Insulin,
        SugarBag,
        Apple,
        Bread,
    }

    public ItemType itemType;
    public int amount;

    public Sprite GetSprite()
    {
        switch (itemType)
        {
            default:
            case ItemType.Insulin:  return ItemAssets.Instance.Insulin;
            case ItemType.SugarBag: return ItemAssets.Instance.SugarBag;
            case ItemType.Apple:    return ItemAssets.Instance.Apple;
            case ItemType.Bread: return ItemAssets.Instance.Bread;
        }
    }
}
