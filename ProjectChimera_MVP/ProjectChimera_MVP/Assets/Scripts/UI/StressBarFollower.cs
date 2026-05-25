using UnityEngine;
using UnityEngine.UI;

public class StressBarFollower : MonoBehaviour
{
    public Image fillImage;
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    public float lerpSpeed = 8f;

    private Transform target;
    private UnitData unitData;

    public void SetTarget(Transform t, UnitData unit)
    {
        target = t;
        unitData = unit;
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01((float)unit.stress / 200f);
    }

    void Update()
    {
        if (target != null)
            transform.position = Camera.main.WorldToScreenPoint(target.position + offset);

        if (fillImage == null || unitData == null) return;

        float targetFill = Mathf.Clamp01((float)unitData.stress / 200f);
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);

        if (unitData.stress >= 150) fillImage.color = Color.red;
        else if (unitData.stress >= 100) fillImage.color = new Color(1f, 0.55f, 0f);
        else if (unitData.stress >= 50) fillImage.color = Color.yellow;
        else fillImage.color = Color.green;
    }
}
