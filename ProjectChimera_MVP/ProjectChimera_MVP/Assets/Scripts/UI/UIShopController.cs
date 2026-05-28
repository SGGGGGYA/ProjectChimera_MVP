using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopController : MonoBehaviour
{
    public GameObject panel;
    Transform itemList;
    GameObject detailPanel;
    TextMeshProUGUI detailName;
    TextMeshProUGUI detailDesc;
    TextMeshProUGUI detailPrice;
    TextMeshProUGUI detailQty;
    Button btnBuy;
    Button btnSell;
    TextMeshProUGUI goldLabel;
    ItemDefinition selectedDef;

    public static UIShopController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (panel == null) EnsurePanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && GameManager.Instance?.currentState == GameState.WorldMap)
        {
            if (panel != null && panel.activeSelf) Close();
            else Open();
        }
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    void EnsurePanel()
    {
        if (panel != null) return;

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("InventoryCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }
        else if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        UIFonts.EnsureEventSystem();

        var root = new GameObject("ShopPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        panel = root;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 400);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var title = new GameObject("Title", typeof(RectTransform));
        title.layer = 5;
        title.transform.SetParent(root.transform, false);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.sizeDelta = new Vector2(200, 30);
        titleRt.anchoredPosition = new Vector2(0, -8);
        title.AddComponent<CanvasRenderer>();
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "商店";
        titleTmp.fontSize = 20;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        UIFonts.Apply(titleTmp);

        var scrollObj = new GameObject("ShopItemScroll", typeof(RectTransform));
        scrollObj.layer = 5;
        scrollObj.transform.SetParent(root.transform, false);
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(0.5f, 1);
        scrollRt.offsetMin = new Vector2(8, 40);
        scrollRt.offsetMax = new Vector2(-8, -8);
        var scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.layer = 5;
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.layer = 5;
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0, 1);
        contentRt.sizeDelta = new Vector2(0, 0);
        var vLayout = content.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 4;
        vLayout.padding = new RectOffset(4, 4, 4, 4);
        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;
        itemList = content.transform;

        detailPanel = new GameObject("DetailPanel", typeof(RectTransform));
        detailPanel.layer = 5;
        detailPanel.transform.SetParent(root.transform, false);
        var detailRt = detailPanel.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0.5f, 0);
        detailRt.anchorMax = new Vector2(1, 1);
        detailRt.offsetMin = new Vector2(8, 8);
        detailRt.offsetMax = new Vector2(-8, -8);
        var detailImg = detailPanel.AddComponent<Image>();
        detailImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        detailName = MakeLabel(detailPanel, "DetailName", 0.6f, 1, -8, "物品详情", 16, Color.white);
        detailDesc = MakeLabel(detailPanel, "DetailDesc", 0.35f, 0.5f, 0, "描述", 13, Color.gray);
        detailPrice = MakeLabel(detailPanel, "DetailPrice", 0.2f, 0.3f, 0, "价格", 13, new Color(1, 0.8f, 0));
        detailQty = MakeLabel(detailPanel, "DetailQty", 0.05f, 0.1f, 0, "持有: 0", 12, Color.gray);

        btnBuy = MakeButton(detailPanel, "BuyBtn", -65, 60, "购买", new Color(0.2f, 0.4f, 0.2f));
        btnSell = MakeButton(detailPanel, "SellBtn", 65, 60, "出售", new Color(0.4f, 0.3f, 0.2f));

        goldLabel = MakeLabel(root, "GoldLabel", 0, 0, 10, "金币: 100", 16, new Color(1, 0.8f, 0));
        var goldRt = goldLabel.GetComponent<RectTransform>();
        goldRt.anchorMin = new Vector2(0.5f, 0);
        goldRt.anchorMax = new Vector2(1, 0);

        btnBuy.onClick.AddListener(OnBuyClick);
        btnSell.onClick.AddListener(OnSellClick);

        panel.SetActive(false);
        detailPanel.SetActive(false);
    }

    TextMeshProUGUI MakeLabel(GameObject parent, string name, float anchorY, float pivotY, float yOff, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, anchorY);
        rt.anchorMax = new Vector2(1, anchorY + 0.2f);
        rt.pivot = new Vector2(0.5f, pivotY);
        rt.sizeDelta = new Vector2(0, 24);
        rt.anchoredPosition = new Vector2(0, yOff);
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        UIFonts.Apply(tmp);
        return tmp;
    }

    Button MakeButton(GameObject parent, string name, float xOff, float yOff, string label, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120, 32);
        rt.anchoredPosition = new Vector2(xOff, yOff);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var lbl = new GameObject("Label", typeof(RectTransform));
        lbl.layer = 5;
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        UIFonts.CreateLabel(lbl, "Label", label, 14, Color.white);

        return go.AddComponent<Button>();
    }

    public void Open()
    {
        if (panel == null) EnsurePanel();
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    void Refresh()
    {
        if (itemList == null) return;
        foreach (Transform t in itemList)
            Destroy(t.gameObject);

        var gm = GameManager.Instance;
        if (gm == null) return;

        UpdateGoldLabel();

        var shop = gm.shopData;
        if (shop == null) return;

        foreach (var itemId in shop.shopItemIds)
        {
            var def = ItemDatabase.Get(itemId);
            if (def == null) continue;
            MakeShopSlot(def);
        }
    }

    void MakeShopSlot(ItemDefinition def)
    {
        var go = new GameObject("ShopSlot", typeof(RectTransform));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);

        int buyPrice = GetBuyPrice(def);
        int playerQty = GameManager.Instance.GetItemQuantity(def.itemId);
        string slotText = $"{def.itemName}  ¥{buyPrice}  x{playerQty}";

        var tmp = MakeLabel(go, "SlotLabel", 0, 0.5f, 0, slotText, 13, Color.white);
        tmp.alignment = TextAlignmentOptions.Left;
        var lrt = tmp.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 0);
        lrt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        btn.colors = colors;
        btn.onClick.AddListener(() => ShowDetail(def));

        go.transform.SetParent(itemList, false);
    }

    void ShowDetail(ItemDefinition def)
    {
        selectedDef = def;
        detailPanel.SetActive(true);

        int buyPrice = GetBuyPrice(def);
        int sellPrice = GetSellPrice(def);
        int playerQty = GameManager.Instance.GetItemQuantity(def.itemId);

        detailName.text = def.itemName;
        detailDesc.text = def.description;
        detailPrice.text = $"购买: ¥{buyPrice}  /  出售: ¥{sellPrice}";
        detailQty.text = $"持有: {playerQty}";
    }

    void OnBuyClick()
    {
        if (selectedDef == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        int price = GetBuyPrice(selectedDef);
        if (gm.gold < price)
        {
            detailDesc.text = "金币不足";
            return;
        }

        gm.gold -= price;
        gm.AddItem(selectedDef.itemId, 1);
        UpdateGoldLabel();
        RefreshList();
        ShowDetail(selectedDef);
    }

    void OnSellClick()
    {
        if (selectedDef == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        int qty = gm.GetItemQuantity(selectedDef.itemId);
        if (qty <= 0)
        {
            detailDesc.text = "库存不足";
            return;
        }

        int price = GetSellPrice(selectedDef);
        gm.RemoveItem(selectedDef.itemId, 1);
        gm.gold += price;
        UpdateGoldLabel();
        RefreshList();
        ShowDetail(selectedDef);
    }

    void RefreshList()
    {
        if (itemList == null) return;
        foreach (Transform t in itemList)
            Destroy(t.gameObject);

        var gm = GameManager.Instance;
        if (gm?.shopData == null) return;

        foreach (var itemId in gm.shopData.shopItemIds)
        {
            var def = ItemDatabase.Get(itemId);
            if (def == null) continue;
            MakeShopSlot(def);
        }
    }

    void UpdateGoldLabel()
    {
        var gm = GameManager.Instance;
        if (gm != null && goldLabel != null)
            goldLabel.text = $"金币: {gm.gold}";
    }

    public static int GetBuyPrice(ItemDefinition def)
    {
        if (def.weaponTemplate != null) return def.weaponTemplate.GetBuyPrice();
        if (def.armorTemplate != null) return def.armorTemplate.GetBuyPrice();
        return def.buyPrice;
    }

    public static int GetSellPrice(ItemDefinition def)
    {
        if (def.weaponTemplate != null) return def.weaponTemplate.GetBuyPrice() / 2;
        if (def.armorTemplate != null) return def.armorTemplate.GetBuyPrice() / 2;
        return def.sellPrice;
    }
}
