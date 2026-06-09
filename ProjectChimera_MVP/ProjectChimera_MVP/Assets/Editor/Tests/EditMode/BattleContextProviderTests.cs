using NUnit.Framework;
using ProjectChimera.Core;

namespace ProjectChimera.Tests.EditMode
{
    /// <summary>
    /// BattleContextProvider 单元测试 — 验证：
    /// 1. 初始 Current 永远返回非 null（NullBattleContext 兜底）
    /// 2. Set 后 Current 切换到新注入的实现
    /// 3. Set(null) 不会清空（自动回落到 NullBattleContext）
    /// 4. Reset 后回到 NullBattleContext
    /// 5. 同一 Set 多次幂等
    /// </summary>
    [TestFixture]
    public class BattleContextProviderTests
    {
        [TearDown]
        public void TearDown()
        {
            BattleContextProvider.Reset();
        }

        // ---------- 默认行为 ----------

        [Test]
        public void Current_BeforeSet_ReturnsNullContext()
        {
            // 刚初始化的 Provider 拿到的应该是 NullBattleContext
            var ctx = BattleContextProvider.Current;
            Assert.IsNotNull(ctx, "Current 永远不能为 null");
            Assert.IsInstanceOf<NullBattleContext>(ctx, "未注入时应返回 NullBattleContext 兜底");
        }

        [Test]
        public void Current_RepeatedAccess_StaysNonNull()
        {
            for (int i = 0; i < 5; i++)
            {
                var ctx = BattleContextProvider.Current;
                Assert.IsNotNull(ctx);
            }
        }

        // ---------- Set / Reset ----------

        [Test]
        public void Set_CustomContext_OverwritesCurrent()
        {
            var stub = new StubBattleContext();
            BattleContextProvider.Set(stub);

            var current = BattleContextProvider.Current;
            Assert.AreSame(stub, current);
        }

        [Test]
        public void Set_Null_FallsBackToNullContext()
        {
            BattleContextProvider.Set(new StubBattleContext());
            BattleContextProvider.Set(null);

            var current = BattleContextProvider.Current;
            Assert.IsNotNull(current);
            Assert.IsInstanceOf<NullBattleContext>(current, "Set(null) 应回落到 NullBattleContext 兜底");
        }

        [Test]
        public void Reset_RestoresNullContext()
        {
            BattleContextProvider.Set(new StubBattleContext());
            BattleContextProvider.Reset();

            var current = BattleContextProvider.Current;
            Assert.IsInstanceOf<NullBattleContext>(current);
        }

        [Test]
        public void Set_MultipleTimes_LastWins()
        {
            var a = new StubBattleContext();
            var b = new StubBattleContext();

            BattleContextProvider.Set(a);
            BattleContextProvider.Set(b);

            Assert.AreSame(b, BattleContextProvider.Current);
        }

        // ---------- 集成：NullBattleContext 行为 ----------

        [Test]
        public void NullContext_AllMethods_NoOp()
        {
            var ctx = BattleContextProvider.Current;  // 未注入，拿到 NullBattleContext
            Assert.IsFalse(ctx.IsInTargetingMode());
            Assert.IsFalse(ctx.IsValidTarget(null));
            Assert.IsNull(ctx.GetFirstAlive(null));
            Assert.IsNull(ctx.RawContext);
            // 弹窗方法安静 no-op，不抛异常
            Assert.DoesNotThrow(() => ctx.SpawnDamagePopup(Vector3.zero, 10, PopupType.Damage));
            Assert.DoesNotThrow(() => ctx.SpawnTextPopup(Vector3.zero, "test", Color.red));
        }

        // ---------- 测试桩 ----------

        /// <summary>测试用：最小化可标记的 IBattleContext 实现</summary>
        class StubBattleContext : IBattleContext
        {
            public bool IsBattleOver => false;
            public IReadOnlyList<UnitData> PlayerUnits => System.Array.Empty<UnitData>();
            public IReadOnlyList<UnitData> EnemyUnits => System.Array.Empty<UnitData>();
            public object RawContext => null;
            public bool IsInTargetingMode() => false;
            public bool IsValidTarget(UnitData unit) => false;
            public UnitData GetFirstAlive(IReadOnlyList<UnitData> units) => null;
            public void SpawnDamagePopup(Vector3 worldPos, int amount, PopupType type) { }
            public void SpawnTextPopup(Vector3 worldPos, string text, Color color, float fontSize = 4f) { }
            public void DealDamage(UnitData attacker, UnitData target, int damage, bool isCrit = false) { }
        }
    }
}
