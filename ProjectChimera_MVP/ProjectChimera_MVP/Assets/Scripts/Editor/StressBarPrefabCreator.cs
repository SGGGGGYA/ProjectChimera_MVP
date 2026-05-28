using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class StressBarPrefabCreator
{
    [MenuItem("Tools/Create StressBar Prefab")]
    public static void Create()
    {
        GameObject root = new GameObject("StressBar", typeof(RectTransform));
        root.layer = 5;

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 12);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        StressBarFollower follower = root.AddComponent<StressBarFollower>();
        follower.offset = new Vector3(0, 1.2f, 0);
        follower.lerpSpeed = 8f;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.layer = 5;
        fill.transform.SetParent(root.transform, false);

        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.pivot = new Vector2(0.5f, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        fill.AddComponent<CanvasRenderer>();
        Image img = fill.AddComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        img.fillAmount = 1f;
        img.color = Color.green;
        img.raycastTarget = false;

        follower.fillImage = img;

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.layer = 5;
        label.transform.SetParent(root.transform, false);

        RectTransform labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(1, 0);
        labelRt.anchorMax = new Vector2(1, 0);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.anchoredPosition = new Vector2(4, 0);
        labelRt.sizeDelta = new Vector2(50, 14);

        CanvasRenderer cr2 = label.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = "0/200";
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.green;
        tmp.raycastTarget = false;

        follower.stressText = tmp;

        string path = "Assets/Resources/StressBar.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Log.Info($"StressBar prefab created at: {path}");
    }
}
