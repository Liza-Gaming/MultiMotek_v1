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
        EnergyDrink,
        ChocolateCup,
        CokeCup,
        Candy,
        LineChocolate,
        ChickenLeg,
        Fish,
        Sausages,
        DietYogurt,
        Banana,
        WaterMelon,
        BeanBag,
        Bamba,
        Baigale,
        Icecream,
        Cucumber,
        Carrot,
        Tomato
        
    } // add vegetables, milk products, No impact drinks

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
            case ItemType.EnergyDrink: return ItemAssets.Instance.EnergyDrink;
            case ItemType.ChocolateCup: return ItemAssets.Instance.ChocolateCup;
            case ItemType.CokeCup: return ItemAssets.Instance.CokeCup;
            case ItemType.Candy: return ItemAssets.Instance.Candy;
            case ItemType.LineChocolate: return ItemAssets.Instance.LineChocolate;
            case ItemType.ChickenLeg: return ItemAssets.Instance.ChickenLeg;
            case ItemType.Fish: return ItemAssets.Instance.Fish;
            case ItemType.Sausages: return ItemAssets.Instance.Sausages;
            case ItemType.DietYogurt: return ItemAssets.Instance.DietYogurt;
            case ItemType.Banana: return ItemAssets.Instance.Banana;
            case ItemType.WaterMelon: return ItemAssets.Instance.WaterMelon;
            case ItemType.BeanBag: return ItemAssets.Instance.BeanBag;
            case ItemType.Bamba: return ItemAssets.Instance.Bamba;
            case ItemType.Baigale: return ItemAssets.Instance.Baigale;
            case ItemType.Icecream: return ItemAssets.Instance.Icecream;
            case ItemType.Cucumber: return ItemAssets.Instance.Cucamber;
            case ItemType.Carrot: return ItemAssets.Instance.Carrot;
            case ItemType.Tomato: return ItemAssets.Instance.Tomato;
            
        }
    }
}
