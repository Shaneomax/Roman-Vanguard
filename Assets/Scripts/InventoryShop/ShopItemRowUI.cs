using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemRowUI : MonoBehaviour
{
    [Header("Required")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;

    [Header("Optional")]
    public TextMeshProUGUI classText;
    [Tooltip("Shows the item sub-type label (e.g. 'Sword', 'Helmet', 'Potion'). Leave empty to skip.")]
    public TextMeshProUGUI subTypeText;
    public Button button;

    private ShopUI shopUI;
    private ShopItemData item;

    public void Setup(ShopUI shopUI, ShopItemData item)
    {
        this.shopUI = shopUI;
        this.item   = item;

        if (iconImage != null)
        {
            iconImage.enabled = item.icon != null;
            iconImage.sprite  = item.icon;
            iconImage.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = item.itemName;

        if (priceText != null)
            priceText.text = item.GetPriceText();

        if (classText != null)
            classText.text = item.GetClassText();

        if (subTypeText != null)
        {
            string label = item.GetSubTypeLabel();
            subTypeText.text    = label;
            subTypeText.enabled = !string.IsNullOrEmpty(label);
        }

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (this.shopUI != null)
                    this.shopUI.SelectItem(this.item);
            });
        }
    }
}
