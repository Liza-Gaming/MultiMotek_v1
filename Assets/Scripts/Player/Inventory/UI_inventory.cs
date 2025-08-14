using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using CodeMonkey.Utils;
using UnityEngine.UI;
using TMPro;

public class UI_inventory : MonoBehaviour
{
    private Inventory inventory;
    private Transform itemSlotContainer;
    private Transform itemSlotTemplate;

    private void Awake()
    {
        itemSlotContainer = transform.Find("itemSlotContainer");
        itemSlotTemplate = itemSlotContainer.Find("itemSlotTemplate");
    }

    public void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;

        inventory.OnItemListChanged += Inventory_OnItemChanged;

        RefreshInventoryItems();
    }


    private void Inventory_OnItemChanged(object sender, System.EventArgs e)
    {
        RefreshInventoryItems();
    }
    private void RefreshInventoryItems()
    {
        foreach (Transform child in itemSlotContainer)
        {
            if (child == itemSlotTemplate) continue;
            Destroy(child.gameObject);
        }
        float x = 0f;
        float y = 0f;
        float itemSlotCellSize = 120f;
        foreach (Item item in inventory.GetItemList())
        {
            RectTransform itemSlotRectTransform = Instantiate(itemSlotTemplate, itemSlotContainer).GetComponent<RectTransform>();
            itemSlotRectTransform.gameObject.SetActive(true);

            itemSlotRectTransform.GetComponent<Button_UI>().ClickFunc = () =>
            {
                inventory.UseItem(item);
            };
            itemSlotRectTransform.anchoredPosition = new Vector2(x* itemSlotCellSize, y * itemSlotCellSize);
            Image image = itemSlotRectTransform.Find("Image").GetComponent<Image>();
            image.sprite = item.GetSprite();
            Text uiText = itemSlotRectTransform.Find("amountText").GetComponent<Text>();
            if(item.amount > 1)
            {
                uiText.text = item.amount.ToString();
            }
            else
            {
                uiText.text = "";
            }

            x++;
            if(x > 2f)
            {
                x = 0;
                y-=1f;
            }
        }
    }
}
