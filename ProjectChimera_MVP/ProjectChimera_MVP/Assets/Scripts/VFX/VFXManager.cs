using System.Collections;
using UnityEngine;

public static class VFXManager
{
    private static MonoBehaviour coroutineHost;
    private static Vector3 originalCameraPos;

    public static void Initialize(MonoBehaviour host)
    {
        coroutineHost = host;
    }

    public static void ScreenShake(float intensity = 0.2f, float duration = 0.15f)
    {
        if (coroutineHost == null)
        {
            coroutineHost = Camera.main?.GetComponent<MonoBehaviour>();
            if (coroutineHost == null) return;
        }
        coroutineHost.StartCoroutine(ScreenShakeRoutine(intensity, duration));
    }

    private static IEnumerator ScreenShakeRoutine(float intensity, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        originalCameraPos = cam.transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            Vector3 offset = Random.insideUnitSphere * intensity;
            offset.z = 0f;
            cam.transform.position = originalCameraPos + offset;
            timer += Time.deltaTime;
            yield return null;
        }

        cam.transform.position = originalCameraPos;
    }

    public static void FlashDamage(UnitData target)
    {
        if (coroutineHost == null) return;
        coroutineHost.StartCoroutine(FlashRoutine(target, Color.white, 0.1f));
    }

    public static void FlashHeal(UnitData target)
    {
        if (coroutineHost == null) return;
        coroutineHost.StartCoroutine(FlashRoutine(target, Color.green, 0.15f));
    }

    private static IEnumerator FlashRoutine(UnitData target, Color flashColor, float duration)
    {
        if (target == null) yield break;

        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color original = sr.color;
        sr.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (sr != null)
            sr.color = original;
    }

    public static void PlayHitEffect(Vector3 position)
    {
        if (coroutineHost == null) return;
        coroutineHost.StartCoroutine(HitEffectRoutine(position));
    }

    private static IEnumerator HitEffectRoutine(Vector3 position)
    {
        GameObject go = new GameObject("HitEffect", typeof(SpriteRenderer));
        go.transform.position = position;
        go.transform.localScale = Vector3.zero;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.color = new Color(1f, 1f, 1f, 0.8f);

        Sprite circle = Resources.Load<Sprite>("vfx_hit_circle");
        if (circle != null)
            sr.sprite = circle;
        else
        {
            Texture2D tex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = x - 32f, dy = y - 32f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, dist <= 30f ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }

        float duration = 0.3f;
        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            go.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 0.5f, t);
            Color c = sr.color;
            c.a = Mathf.Lerp(0.8f, 0f, t);
            sr.color = c;
            timer += Time.deltaTime;
            yield return null;
        }

        if (go != null)
            Object.Destroy(go);
    }
}
