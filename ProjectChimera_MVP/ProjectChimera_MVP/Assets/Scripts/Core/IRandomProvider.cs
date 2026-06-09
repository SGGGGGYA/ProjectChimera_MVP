using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// 随机数提供者接口。抽象所有 <c>UnityEngine.Random</c> 调用，
    /// 让战斗/压力/AI 等随机数相关逻辑可注入确定性种子用于测试。
    /// 语义与 UnityEngine.Random 完全一致（int 区间为 [min, max) 排他，float 区间为 [min, max] 包含），
    /// 重构时调用点行为零变化。
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>[0, 1) 区间的随机浮点数 — 等价 UnityEngine.Random.value</summary>
        float Value { get; }

        /// <summary>[min, max] 区间的随机浮点数（包含两端）— 等价 UnityEngine.Random.Range(float, float)</summary>
        float Range(float min, float max);

        /// <summary>[min, max) 区间的随机整数（max 排他）— 等价 UnityEngine.Random.Range(int, int)</summary>
        int Range(int min, int max);
    }
}
