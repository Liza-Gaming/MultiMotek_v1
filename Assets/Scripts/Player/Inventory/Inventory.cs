using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Inventory:MonoBehaviour
{
    public event EventHandler OnItemListChanged;

    private List<Item> itemList;
    private Action<Item> useItemAction;

    public Inventory(Action<Item> useItemAction)
    {
        this.useItemAction = useItemAction;
        itemList = new List<Item> ();
        Debug.Log(itemList.Count);
    }

    public void AddItem(Item item)
    {
        bool itemInInventory = false;
        foreach (Item inventoryItem in itemList)
        {
            if(inventoryItem.itemType == item.itemType)
            {
                inventoryItem.amount += item.amount;
                itemInInventory = true;
            }
        }
        if (!itemInInventory)
        {
            itemList.Add(item);
        }
        OnItemListChanged?.Invoke (this, EventArgs.Empty);
    }

    public List<Item> GetItemList()
    {
        return itemList;
    }

    public void UseItem(Item item)
    {
        useItemAction(item);
    }

    public void RemoveItem(Item item)
    {
        Item itemInInventory = null;
        foreach (Item inventoryItem in itemList)
        {
            if (inventoryItem.itemType == item.itemType)
            {
                inventoryItem.amount -= item.amount;
                itemInInventory = inventoryItem;
            }
        }

        if (itemInInventory != null && itemInInventory.amount <= 0)
        {
            itemList.Remove(itemInInventory);
        }
        OnItemListChanged?.Invoke (this, EventArgs.Empty);
    }
    
    public void ClearAll()
    {
        if (itemList == null) itemList = new List<Item>();
        else itemList.Clear();

        OnItemListChanged?.Invoke(this, EventArgs.Empty);
    }

}
