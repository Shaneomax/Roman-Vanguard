using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;

    // ── Main UI ─────────────────────────────────────────────────────────────
    [Header("Main UI")]
    public GameObject rootPanel;
    public TextMeshProUGUI shopTitleText;
    public Transform itemListParent;

    [Tooltip("Row-based list prefab. Used when cardPrefab is null.")]
    public ShopItemRowUI rowPrefab;

    [Tooltip("Grid card prefab (ShopItemCardUI). When assigned, the shop displays a grid instead of a list.")]
    public ShopItemCardUI cardPrefab;

    // ── Category Tabs ────────────────────────────────────────────────────────
    // Wire each ShopTabButton here.  The "All" tab must have isAllTab = true.
    [Header("Category Tabs")]
    [Tooltip("The 'All' tab button (ShopTabButton with isAllTab = true).")]
    public ShopTabButton tabAll;

    [Tooltip("The 'Weapons' tab button.")]
    public ShopTabButton tabWeapon;

    [Tooltip("The 'Armor' tab button.")]
    public ShopTabButton tabArmor;

    [Tooltip("The 'Food & Drink' tab button (maps to Consumable items).")]
    public ShopTabButton tabFoodDrink;

    // ── Selected Item Info ───────────────────────────────────────────────────
    [Header("Selected Item Info")]
    public Image selectedIcon;
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI selectedDescriptionText;
    public TextMeshProUGUI selectedStatsText;
    public TextMeshProUGUI selectedPriceText;
    public TextMeshProUGUI selectedClassText;
    public TextMeshProUGUI playerMoneyText;

    // ── Buttons ──────────────────────────────────────────────────────────────
    [Header("Buttons")]
    public Button buyButton;
    public Button closeButton;

    // ── Runtime state ────────────────────────────────────────────────────────
    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    private ShopInteractable currentShop;
    private ShopItemData     selectedItem;

    /// <summary>
    /// Currently active category filter.
    /// null  = show all items.
    /// value = show only items whose itemType matches.
    /// </summary>
    private RomanItemType? activeCategory = null;

    // ── Tracks spawned grid cards so we can deselect the previous one ─────────
    private readonly List<ShopItemCardUI> spawnedCards = new List<ShopItemCardUI>();
    private ShopItemCardUI selectedCard = null;

    // ── All registered tab buttons (populated in Awake) ──────────────────────
    private readonly List<ShopTabButton> allTabs = new List<ShopTabButton>();

    // ────────────────────────────────────────────────────────────────────────
    #region Unity Messages

    private void Awake()
    {
        Instance = this;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        GameUIState.SetShopOpen(false);

        if (buyButton  != null) buyButton.onClick.AddListener(BuySelectedItem);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

        // Register all tabs so we can loop over them later
        if (tabAll       != null) allTabs.Add(tabAll);
        if (tabWeapon    != null) allTabs.Add(tabWeapon);
        if (tabArmor     != null) allTabs.Add(tabArmor);
        if (tabFoodDrink != null) allTabs.Add(tabFoodDrink);
    }

    private void OnDisable()
    {
        GameUIState.SetShopOpen(false);
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Open / Close

    public void OpenShop(ShopInteractable shop)
    {
        if (rootPanel == null)
        {
            Debug.LogWarning("ShopUI is missing Root Panel.");
            return;
        }

        // Do not stack inventory and shop on top of each other.
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            InventoryUI.Instance.CloseInventory();

        currentShop    = shop;
        selectedItem   = null;
        activeCategory = null; // always start on "All"

        // Show/hide the All tab depending on the shop config
        if (tabAll != null)
            tabAll.gameObject.SetActive(shop.showAllTab);

        rootPanel.SetActive(true);
        GameUIState.SetShopOpen(true);

        // Update title
        if (shopTitleText != null)
            shopTitleText.text = !string.IsNullOrEmpty(shop.shopName) ? shop.shopName : "Shop";

        UpdateTabBadges();
        UpdateTabHighlights();
        BuildShopList();
        UpdateSelectedInfo();
        UpdateMoneyText();
    }

    public void CloseShop()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        GameUIState.SetShopOpen(false);
        currentShop  = null;
        selectedItem = null;
        UpdateSelectedInfo();
    }

    public void ToggleShop(ShopInteractable shop)
    {
        if (IsOpen) CloseShop();
        else        OpenShop(shop);
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Category Tabs

    /// <summary>
    /// Called by <see cref="ShopTabButton"/> when the player clicks a tab.
    /// Pass null to show all items.
    /// </summary>
    public void SelectCategory(RomanItemType? category)
    {
        activeCategory = category;
        UpdateTabHighlights();
        BuildShopList();

        // Clear selected item when switching tabs
        selectedItem = null;
        UpdateSelectedInfo();
    }

    private void UpdateTabHighlights()
    {
        foreach (ShopTabButton tab in allTabs)
        {
            if (tab == null) continue;

            bool isActive;

            if (tab.isAllTab)
                isActive = activeCategory == null;
            else
                isActive = activeCategory.HasValue && activeCategory.Value == tab.category;

            tab.SetActive(isActive);
        }
    }

    /// <summary>
    /// Refreshes the item-count badge on every tab based on the current shop's inventory.
    /// </summary>
    private void UpdateTabBadges()
    {
        if (currentShop == null) return;

        foreach (ShopTabButton tab in allTabs)
        {
            if (tab == null) continue;

            int count;

            if (tab.isAllTab)
            {
                count = currentShop.itemsForSale.FindAll(i => i != null && i.canBeSoldInShop).Count;
            }
            else
            {
                count = currentShop.itemsForSale.FindAll(
                    i => i != null && i.canBeSoldInShop && i.itemType == tab.category
                ).Count;
            }

            tab.SetCountBadge(count);
        }
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Item List

    private void BuildShopList()
    {
        // ── Guard checks ──────────────────────────────────────────────────
        if (itemListParent == null)
        {
            Debug.LogError("[ShopUI] BuildShopList: itemListParent is NULL — wire it in the Inspector.");
            return;
        }

        if (currentShop == null)
        {
            Debug.LogError("[ShopUI] BuildShopList: currentShop is NULL — OpenShop() was not called.");
            return;
        }

        // ── Clear ─────────────────────────────────────────────────────────
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        spawnedCards.Clear();
        selectedCard = null;

        bool useGrid = cardPrefab != null;

        Debug.Log($"[ShopUI] BuildShopList — shop='{currentShop.shopName}', " +
                  $"totalItems={currentShop.itemsForSale.Count}, " +
                  $"mode={(useGrid ? "GRID (cardPrefab)" : rowPrefab != null ? "ROW (rowPrefab)" : "NO PREFAB ASSIGNED")}, " +
                  $"activeCategory={(activeCategory.HasValue ? activeCategory.Value.ToString() : "All")}");

        int spawned    = 0;
        int skippedNull       = 0;
        int skippedNotForSale = 0;
        int skippedCategory   = 0;

        foreach (ShopItemData item in currentShop.itemsForSale)
        {
            if (item == null)
            {
                skippedNull++;
                continue;
            }

            if (!item.canBeSoldInShop)
            {
                skippedNotForSale++;
                Debug.Log($"[ShopUI]   SKIP (canBeSoldInShop=false): {item.itemName}");
                continue;
            }

            // Apply category filter (null = All)
            if (activeCategory.HasValue && item.itemType != activeCategory.Value)
            {
                skippedCategory++;
                continue;
            }

            if (useGrid)
            {
                ShopItemCardUI card = Instantiate(cardPrefab, itemListParent);
                card.Setup(this, item);
                spawnedCards.Add(card);
                spawned++;
            }
            else if (rowPrefab != null)
            {
                ShopItemRowUI row = Instantiate(rowPrefab, itemListParent);
                row.Setup(this, item);
                spawned++;
            }
            else
            {
                Debug.LogWarning($"[ShopUI]   Cannot spawn '{item.itemName}' — both cardPrefab and rowPrefab are null!");
            }
        }

        Debug.Log($"[ShopUI] BuildShopList done — spawned={spawned}, " +
                  $"skippedNull={skippedNull}, skippedNotForSale={skippedNotForSale}, " +
                  $"skippedByCategory={skippedCategory}");
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Item Selection & Info Panel

    /// <summary>
    /// Called by <see cref="ShopItemRowUI"/> (row mode).
    /// </summary>
    public void SelectItem(ShopItemData item)
    {
        selectedItem = item;
        UpdateSelectedInfo();
    }

    /// <summary>
    /// Called by <see cref="ShopItemCardUI"/> (grid mode).
    /// Deselects the previously selected card and highlights the new one.
    /// </summary>
    public void SelectItemCard(ShopItemCardUI card)
    {
        // Deselect previous card
        if (selectedCard != null && selectedCard != card)
            selectedCard.SetSelected(false);

        selectedCard = card;

        if (selectedCard != null)
            selectedCard.SetSelected(true);

        selectedItem = card != null ? card.Item : null;
        UpdateSelectedInfo();
    }

    private void UpdateSelectedInfo()
    {
        bool hasItem = selectedItem != null;

        if (selectedIcon != null)
        {
            selectedIcon.enabled      = hasItem && selectedItem.icon != null;
            selectedIcon.sprite       = hasItem ? selectedItem.icon : null;
            selectedIcon.preserveAspect = true;
        }

        if (selectedNameText != null)
            selectedNameText.text = hasItem ? selectedItem.itemName : "Select an item";

        if (selectedDescriptionText != null)
            selectedDescriptionText.text = hasItem ? selectedItem.description : "Choose an item from the shop list.";

        if (selectedStatsText != null)
            selectedStatsText.text = hasItem ? selectedItem.GetStatsText() : string.Empty;

        if (selectedPriceText != null)
            selectedPriceText.text = hasItem ? selectedItem.GetPriceText() : string.Empty;

        if (selectedClassText != null)
            selectedClassText.text = hasItem ? selectedItem.GetClassText() : string.Empty;

        if (buyButton != null)
            buyButton.interactable = hasItem;
    }

    #endregion

    // ────────────────────────────────────────────────────────────────────────
    #region Buying

    private void BuySelectedItem()
    {
        if (selectedItem == null)
            return;

        if (PlayerStats.Instance == null || PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerStats or PlayerInventory missing from Player.");
            return;
        }

        // Class restriction check
        CharacterClassData currentClass = CharacterClassData.SelectedClass;
        PlayerController   player       = Object.FindFirstObjectByType<PlayerController>();

        if (player != null && player.classData != null)
            currentClass = player.classData;

        if (!selectedItem.CanUseWithClass(currentClass))
        {
            Debug.Log($"{selectedItem.itemName} cannot be bought by your current class.");
            return;
        }

        if (!PlayerInventory.Instance.HasFreeSpaceFor(selectedItem))
        {
            Debug.Log("Inventory is full.");
            return;
        }

        if (!PlayerStats.Instance.TrySpendDenarii(selectedItem.priceDenarii))
        {
            Debug.Log("Not enough denarii.");
            return;
        }

        PlayerInventory.Instance.AddItem(selectedItem, 1);
        UpdateMoneyText();

        Debug.Log($"Bought {selectedItem.itemName}");
    }

    private void UpdateMoneyText()
    {
        if (playerMoneyText == null || PlayerStats.Instance == null)
            return;

        int money = PlayerStats.Instance.denarii;
        playerMoneyText.text = $"Your money: {RomanCurrency.FormatDenarii(money)}";
    }

    #endregion
}
