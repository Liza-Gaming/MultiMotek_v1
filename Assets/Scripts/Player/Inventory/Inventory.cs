using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Inventory
{
    public event EventHandler OnItemListChanged;

    private List<Item> itemList;

    public Inventory()
    {
        itemList = new List<Item> ();
        Debug.Log(itemList.Count);
        //AddItem(new Item { itemType = Item.ItemType.Insulin, amount = 1 });
        //AddItem(new Item { itemType = Item.ItemType.Insulin, amount = 1 });
        //AddItem(new Item { itemType = Item.ItemType.Insulin, amount = 1 });
    }

    public void AddItem(Item item)
    {
        itemList.Add (item);
        OnItemListChanged?.Invoke (this, EventArgs.Empty);
    }

    public List<Item> GetItemList()
    {
        return itemList;
    }
}
