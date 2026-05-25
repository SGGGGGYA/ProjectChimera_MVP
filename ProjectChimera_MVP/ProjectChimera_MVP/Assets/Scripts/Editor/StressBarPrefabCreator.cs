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
        rt.sizeDelta = new Vector2(60, 8);
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

        CanvasRenderer cr = fill.AddComponent<CanvasRenderer>();
        Image img = fill.AddComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        img.fillAmount = 1f;
        img.color = Color.green;
        img.raycastTarget = false;

        follower.fillImage = img;

        string path = "Assets/Resources/StressBar.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"StressBar prefab created at: {path}");
    }
}
