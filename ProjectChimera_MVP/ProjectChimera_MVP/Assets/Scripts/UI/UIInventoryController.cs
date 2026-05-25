using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryController : MonoBehaviour
{
    [Header("面板")]
    public GameObject panel;
    public Transform itemGrid;
    public GameObject slotPrefab;

    [Header("详情")]
    public GameObject detailPanel;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDesc;
    public TextMeshProUGUI detailQty;
    public Button btnEquip;
    public Button btnUse;
    public Button btnClose;

    [Header("换装角色选择")]
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
        btnClose.onClick.AddListener(Close);
        btnEquip.onClick.AddListener(OnEquipClick);
        btnUse.onClick.AddListener(OnUseClick);
        panel.SetActive(false);
        detailPanel.SetActive(false);
        charSelector.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) && GameManager.Instance?.currentState == GameState.WorldMap)
        {
            if (panel.activeSelf) Close();
            else Open();
        }

        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        detailPanel.SetActive(false);
        charSelector.SetActive(false);
    }

    void Refresh()
    {
        foreach (Transform t in itemGrid)
            Destroy(t.gameObject);

        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (var stack in gm.inventory)
        {
            var def = ItemDatabase.Get(stack.itemId);
            var slot = Instantiate(slotPrefab, itemGrid);
            var texts = slot.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = def != null ? def.itemName : stack.itemId;
            if (texts.Length > 1) texts[1].text = $"x{stack.quantity}";

            var btn = slot.GetComponentInChildren<Button>();
            if (btn == null) btn = slot.AddComponent<Button>();
            var captured = stack;
            btn.onClick.AddListener(() => ShowDetail(captured));
        }
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

        btnEquip.gameObject.SetActive(def != null && def.IsEquipment);
        btnUse.gameObject.SetActive(def != null && def.category == ItemCategory.Consumable);
    }

    public void OnEquipClick()
    {
        if (selectedStack == null) return;
        ShowCharSelector();
    }

    public void OnUseClick()
    {
        if (selectedStack == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var def = ItemDatabase.Get(selectedStack.itemId);
        if (def == null) return;
        if (def.category != ItemCategory.Consumable) return;

        bool used = false;
        if (def.itemId == "health_potion")
        {
            foreach (var unit in gm.playerTeamData)
            {
                int maxHp = unit.VIT * 5 + 40;
                unit.VIT = Mathf.Min(unit.VIT + 1, 20);
                used = true;
                break;
            }
            if (!used && gm.playerTeamData.Count > 0)
            {
                used = true;
                Debug.Log("[背包] 使用了生命药水（无实际效果，需战斗场景才扣血）");
            }
        }
        else if (def.itemId == "stress_herb")
        {
            foreach (var unit in gm.playerTeamData)
            {
                unit.stress = Mathf.Max(0, unit.stress - 15);
                used = true;
                break;
            }
        }

        if (used)
        {
            gm.RemoveItem(selectedStack.itemId, 1);
            Refresh();
            detailPanel.SetActive(false);
        }
    }

    void ShowCharSelector()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (Transform t in charButtonGrid)
            Destroy(t.gameObject);

        for (int i = 0; i < gm.playerTeamData.Count; i++)
        {
            var unit = gm.playerTeamData[i];
            var btn = Instantiate(charButtonPrefab, charButtonGrid);
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = unit.unitName;

            int idx = i;
            btn.GetComponent<Button>().onClick.AddListener(() => EquipTo(idx));
        }
        charSelector.SetActive(true);
    }

    void EquipTo(int charIdx)
    {
        if (selectedStack == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var def = ItemDatabase.Get(selectedStack.itemId);
        if (def == null) return;

        var unit = gm.playerTeamData[charIdx];

        if (def.weaponTemplate != null)
        {
            unit.equippedWeapon = CloneEquipment(def.weaponTemplate) as Weapon;
        }
        if (def.armorTemplate != null)
        {
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
