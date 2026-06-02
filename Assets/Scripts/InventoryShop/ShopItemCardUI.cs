using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used on each grid cell in the shop panel.
/// Shows item icon, name, and price in a compact card format.
/// Attach to your PF_UI_ShopItemCard prefab.
/// </summary>
public class ShopItemCardUI : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("The icon image inside the cell.")]
    public Image iconImage;

    [Tooltip("Item name label (can be null if your grid is icon-only).")]
    public TextMeshProUGUI nameText;

    [Tooltip("Price label shown below the icon.")]
    public TextMeshProUGUI priceText;

    [Header("Optional")]
    [Tooltip("Sub-type badge (e.g. 'Sword', 'Potion'). Hides when empty.")]
    public TextMeshProUGUI subTypeText;

    [Tooltip("Highlight overlay shown when this card is selected.")]
    public GameObject selectedHighlight;

    [Tooltip("Button component — auto-found on this GameObject if left empty.")]
    public Button button;

    // ── Runtime state ────────────────────────────────────────────────────────
    private ShopUI      shopUI;
    private ShopItemData item;
    private bool         isSelected;

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="ShopUI"/> when building the grid.
    /// </summary>
    public void Setup(ShopUI owner, ShopItemData itemData)
    {
        shopUI = owner;
        item   = itemData;

        // ── Icon ──────────────────────────────────────────────────────────
        if (iconImage != null)
        {
            iconImage.enabled        = itemData.icon != null;
            iconImage.sprite         = itemData.icon;
            iconImage.preserveAspect = true;
        }

        // ── Name ──────────────────────────────────────────────────────────
        if (nameText != null)
            nameText.text = itemData.itemName;

        // ── Price ─────────────────────────────────────────────────────────
        if (priceText != null)
            priceText.text = itemData.GetPriceText();

        // ── Sub-type badge ────────────────────────────────────────────────
        if (subTypeText != null)
        {
            string label       = itemData.GetSubTypeLabel();
            subTypeText.text   = label;
            subTypeText.enabled = !string.IsNullOrEmpty(label);
        }

        // ── Selection highlight (start deselected) ────────────────────────
        SetSelected(false);

        // ── Button ────────────────────────────────────────────────────────
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }
    }

    /// <summary>
    /// Visually marks/unmarks this card as selected.
    /// Called by <see cref="ShopUI"/> when the player picks a different item.
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    /// <summary>
    /// Returns the item this card represents.
    /// </summary>
    public ShopItemData Item => item;

    // ────────────────────────────────────────────────────────────────────────
    private void OnCardClicked()
    {
        if (shopUI != null)
            shopUI.SelectItemCard(this);
    }
}
