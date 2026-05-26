using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryController : MonoBehaviour
{
    [Header("面板（留空则自动创建）")]
    public GameObject panel;
    public Transform itemGrid;
    public GameObject slotPrefab;
    public GameObject detailPanel;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDesc;
    public TextMeshProUGUI detailQty;
    public TextMeshProUGUI detailStats;
    public Button btnEquip;
    public Button btnUnequip;
    public Button btnUse;
    public Button btnClose;
    public GameObject charSelector;
    public Transform charButtonGrid;
    public GameObject charButtonPrefab;

    ItemStack selectedStack;
    int selectedCharIndex;

    public static UIInventoryController Instance { get; private set; }

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
        if (Input.GetKeyDown(KeyCode.I) && GameManager.Instance?.currentState == GameState.WorldMap)
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

        var root = new GameObject("InventoryPanel", typeof(RectTransform));
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
        titleTmp.text = "背包";
        titleTmp.fontSize = 20;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        UIFonts.Apply(titleTmp);

        var gridObj = new GameObject("ItemGrid", typeof(RectTransform));
        gridObj.layer = 5;
        gridObj.transform.SetParent(root.transform, false);
        var gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 0);
        gridRt.anchorMax = new Vector2(0.5f, 1);
        gridRt.pivot = new Vector2(0, 1);
        gridRt.offsetMin = new Vector2(8, 40);
        gridRt.offsetMax = new Vector2(-8, -8);
        gridObj.AddComponent<GridLayoutGroup>();
        var gridMask = gridObj.AddComponent<Mask>();
        gridMask.showMaskGraphic = false;
        var gridImg = gridObj.AddComponent<Image>();
        gridImg.color = new Color(0, 0, 0, 0.3f);
        itemGrid = gridObj.transform;

        var detailObj = new GameObject("DetailPanel", typeof(RectTransform));
        detailObj.layer = 5;
        detailObj.transform.SetParent(root.transform, false);
        var detailRt = detailObj.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0.5f, 0);
        detailRt.anchorMax = new Vector2(1, 1);
        detailRt.offsetMin = new Vector2(8, 8);
        detailRt.offsetMax = new Vector2(-8, -8);
        var detailImg = detailObj.AddComponent<Image>();
        detailImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        detailPanel = detailObj;

        detailName = MakeLabel(detailObj, "DetailName", 0, 1, -8, "物品详情", 16, Color.white);
        detailDesc = MakeLabel(detailObj, "DetailDesc", 0, 0.5f, 0, "描述", 13, Color.gray);
        detailQty = MakeLabel(detailObj, "DetailQty", 0, 0.3f, 0, "数量: 0", 12, Color.gray);
        detailStats = MakeLabel(detailObj, "DetailStats", 0, 0.1f, 0, "", 11, Color.green);

        btnClose = MakeButton(detailObj, "CloseBtn", 0, 60, "关闭", new Color(0.3f, 0.3f, 0.3f));
        btnEquip = MakeButton(detailObj, "EquipBtn", -90, 60, "装备", new Color(0.25f, 0.4f, 0.25f));
        btnUnequip = MakeButton(detailObj, "UnequipBtn", 70, 60, "卸下", new Color(0.5f, 0.3f, 0.2f));
        btnUse = MakeButton(detailObj, "UseBtn", 90, 60, "使用", new Color(0.3f, 0.3f, 0.5f));

        var charSelObj = new GameObject("CharSelector", typeof(RectTransform));
        charSelObj.layer = 5;
        charSelObj.transform.SetParent(root.transform, false);
        var csRt = charSelObj.GetComponent<RectTransform>();
        csRt.anchorMin = Vector2.zero;
        csRt.anchorMax = Vector2.one;
        csRt.offsetMin = Vector2.zero;
        csRt.offsetMax = Vector2.zero;
        var csBg = charSelObj.AddComponent<Image>();
        csBg.color = new Color(0, 0, 0, 0.6f);
        charSelector = charSelObj;

        var csGrid = new GameObject("CharButtonGrid", typeof(RectTransform));
        csGrid.layer = 5;
        csGrid.transform.SetParent(charSelObj.transform, false);
        var csGridRt = csGrid.GetComponent<RectTransform>();
        csGridRt.anchorMin = new Vector2(0.5f, 0.5f);
        csGridRt.anchorMax = new Vector2(0.5f, 0.5f);
        csGridRt.pivot = new Vector2(0.5f, 0.5f);
        csGridRt.sizeDelta = new Vector2(400, 60);
        var csLayout = csGrid.AddComponent<GridLayoutGroup>();
        csLayout.cellSize = new Vector2(100, 30);
        csLayout.spacing = new Vector2(8, 8);
        charButtonGrid = csGrid.transform;

        btnClose.onClick.AddListener(Close);
        btnEquip.onClick.AddListener(OnEquipClick);
        btnUnequip.onClick.AddListener(OnUnequipClick);
        btnUse.onClick.AddListener(OnUseClick);

        panel.SetActive(false);
        detailPanel.SetActive(false);
        charSelector.SetActive(false);
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
        var tmp = UIFonts.CreateLabel(lbl, "Label", label, 14, Color.white);

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
        detailPanel?.SetActive(false);
        charSelector?.SetActive(false);
    }

    void Refresh()
    {
        if (itemGrid == null) return;
        foreach (Transform t in itemGrid)
            Destroy(t.gameObject);

        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (var stack in gm.inventory)
        {
            var def = ItemDatabase.Get(stack.itemId);
            var slot = MakeSlot(stack, def);
            slot.transform.SetParent(itemGrid, false);
        }
    }

    GameObject MakeSlot(ItemStack stack, ItemDefinition def)
    {
        var go = new GameObject("Slot", typeof(RectTransform));
        go.layer = 5;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 36);

        var nameTmp = MakeLabel(go, "Name", 0, 0.5f, 0, def != null ? def.itemName : stack.itemId, 14, Color.white);
        nameTmp.alignment = TextAlignmentOptions.Left;
        var nrt = nameTmp.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0, 0);
        nrt.anchorMax = new Vector2(1, 1);
        nrt.offsetMin = new Vector2(4, 0);
        nrt.offsetMax = new Vector2(-50, 0);

        var qtyTmp = MakeLabel(go, "Qty", 0, 0.5f, 0, $"x{stack.quantity}", 12, Color.gray);
        qtyTmp.alignment = TextAlignmentOptions.Right;
        var qrt = qtyTmp.GetComponent<RectTransform>();
        qrt.anchorMin = new Vector2(0, 0);
        qrt.anchorMax = new Vector2(1, 1);
        qrt.offsetMin = new Vector2(-46, 0);
        qrt.offsetMax = new Vector2(-4, 0);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        btn.colors = colors;
        btn.onClick.AddListener(() => ShowDetail(stack));

        return go;
    }

    void ShowDetail(ItemStack stack)
    {
        selectedStack = stack;
        var def = ItemDatabase.Get(stack.itemId);
        detailPanel.SetActive(true);
        charSelector.SetActive(false);

        detailName.text = def != null ? def.itemName : stack.itemId;
        detailDesc.text = def != null ? def.description : "";
        detailQty.text = $"数量: {stack.quantity}";

        bool isEquip = def != null && def.IsEquipment;
        btnEquip.gameObject.SetActive(isEquip);
        btnUnequip.gameObject.SetActive(isEquip);
        btnUse.gameObject.SetActive(def != null && def.category == ItemCategory.Consumable);

        // 显示装备属性
        if (detailStats != null)
        {
            if (isEquip && def != null)
            {
                var sb = new System.Text.StringBuilder();
                if (def.weaponBaseAttack > 0)
                    sb.AppendLine($"攻击力 +{def.weaponBaseAttack}");
                var template = def.weaponTemplate ?? (Equipment)def.armorTemplate;
                if (template != null)
                {
                    foreach (var mod in template.mods)
                    {
                        string label = StatLabel(mod.stat);
                        if (mod.isPercent)
                            sb.AppendLine($"{label} +{mod.amount}%");
                        else
                            sb.AppendLine($"{label} +{mod.amount}");
                    }
                }
                detailStats.text = sb.ToString();
                detailStats.gameObject.SetActive(true);
            }
            else
            {
                detailStats.gameObject.SetActive(false);
            }
        }
    }

    static string StatLabel(StatType stat)
    {
        switch (stat)
        {
            case StatType.VIT: return "体质";
            case StatType.STR: return "力量";
            case StatType.AGI: return "敏捷";
            case StatType.INT: return "智力";
            case StatType.DEF: return "防御";
            case StatType.MaxHP: return "生命上限";
            case StatType.SPD: return "速度";
            case StatType.ACC: return "命中";
            case StatType.DOD: return "闪避";
            case StatType.CRT: return "暴击";
            default: return stat.ToString();
        }
    }

    public void OnEquipClick()
    {
        if (selectedStack == null) return;
        ShowCharSelector();
    }

    public void OnUnequipClick()
    {
        if (selectedStack == null) return;
        var def = ItemDatabase.Get(selectedStack.itemId);
        if (def == null || !def.IsEquipment) return;
        bool isWeapon = def.weaponTemplate != null;
        ShowUnequipSelector(isWeapon);
    }

    void ShowUnequipSelector(bool isWeapon)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (Transform t in charButtonGrid)
            Destroy(t.gameObject);

        var csGridRt = charButtonGrid.GetComponent<RectTransform>();
        csGridRt.sizeDelta = new Vector2(400, 60);

        var csLayout = charButtonGrid.GetComponent<GridLayoutGroup>();
        if (csLayout != null)
            csLayout.cellSize = new Vector2(160, 30);

        int count = 0;
        for (int i = 0; i < gm.playerTeamData.Count; i++)
        {
            var unit = gm.playerTeamData[i];
            bool hasEquipped = isWeapon ? (unit.equippedWeapon != null) : (unit.equippedArmor != null);
            if (!hasEquipped) continue;

            string slotName = isWeapon ? unit.equippedWeapon.equipmentName : unit.equippedArmor.equipmentName;
            var go = new GameObject("UnequipBtn", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(charButtonGrid, false);

            var tmp = MakeLabel(go, "Label", 0, 0.5f, 0, $"{unit.unitName} ({slotName})", 12, Color.white);
            var lrt = tmp.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.35f, 0.25f, 0.2f);

            int idx = i;
            bool isW = isWeapon;
            go.AddComponent<Button>().onClick.AddListener(() => UnequipFrom(idx, isW));
            count++;
        }

        if (count == 0)
        {
            var go = new GameObject("NoEquipLabel", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(charButtonGrid, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            MakeLabel(go, "Label", 0, 0.5f, 0, "没有角色装备此部位", 13, Color.gray);
        }

        charSelector.SetActive(true);
    }

    void UnequipFrom(int charIdx, bool isWeapon)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (charIdx < 0 || charIdx >= gm.playerTeamData.Count) return;

        var unit = gm.playerTeamData[charIdx];

        if (isWeapon && unit.equippedWeapon != null)
        {
            gm.AddItem(unit.equippedWeapon.id, 1);
            unit.equippedWeapon = null;
            unit.weaponAttack = 0;
        }
        else if (!isWeapon && unit.equippedArmor != null)
        {
            gm.AddItem(unit.equippedArmor.id, 1);
            unit.equippedArmor = null;
        }

        charSelector.SetActive(false);
        Refresh();
        detailPanel.SetActive(false);
        Debug.Log($"[背包] 已卸下 {unit.unitName} 的装备");
    }

    public void OnUseClick()
    {
        if (selectedStack == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var def = ItemDatabase.Get(selectedStack.itemId);
        if (def == null || def.category != ItemCategory.Consumable) return;

        if (def.healAmount > 0)
        {
            var healAmt = def.healAmount;
            var itemName = def.itemName;
            var stack = selectedStack;
            ShowCharSelectorGeneric((unit) =>
            {
                unit.currentHP = Mathf.Min(unit.currentHP + healAmt, unit.maxHp > 0 ? unit.maxHp : unit.ComputeMaxHP());
                gm.RemoveItem(stack.itemId, 1);
                Refresh();
                detailPanel.SetActive(false);
                charSelector.SetActive(false);
                Debug.Log($"[背包] {itemName} 对 {unit.unitName} 使用了，回复 {healAmt} HP");
            });
            return;
        }
        else if (def.stressRelief > 0)
        {
            var reliefAmt = def.stressRelief;
            var itemName = def.itemName;
            var stack = selectedStack;
            ShowCharSelectorGeneric((unit) =>
            {
                unit.stress = Mathf.Max(0, unit.stress - reliefAmt);
                gm.RemoveItem(stack.itemId, 1);
                Refresh();
                detailPanel.SetActive(false);
                charSelector.SetActive(false);
                Debug.Log($"[背包] {itemName} 对 {unit.unitName} 使用了，减压 {reliefAmt}");
            });
            return;
        }

        gm.RemoveItem(selectedStack.itemId, 1);
        Refresh();
        detailPanel.SetActive(false);
    }

    void ShowCharSelectorGeneric(UnityEngine.Events.UnityAction<UnitBattleData> onSelect)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (Transform t in charButtonGrid)
            Destroy(t.gameObject);

        var csLayout = charButtonGrid.GetComponent<GridLayoutGroup>();
        if (csLayout != null)
            csLayout.cellSize = new Vector2(100, 30);

        for (int i = 0; i < gm.playerTeamData.Count; i++)
        {
            var unit = gm.playerTeamData[i];
            var go = new GameObject("CharBtn", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(charButtonGrid, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 30);

            var tmp = MakeLabel(go, "Label", 0, 0.5f, 0, unit.unitName, 14, Color.white);
            var lrt = tmp.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f);

            int idx = i;
            go.AddComponent<Button>().onClick.AddListener(() => onSelect(gm.playerTeamData[idx]));
        }
        charSelector.SetActive(true);
    }

    void ShowCharSelector()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (Transform t in charButtonGrid)
            Destroy(t.gameObject);

        var csLayout = charButtonGrid.GetComponent<GridLayoutGroup>();
        if (csLayout != null)
            csLayout.cellSize = new Vector2(100, 30);

        for (int i = 0; i < gm.playerTeamData.Count; i++)
        {
            var unit = gm.playerTeamData[i];
            var go = new GameObject("CharBtn", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(charButtonGrid, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 30);

            var tmp = MakeLabel(go, "Label", 0, 0.5f, 0, unit.unitName, 14, Color.white);
            var lrt = tmp.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f);

            int idx = i;
            go.AddComponent<Button>().onClick.AddListener(() => EquipTo(idx));
        }
        charSelector.SetActive(true);
    }

    void EquipTo(int charIdx)
    {
        if (selectedStack == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (charIdx < 0 || charIdx >= gm.playerTeamData.Count) return;

        var def = ItemDatabase.Get(selectedStack.itemId);
        if (def == null) return;

        var unit = gm.playerTeamData[charIdx];

        if (def.weaponTemplate != null)
        {
            if (unit.equippedWeapon != null)
                gm.AddItem(unit.equippedWeapon.id, 1);
            unit.equippedWeapon = CloneEquipment(def.weaponTemplate) as Weapon;
            unit.weaponAttack = def.weaponBaseAttack;
        }
        if (def.armorTemplate != null)
        {
            if (unit.equippedArmor != null)
                gm.AddItem(unit.equippedArmor.id, 1);
            unit.equippedArmor = CloneEquipment(def.armorTemplate) as Armor;
        }

        gm.RemoveItem(selectedStack.itemId, 1);
        charSelector.SetActive(false);
        Refresh();
        detailPanel.SetActive(false);
        Debug.Log($"[背包] {def.itemName} 已装备到 {unit.unitName}");
    }

    static Equipment CloneEquipment(Equipment src)
    {
        if (src == null) return null;
        var dst = src is Weapon ? (Equipment)new Weapon() : new Armor();
        dst.id = src.id;
        dst.equipmentName = src.equipmentName;
        dst.mods = new List<StatMod>(src.mods);
        return dst;
    }
}
