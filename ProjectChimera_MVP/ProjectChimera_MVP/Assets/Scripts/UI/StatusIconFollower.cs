using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class StatusIconFollower : MonoBehaviour
{
    private TextMeshPro label;
    private UnitData unit;
    private Transform unitTransform;

    static readonly Dictionary<StatusType, string> IconMap = new Dictionary<StatusType, string>
    {
        { StatusType.Stun, "<color=yellow>晕</color>" },
        { StatusType.Bleed, "<color=red>血</color>" },
        { StatusType.Mark, "<color=cyan>标</color>" },
        { StatusType.Taunt, "<color=#ff8800>嘲</color>" },
        { StatusType.Protected, "<color=green>护</color>" },
        { StatusType.Berserk, "<color=#ff4400>狂</color>" },
        { StatusType.Enlightened, "<color=#88ddff>启</color>" },
    };

    void Start()
    {
        unit = GetComponent<UnitData>();
        if (unit == null) { Destroy(this); return; }
        unitTransform = transform;

        var go = new GameObject("StatusIcon", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 2.2f, 0);

        label = go.AddComponent<TextMeshPro>();
        label.fontSize = 3;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (UIFonts.Chinese != null) label.font = UIFonts.Chinese;
    }

    void Update()
    {
        if (unit == null || label == null) return;
        var active = unit.statusEffects.FindAll(e => !e.expired);
        if (active.Count == 0)
        {
            if (label.gameObject.activeSelf) label.gameObject.SetActive(false);
            return;
        }
        if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);

        var sb = new StringBuilder();
        foreach (var e in active)
        {
            if (IconMap.TryGetValue(e.type, out string icon))
                sb.Append(icon);
            else
                sb.Append($"{e.type}");
            sb.Append(' ');
        }
        label.text = sb.ToString().TrimEnd();
    }
}
