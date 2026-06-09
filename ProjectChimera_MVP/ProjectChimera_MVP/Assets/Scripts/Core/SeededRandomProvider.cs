using System;

namespace ProjectChimera.Core
{
    /// <summary>
    /// 基于种子的确定性 IRandomProvider（用于单元测试）。
    /// 给定相同 seed，序列完全可重现 — 解决 QuirkTriggerSystem/TurnManager
    /// 等随机数相关测试的概率性 flaky 问题。
    /// 内部用 System.Random（与 UnityRandom.Range 语义对齐）。
    /// </summary>
    public sealed class SeededRandomProvider : IRandomProvider
    {
        private Random _rng;
        private readonly object _lock = new object();

        public int Seed { get; private set; }

        public SeededRandomProvider(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }

        public float Value
        {
            get { lock (_lock) { return (float)_rng.NextDouble(); } }
        }

        public float Range(float min, float max)
        {
            if (max < min) (min, max) = (max, min);
            lock (_lock) { return min + (float)_rng.NextDouble() * (max - min); }
        }

        public int Range(int min, int max)
        {
            if (max < min) (min, max) = (max, min);
            lock (_lock) { return _rng.Next(min, max); }
        }

        /// <summary>重置回初始 seed，下一次调用从头开始</summary>
        public void Reseed(int seed)
        {
            lock (_lock)
            {
                Seed = seed;
                _rng = new Random(seed);
            }
        }
    }
}
