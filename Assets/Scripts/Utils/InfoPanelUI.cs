using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanelUI : MonoBehaviour
{
    [Header("Open/Close")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Scroll & Content")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject itemEntryPrefab;

    // שומרים אילו סוגי פריטים כבר נחשפו
    private readonly HashSet<Item.ItemType> discoveredTypes = new HashSet<Item.ItemType>();

    public static InfoPanelUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (panelRoot) panelRoot.SetActive(false);
        if (openButton)  openButton.onClick.AddListener(TogglePanel);
        if (closeButton) closeButton.onClick.AddListener(ClosePanel);
    }

    public void TogglePanel()
    {
        if (!panelRoot) return;
        panelRoot.SetActive(!panelRoot.activeSelf);
        if (panelRoot.activeSelf) ScrollToTop();
    }

    public void ClosePanel()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    /// קראי לזה כשנאסף פריט חדש בפעם הראשונה
    public void RegisterDiscovery(Item item)
    {
        if (item == null) return;
        if (!discoveredTypes.Add(item.itemType)) return; // כבר קיים

        var go = Instantiate(itemEntryPrefab, contentRoot);
        var entry = go.GetComponent<ItemEntry>();
        if (entry) entry.Setup(item.GetSprite());

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
        Canvas.ForceUpdateCanvases();
    }

    private void ScrollToTop()
    {
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ScrollToBottom()
    {
        if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;
    }
}