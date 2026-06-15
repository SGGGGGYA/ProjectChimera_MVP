using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战争迷雾控制器 — 基于 Shader 的迷雾系统
/// 使用 MaterialPropertyBlock 为每个地块设置迷雾因子，避免材质实例化导致的内存泄漏
/// </summary>
public class FogOfWarController : MonoBehaviour
{
    [Header("迷雾参数")]
    public float exploredFactor = 0.4f;
    public float hiddenFactor = 1.0f;
    public Color fogColor = new Color(0.1f, 0.1f, 0.15f, 1f);

    Material _fogMaterial;
    MaterialPropertyBlock _propBlock;
    Dictionary<HexCoord, SpriteRenderer[]> _tileRenderers = new Dictionary<HexCoord, SpriteRenderer[]>();

    static readonly int FogFactorID = Shader.PropertyToID("_FogFactor");
    static readonly int FogColorID = Shader.PropertyToID("_FogColor");

    void Awake()
    {
        var shader = Shader.Find("Custom/FogOfWarSprite");
        if (shader == null)
        {
            Debug.LogError("[FogOfWar] Shader 'Custom/FogOfWarSprite' not found, fog system disabled.");
            return;
        }

        _fogMaterial = new Material(shader);
        _fogMaterial.SetColor(FogColorID, fogColor);
        _propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// 注册一个地块，缓存其所有 SpriteRenderers 并替换为迷雾材质
    /// </summary>
    public void RegisterTile(HexCoord coord, HexTile tile)
    {
        if (_fogMaterial == null) return;

        var renderers = tile.GetComponentsInChildren<SpriteRenderer>();
        _tileRenderers[coord] = renderers;

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;
            sr.sharedMaterial = _fogMaterial;
            // 立即设置属性块，绑定纹理和初始雾效因子
            _propBlock.SetFloat(FogFactorID, 0f);
            _propBlock.SetTexture("_MainTex", sr.sprite.texture);
            sr.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// 更新所有地块的可见性状态
    /// </summary>
    /// <param name="squadCoord">小队当前坐标</param>
    /// <param name="radius">视野半径</param>
    /// <param name="tiles">所有地块字典</param>
    public void UpdateVisibility(HexCoord squadCoord, int radius, Dictionary<HexCoord, HexTile> tiles)
    {
        foreach (var kv in tiles)
        {
            var coord = kv.Key;
            var tile = kv.Value;
            int dist = coord.Distance(squadCoord);

            float fogFactor;

            if (dist <= radius)
            {
                tile.fogState = FogState.Visible;
                tile.isExplored = true;
                fogFactor = 0f;
            }
            else if (tile.isExplored)
            {
                tile.fogState = FogState.Explored;
                fogFactor = exploredFactor;
            }
            else
            {
                tile.fogState = FogState.Hidden;
                fogFactor = hiddenFactor;
            }

            ApplyFogFactor(coord, fogFactor);
        }
    }

    /// <summary>
    /// 禁用迷雾时，将所有地块设为完全可见
    /// </summary>
    public void SetAllVisible(Dictionary<HexCoord, HexTile> tiles)
    {
        foreach (var kv in tiles)
        {
            kv.Value.fogState = FogState.Visible;
            kv.Value.isExplored = true;
            ApplyFogFactor(kv.Key, 0f);
        }
    }

    void ApplyFogFactor(HexCoord coord, float factor)
    {
        if (!_tileRenderers.TryGetValue(coord, out var renderers)) return;

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;
            sr.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(FogFactorID, factor);
            // 关键修复：必须同时绑定 _MainTex，否则 SpriteRenderer 的内部纹理绑定
            // 会被自定义 MaterialPropertyBlock 覆盖，导致回退到 shader 默认白色纹理
            _propBlock.SetTexture("_MainTex", sr.sprite.texture);
            sr.SetPropertyBlock(_propBlock);
        }
    }

    void OnDestroy()
    {
        if (_fogMaterial != null)
            Destroy(_fogMaterial);
    }
}
