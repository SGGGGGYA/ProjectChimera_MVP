using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public float moveSpeed = 1.2f;
    public float destroyTime = 0.7f;

    private TextMeshPro textMesh;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    private float timer;

    void Start()
    
    {
        if (destroyTime <= 0f)
        {
            destroyTime = 0.7f;
        }
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;

        if (textMesh != null)
        {
            Color c = textMesh.color;
            float progress = Mathf.Clamp01(timer / destroyTime);
            c.a = 1f - progress;
            textMesh.color = c;
        }

        if (timer >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    public void Setup(int damageAmount)
    {
        if (textMesh != null)
        {
            textMesh.text = "-" + damageAmount.ToString();
        }
    }
}