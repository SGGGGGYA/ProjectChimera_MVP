using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// 默认 IRandomProvider：直接转发到 UnityEngine.Random。
    /// 运行时所有业务代码默认走这个实现。
    /// </summary>
    public sealed class UnityRandomProvider : IRandomProvider
    {
        public float Value => Random.value;
        public float Range(float min, float max) => Random.Range(min, max);
        public int Range(int min, int max) => Random.Range(min, max);
    }
}
