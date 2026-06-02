using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to each category tab button in the Shop UI.
/// Set <see cref="category"/> in the Inspector (leave null for the "All" tab).
/// </summary>
public class ShopTabButton : MonoBehaviour
{
    [Header("Category")]
    [Tooltip("Which item type this tab filters to. Leave unset (None) for the 'All' tab.")]
    public bool isAllTab = false;
    public RomanItemType category = RomanItemType.Weapon;

    [Header("Visual State")]
    [Tooltip("Color when this tab is the active/selected one.")]
    public Color activeColor = new Color(0.95f, 0.80f, 0.30f);   // gold

    [Tooltip("Color when this tab is idle/unselected.")]
    public Color inactiveColor = new Color(0.25f, 0.20f, 0.12f); // dark brown

    [Header("References (auto-found if left empty)")]
    public Image backgroundImage;
    public TextMeshProUGUI labelText;
    public Button button;

    private void Awake()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (labelText == null)       labelText       = GetComponentInChildren<TextMeshProUGUI>();
        if (button == null)          button          = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnTabClicked);
        }

        // Start in inactive state
        SetActive(false);
    }

    private void OnTabClicked()
    {
        if (ShopUI.Instance == null) return;

        if (isAllTab)
            ShopUI.Instance.SelectCategory(null);
        else
            ShopUI.Instance.SelectCategory(category);
    }

    /// <summary>
    /// Called by <see cref="ShopUI"/> to reflect the currently selected tab.
    /// </summary>
    public void SetActive(bool active)
    {
        if (backgroundImage != null)
            backgroundImage.color = active ? activeColor : inactiveColor;

        if (labelText != null)
            labelText.color = active ? Color.black : new Color(0.85f, 0.75f, 0.55f);
    }

    /// <summary>
    /// Optionally show the number of items in this category on the tab label.
    /// Pass -1 to hide the badge.
    /// </summary>
    public void SetCountBadge(int count)
    {
        if (labelText == null) return;

        // Preserve original label, just append the count
        string baseName = isAllTab ? "All" : CategoryDisplayName();
        labelText.text = count >= 0 ? $"{baseName} ({count})" : baseName;
    }

    private string CategoryDisplayName()
    {
        if (isAllTab) return "All";

        switch (category)
        {
            case RomanItemType.Weapon:     return "Weapons";
            case RomanItemType.Armor:      return "Armor";
            case RomanItemType.Consumable: return "Food & Drink";
            case RomanItemType.Material:   return "Materials";
            default:                       return category.ToString();
        }
    }
}
