using UnityEngine;
using TMPro;
using ProjectChimera.Core;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float timer;
    private float moveSpeed;
    private float destroyTime;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    void Start()
    {
        transform.position += new Vector3(RandomProvider.Current.Range(-0.3f, 0.3f), 0, 0);
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        timer += Time.deltaTime;
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 1f - Mathf.Clamp01(timer / destroyTime);
            textMesh.color = c;
        }
        if (timer >= destroyTime)
            Destroy(gameObject);
    }

    public void Setup(int amount, PopupType type = PopupType.Damage)
    {
        if (textMesh == null) return;
        switch (type)
        {
            case PopupType.Crit:
                textMesh.text = $"暴击! -{amount}";
                textMesh.color = new Color(1f, 0.5f, 0f);
                textMesh.fontSize = 5;
                moveSpeed = 1.5f;
                destroyTime = 1.0f;
                break;
            case PopupType.Heal:
                textMesh.text = $"+{amount}";
                textMesh.color = Color.green;
                textMesh.fontSize = 3.5f;
                moveSpeed = 1.2f;
                destroyTime = 0.7f;
                break;
            case PopupType.Miss:
                textMesh.text = "Miss";
                textMesh.color = Color.gray;
                textMesh.fontSize = 2.5f;
                moveSpeed = 0.8f;
                destroyTime = 0.5f;
                break;
            case PopupType.Shield:
                textMesh.text = $"-{amount}";
                textMesh.color = Color.cyan;
                textMesh.fontSize = 2.5f;
                moveSpeed = 1.0f;
                destroyTime = 0.6f;
                break;
            case PopupType.Stress:
                textMesh.text = $"+{amount}";
                textMesh.color = new Color(0.6f, 0.2f, 0.8f);
                textMesh.fontSize = 3f;
                moveSpeed = 1.2f;
                destroyTime = 0.7f;
                break;
            default:
                textMesh.text = $"-{amount}";
                textMesh.color = Color.red;
                textMesh.fontSize = 3.5f;
                moveSpeed = 1.2f;
                destroyTime = 0.7f;
                break;
        }
    }

    public void SetupText(string text, Color color, float fontSize = 4f)
    {
        if (textMesh == null) return;
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        moveSpeed = 0.8f;
        destroyTime = 1.5f;
    }
}
