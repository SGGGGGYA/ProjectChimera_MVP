namespace ProjectChimera.Core
{
    /// <summary>
    /// IBattleContext 静态服务定位器。
    /// 与 <see cref="RandomProvider"/> 模式一致：
    ///   - 由 BattleSetup 在装配 BattleManager 时调用 <see cref="Set"/> 注入；
    ///   - 任何"无法被 BattleSetup 直接注入"的位置（预制体上的 MonoBehaviour、
    ///     编辑器工具、单元测试桩）通过 <see cref="Current"/> 拿到当前激活上下文；
    ///   - 永不返回 null，未注入时返回 <see cref="NullBattleContext"/>（安静 no-op）。
    ///
    /// 引入原因：Unity 的 FindObjectOfType&lt;T&gt; 不支持按接口查找，
    /// 而 UnitClickDetector 等挂在预制体上的组件需要在 OnEnable 时拿到上下文。
    /// 让它们直接调 FindObjectOfType&lt;BattleManager&gt;() 会把 BattleManager 类名
    /// 硬编码进所有这些组件；通过本定位器，预制体侧只依赖 IBattleContext 接口。
    /// </summary>
    public static class BattleContextProvider
    {
        private static IBattleContext _current;

        /// <summary>当前激活的 BattleContext（永不返回 null）</summary>
        public static IBattleContext Current
        {
            get
            {
                if (_current == null) _current = new NullBattleContext();
                return _current;
            }
        }

        /// <summary>由 BattleSetup 在装配 BattleManager 时调用。</summary>
        public static void Set(IBattleContext ctx)
        {
            _current = ctx ?? new NullBattleContext();
        }

        /// <summary>恢复为 NullBattleContext（用于场景卸载/重置）。</summary>
        public static void Reset()
        {
            _current = new NullBattleContext();
        }
    }
}
