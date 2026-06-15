using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StressBarFollower : MonoBehaviour, IUnitStressBar
{
    public Image fillImage;
    public TextMeshProUGUI stressText;
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    public float lerpSpeed = 8f;

    private Transform target;
    private UnitData unitData;

    public void SetTarget(Transform t, UnitData unit)
    {
        target = t;
        unitData = unit;
        Refresh();
    }

    public void Refresh()
    {
        if (unitData == null) return;
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01((float)unitData.stress / 200f);
        if (stressText != null)
            stressText.text = $"{unitData.stress}/200";
    }

    void Update()
    {
        if (target != null)
            transform.position = Camera.main.WorldToScreenPoint(target.position + offset);

        if (fillImage == null || unitData == null) return;

        float targetFill = Mathf.Clamp01((float)unitData.stress / 200f);
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);

        Color c;
        if (unitData.stress >= 150) c = Color.red;
        else if (unitData.stress >= 100) c = new Color(1f, 0.55f, 0f);
        else if (unitData.stress >= 50) c = Color.yellow;
        else c = Color.green;
        fillImage.color = c;

        if (stressText != null)
        {
            stressText.text = $"{unitData.stress}/200";
            stressText.color = c;
        }
    }
}
