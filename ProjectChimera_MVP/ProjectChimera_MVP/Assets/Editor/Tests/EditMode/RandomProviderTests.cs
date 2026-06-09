using NUnit.Framework;
using ProjectChimera.Core;

namespace ProjectChimera.Tests.EditMode
{
    /// <summary>
    /// IRandomProvider 单元测试 — 验证：
    /// 1. UnityRandomProvider 包装 UnityEngine.Random
    /// 2. SeededRandomProvider 确定性：相同 seed → 相同序列
    /// 3. Range(int) 排他，Range(float) 包含两端
    /// 4. Reseed 重置回初始状态
    /// 5. RandomProvider 静态定位器 Set/Reset 行为
    /// </summary>
    [TestFixture]
    public class RandomProviderTests
    {
        [TearDown]
        public void TearDown()
        {
            RandomProvider.Reset();
        }

        // ---------- UnityRandomProvider ----------

        [Test]
        public void UnityRandomProvider_Value_IsInRange()
        {
            var rng = new UnityRandomProvider();
            for (int i = 0; i < 100; i++)
            {
                float v = rng.Value;
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f, "RandomProvider.Value 必须严格小于 1（[0, 1)）");
            }
        }

        [Test]
        public void UnityRandomProvider_Range_Float_Inclusive()
        {
            var rng = new UnityRandomProvider();
            for (int i = 0; i < 100; i++)
            {
                float v = rng.Range(0.9f, 1.1f);
                Assert.GreaterOrEqual(v, 0.9f);
                Assert.LessOrEqual(v, 1.1f, "float Range 必须包含 max（[min, max]）");
            }
        }

        [Test]
        public void UnityRandomProvider_Range_Int_Exclusive()
        {
            var rng = new UnityRandomProvider();
            for (int i = 0; i < 100; i++)
            {
                int v = rng.Range(5, 15);
                Assert.GreaterOrEqual(v, 5);
                Assert.Less(v, 15, "int Range 必须排他 max（[min, max)）");
            }
        }

        // ---------- SeededRandomProvider ----------

        [Test]
        public void SeededRandomProvider_SameSeed_SameSequence()
        {
            var a = new SeededRandomProvider(42);
            var b = new SeededRandomProvider(42);

            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(a.Value, b.Value, $"第 {i} 次 Value 必须一致");
                Assert.AreEqual(a.Range(0, 100), b.Range(0, 100), $"第 {i} 次 int Range 必须一致");
                Assert.AreEqual(a.Range(0f, 1f), b.Range(0f, 1f), $"第 {i} 次 float Range 必须一致");
            }
        }

        [Test]
        public void SeededRandomProvider_DifferentSeed_DifferentSequence()
        {
            var a = new SeededRandomProvider(42);
            var b = new SeededRandomProvider(43);
            int sameCount = 0;
            for (int i = 0; i < 20; i++)
                if (a.Range(0, 100) == b.Range(0, 100)) sameCount++;
            Assert.Less(sameCount, 5, "不同 seed 的序列必须显著不同");
        }

        [Test]
        public void SeededRandomProvider_Range_Int_Exclusive()
        {
            var rng = new SeededRandomProvider(1);
            for (int i = 0; i < 100; i++)
            {
                int v = rng.Range(5, 15);
                Assert.GreaterOrEqual(v, 5);
                Assert.Less(v, 15);
            }
        }

        [Test]
        public void SeededRandomProvider_Range_Float_Inclusive()
        {
            var rng = new SeededRandomProvider(1);
            for (int i = 0; i < 100; i++)
            {
                float v = rng.Range(0.9f, 1.1f);
                Assert.GreaterOrEqual(v, 0.9f);
                Assert.LessOrEqual(v, 1.1f);
            }
        }

        [Test]
        public void SeededRandomProvider_Reseed_RestoresInitialSequence()
        {
            var rng = new SeededRandomProvider(99);
            int[] first = new int[20];
            for (int i = 0; i < 20; i++) first[i] = rng.Range(0, 1000);

            // 消耗一些随机数
            for (int i = 0; i < 50; i++) rng.Range(0, 1000);

            // Reseed 回 99，必须能复现 first 序列
            rng.Reseed(99);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(first[i], rng.Range(0, 1000), $"Reseed 后第 {i} 项必须等于首次序列");
        }

        // ---------- RandomProvider 静态定位器 ----------

        [Test]
        public void RandomProvider_Current_DefaultNotNull()
        {
            RandomProvider.Reset();
            Assert.IsNotNull(RandomProvider.Current, "默认 Current 必须非 null");
        }

        [Test]
        public void RandomProvider_Set_ReplacesProvider()
        {
            var seeded = new SeededRandomProvider(7);
            RandomProvider.Set(seeded);
            Assert.AreSame(seeded, RandomProvider.Current);
        }

        [Test]
        public void RandomProvider_SetNull_FallsBackToUnity()
        {
            RandomProvider.Set(new SeededRandomProvider(7));
            RandomProvider.Set(null);
            Assert.IsNotNull(RandomProvider.Current);
            Assert.IsInstanceOf<UnityRandomProvider>(RandomProvider.Current, "Set(null) 必须回退到 UnityRandomProvider");
        }

        [Test]
        public void RandomProvider_Reset_RestoresUnity()
        {
            RandomProvider.Set(new SeededRandomProvider(7));
            RandomProvider.Reset();
            Assert.IsInstanceOf<UnityRandomProvider>(RandomProvider.Current);
        }

        [Test]
        public void RandomProvider_BusinessCall_ThroughCurrent_IsConsistent()
        {
            // 验证业务侧通过 RandomProvider.Current 调用与直接拿到实例等价
            var seeded = new SeededRandomProvider(2025);
            RandomProvider.Set(seeded);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(seeded.Value, RandomProvider.Current.Value);
            }
        }
    }
}
