using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class VFXManager
{
    private static MonoBehaviour coroutineHost;
    private static Vector3 originalCameraPos;
    private static Canvas cachedCanvas;

    public static void Initialize(MonoBehaviour host)
    {
        coroutineHost = host;
    }

    static Canvas GetCanvas()
    {
        if (cachedCanvas == null)
            cachedCanvas = GameObject.FindObjectOfType<Canvas>();
        return cachedCanvas;
    }

    // ==================== 屏幕震动 ====================

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

    // ==================== 击中暂停(HitStop) ====================

    public static void HitStop(float duration = 0.05f)
    {
        if (coroutineHost == null) return;
        coroutineHost.StartCoroutine(HitStopRoutine(duration));
    }

    static IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    // ==================== 全屏闪色覆盖 ====================

    public static void FlashScreen(Color color, float duration = 0.2f)
    {
        if (coroutineHost == null) return;
        coroutineHost.StartCoroutine(FlashScreenRoutine(color, duration));
    }

    static IEnumerator FlashScreenRoutine(Color color, float duration)
    {
        Canvas canvas = GetCanvas();
        if (canvas == null) yield break;

        GameObject overlay = new GameObject("ScreenFlash", typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = overlay.GetComponent<Image>();
        img.color = color;

        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            Color c = img.color;
            c.a = Mathf.Lerp(color.a, 0f, t);
            img.color = c;
            timer += Time.deltaTime;
            yield return null;
        }

        if (overlay != null)
            Object.Destroy(overlay);
    }

    // ==================== Sprite 闪色(旧版, 非Spine单位) ====================

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

    // ==================== Spine 骨骼闪色 ====================

    public static void FlashSkeleton(UnitData target, Color flashColor, float duration = 0.1f)
    {
        if (coroutineHost == null) return;
        coroutineHost.StartCoroutine(FlashSkeletonRoutine(target, flashColor, duration));
    }

    static IEnumerator FlashSkeletonRoutine(UnitData target, Color flashColor, float duration)
    {
        if (target == null) yield break;
        var sk = target.GetSkeleton();
        if (sk == null || sk.skeleton == null) yield break;

        float origR = sk.skeleton.r, origG = sk.skeleton.g, origB = sk.skeleton.b, origA = sk.skeleton.a;
        sk.skeleton.r = flashColor.r;
        sk.skeleton.g = flashColor.g;
        sk.skeleton.b = flashColor.b;
        sk.skeleton.a = flashColor.a;
        yield return new WaitForSeconds(duration);
        if (sk != null && sk.skeleton != null)
        {
            sk.skeleton.r = origR;
            sk.skeleton.g = origG;
            sk.skeleton.b = origB;
            sk.skeleton.a = origA;
        }
    }

    // ==================== 命中特效(白色膨胀圆) ====================

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
