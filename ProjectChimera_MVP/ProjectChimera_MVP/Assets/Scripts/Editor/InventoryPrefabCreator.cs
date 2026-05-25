using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class InventoryPrefabCreator
{
    [MenuItem("Tools/Create Inventory Prefabs")]
    public static void Create()
    {
        CreateSlotPrefab();
        CreateCharButtonPrefab();
        CreatePanelPrefab();
        Debug.Log("背包预制体已全部生成: Assets/Resources/InventoryPanel.prefab");
    }

    static void CreateSlotPrefab()
    {
        var root = new GameObject("InventorySlot", typeof(RectTransform));
        root.layer = 5;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 40);

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
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var imgObj = new GameObject("Icon", typeof(RectTransform));
        imgObj.layer = 5;
        imgObj.transform.SetParent(root.transform, false);
        var imgRt = imgObj.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0, 0.5f);
        imgRt.anchorMax = new Vector2(0, 0.5f);
        imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.sizeDelta = new Vector2(28, 28);
        imgRt.anchoredPosition = new Vector2(18, 0);
        imgObj.AddComponent<CanvasRenderer>();
        var icon = imgObj.AddComponent<Image>();
        icon.color = Color.white;

        var nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.layer = 5;
        nameObj.transform.SetParent(root.transform, false);
        var nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.5f);
        nameRt.anchorMax = new Vector2(0, 0.5f);
        nameRt.pivot = new Vector2(0, 0.5f);
        nameRt.sizeDelta = new Vector2(90, 20);
        nameRt.anchoredPosition = new Vector2(36, 0);
        nameObj.AddComponent<CanvasRenderer>();
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "物品名";
        nameTmp.fontSize = 14;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.color = Color.white;

        var qtyObj = new GameObject("Quantity", typeof(RectTransform));
        qtyObj.layer = 5;
        qtyObj.transform.SetParent(root.transform, false);
        var qtyRt = qtyObj.GetComponent<RectTransform>();
        qtyRt.anchorMin = new Vector2(1, 0.5f);
        qtyRt.anchorMax = new Vector2(1, 0.5f);
        qtyRt.pivot = new Vector2(1, 0.5f);
        qtyRt.sizeDelta = new Vector2(40, 20);
        qtyRt.anchoredPosition = new Vector2(-4, 0);
        qtyObj.AddComponent<CanvasRenderer>();
        var qtyTmp = qtyObj.AddComponent<TextMeshProUGUI>();
        qtyTmp.text = "x1";
        qtyTmp.fontSize = 12;
        qtyTmp.alignment = TextAlignmentOptions.Right;
        qtyTmp.color = Color.gray;

        root.AddComponent<Button>();

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/InventorySlot.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreateCharButtonPrefab()
    {
        var root = new GameObject("CharButton", typeof(RectTransform));
        root.layer = 5;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 30);

        var txt = new GameObject("Label", typeof(RectTransform));
        txt.layer = 5;
        txt.transform.SetParent(root.transform, false);
        var txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        txt.AddComponent<CanvasRenderer>();
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = "角色";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        root.AddComponent<Button>();

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/CharButton.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreatePanelPrefab()
    {
        var root = new GameObject("InventoryPanel", typeof(RectTransform));
        root.layer = 5;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 400);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

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

        var gridObj = new GameObject("ItemGrid", typeof(RectTransform));
        gridObj.layer = 5;
        gridObj.transform.SetParent(root.transform, false);
        var gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0, 0);
        gridRt.anchorMax = new Vector2(0.5f, 1);
        gridRt.pivot = new Vector2(0, 1);
        gridRt.offsetMin = new Vector2(8, 40);
        gridRt.offsetMax = new Vector2(-8, -8);
        var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(160, 40);
        gridLayout.spacing = new Vector2(4, 4);
        gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        gridLayout.padding = new RectOffset(4, 4, 4, 4);
        var scrollRect = gridObj.AddComponent<ScrollRect>();
        scrollRect.content = gridRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.viewport = null; // auto
        var mask = gridObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var img = gridObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.3f);

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

        var detailName = new GameObject("DetailName", typeof(RectTransform));
        detailName.layer = 5;
        detailName.transform.SetParent(detailObj.transform, false);
        var dnRt = detailName.GetComponent<RectTransform>();
        dnRt.anchorMin = new Vector2(0, 1);
        dnRt.anchorMax = new Vector2(1, 1);
        dnRt.pivot = new Vector2(0.5f, 1);
        dnRt.sizeDelta = new Vector2(0, 24);
        dnRt.anchoredPosition = new Vector2(0, -8);
        detailName.AddComponent<CanvasRenderer>();
        var dnTmp = detailName.AddComponent<TextMeshProUGUI>();
        dnTmp.text = "物品详情";
        dnTmp.fontSize = 16;
        dnTmp.alignment = TextAlignmentOptions.Center;
        dnTmp.color = Color.white;

        var detailDesc = new GameObject("DetailDesc", typeof(RectTransform));
        detailDesc.layer = 5;
        detailDesc.transform.SetParent(detailObj.transform, false);
        var ddRt = detailDesc.GetComponent<RectTransform>();
        ddRt.anchorMin = new Vector2(0, 0.4f);
        ddRt.anchorMax = new Vector2(1, 0.6f);
        ddRt.offsetMin = new Vector2(8, 0);
        ddRt.offsetMax = new Vector2(-8, 0);
        detailDesc.AddComponent<CanvasRenderer>();
        var ddTmp = detailDesc.AddComponent<TextMeshProUGUI>();
        ddTmp.text = "描述";
        ddTmp.fontSize = 13;
        ddTmp.alignment = TextAlignmentOptions.Center;
        ddTmp.color = Color.gray;

        var detailQty = new GameObject("DetailQty", typeof(RectTransform));
        detailQty.layer = 5;
        detailQty.transform.SetParent(detailObj.transform, false);
        var dqRt = detailQty.GetComponent<RectTransform>();
        dqRt.anchorMin = new Vector2(0, 0.3f);
        dqRt.anchorMax = new Vector2(1, 0.3f);
        dqRt.pivot = new Vector2(0.5f, 0.5f);
        dqRt.sizeDelta = new Vector2(0, 20);
        detailQty.AddComponent<CanvasRenderer>();
        var dqTmp = detailQty.AddComponent<TextMeshProUGUI>();
        dqTmp.text = "数量: 0";
        dqTmp.fontSize = 12;
        dqTmp.alignment = TextAlignmentOptions.Center;
        dqTmp.color = Color.gray;

        var btnClose = new GameObject("CloseBtn", typeof(RectTransform));
        btnClose.layer = 5;
        btnClose.transform.SetParent(detailObj.transform, false);
        var bcRt = btnClose.GetComponent<RectTransform>();
        bcRt.anchorMin = new Vector2(0.5f, 0);
        bcRt.anchorMax = new Vector2(0.5f, 0);
        bcRt.pivot = new Vector2(0.5f, 0.5f);
        bcRt.sizeDelta = new Vector2(120, 32);
        bcRt.anchoredPosition = new Vector2(0, 60);
        btnClose.AddComponent<CanvasRenderer>();
        var bcImg = btnClose.AddComponent<Image>();
        bcImg.color = new Color(0.3f, 0.3f, 0.3f);
        var bcTmpObj = new GameObject("Label", typeof(RectTransform));
        bcTmpObj.layer = 5;
        bcTmpObj.transform.SetParent(btnClose.transform, false);
        var bcTmpRt = bcTmpObj.GetComponent<RectTransform>();
        bcTmpRt.anchorMin = Vector2.zero;
        bcTmpRt.anchorMax = Vector2.one;
        bcTmpRt.offsetMin = Vector2.zero;
        bcTmpRt.offsetMax = Vector2.zero;
        bcTmpObj.AddComponent<CanvasRenderer>();
        var bcTmp = bcTmpObj.AddComponent<TextMeshProUGUI>();
        bcTmp.text = "关闭";
        bcTmp.fontSize = 14;
        bcTmp.alignment = TextAlignmentOptions.Center;
        bcTmp.color = Color.white;
        var bcBtn = btnClose.AddComponent<Button>();

        var btnEquip = new GameObject("EquipBtn", typeof(RectTransform));
        btnEquip.layer = 5;
        btnEquip.transform.SetParent(detailObj.transform, false);
        var beRt = btnEquip.GetComponent<RectTransform>();
        beRt.anchorMin = new Vector2(0.5f, 0);
        beRt.anchorMax = new Vector2(0.5f, 0);
        beRt.pivot = new Vector2(0.5f, 0.5f);
        beRt.sizeDelta = new Vector2(120, 32);
        beRt.anchoredPosition = new Vector2(-70, 60);
        btnEquip.AddComponent<CanvasRenderer>();
        var beImg = btnEquip.AddComponent<Image>();
        beImg.color = new Color(0.25f, 0.4f, 0.25f);
        var beTmpObj = new GameObject("Label", typeof(RectTransform));
        beTmpObj.layer = 5;
        beTmpObj.transform.SetParent(btnEquip.transform, false);
        var beTmpRt = beTmpObj.GetComponent<RectTransform>();
        beTmpRt.anchorMin = Vector2.zero;
        beTmpRt.anchorMax = Vector2.one;
        beTmpRt.offsetMin = Vector2.zero;
        beTmpRt.offsetMax = Vector2.zero;
        beTmpObj.AddComponent<CanvasRenderer>();
        var beTmp = beTmpObj.AddComponent<TextMeshProUGUI>();
        beTmp.text = "装备";
        beTmp.fontSize = 14;
        beTmp.alignment = TextAlignmentOptions.Center;
        beTmp.color = Color.white;
        var beBtn = btnEquip.AddComponent<Button>();

        var btnUse = new GameObject("UseBtn", typeof(RectTransform));
        btnUse.layer = 5;
        btnUse.transform.SetParent(detailObj.transform, false);
        var buRt = btnUse.GetComponent<RectTransform>();
        buRt.anchorMin = new Vector2(0.5f, 0);
        buRt.anchorMax = new Vector2(0.5f, 0);
        buRt.pivot = new Vector2(0.5f, 0.5f);
        buRt.sizeDelta = new Vector2(120, 32);
        buRt.anchoredPosition = new Vector2(70, 60);
        btnUse.AddComponent<CanvasRenderer>();
        var buImg = btnUse.AddComponent<Image>();
        buImg.color = new Color(0.3f, 0.3f, 0.5f);
        var buTmpObj = new GameObject("Label", typeof(RectTransform));
        buTmpObj.layer = 5;
        buTmpObj.transform.SetParent(btnUse.transform, false);
        var buTmpRt = buTmpObj.GetComponent<RectTransform>();
        buTmpRt.anchorMin = Vector2.zero;
        buTmpRt.anchorMax = Vector2.one;
        buTmpRt.offsetMin = Vector2.zero;
        buTmpRt.offsetMax = Vector2.zero;
        buTmpObj.AddComponent<CanvasRenderer>();
        var buTmp = buTmpObj.AddComponent<TextMeshProUGUI>();
        buTmp.text = "使用";
        buTmp.fontSize = 14;
        buTmp.alignment = TextAlignmentOptions.Center;
        buTmp.color = Color.white;
        var buBtn = btnUse.AddComponent<Button>();

        var charSelObj = new GameObject("CharSelector", typeof(RectTransform));
        charSelObj.layer = 5;
        charSelObj.transform.SetParent(root.transform, false);
        var csRt = charSelObj.GetComponent<RectTransform>();
        csRt.anchorMin = new Vector2(0, 0);
        csRt.anchorMax = new Vector2(1, 1);
        csRt.offsetMin = Vector2.zero;
        csRt.offsetMax = Vector2.zero;
        var csBg = charSelObj.AddComponent<Image>();
        csBg.color = new Color(0, 0, 0, 0.6f);

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

        var controller = root.AddComponent<UIInventoryController>();
        controller.panel = root;
        controller.itemGrid = gridObj.transform;
        controller.detailPanel = detailObj;
        controller.detailName = dnTmp;
        controller.detailDesc = ddTmp;
        controller.detailQty = dqTmp;
        controller.btnClose = bcBtn;
        controller.btnEquip = beBtn;
        controller.btnUse = buBtn;
        controller.charSelector = charSelObj;
        controller.charButtonGrid = csGrid.transform;
        controller.slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/InventorySlot.prefab");
        controller.charButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CharButton.prefab");

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/InventoryPanel.prefab");
        Object.DestroyImmediate(root);
    }
}
