namespace ProjectChimera.Core
{
    /// <summary>
    /// 随机数服务定位器。运行时通过 <see cref="Set"/> 注入；测试可在 SetUp 中注入
    /// <see cref="SeededRandomProvider"/>，在 TearDown 中 <see cref="Reset"/> 恢复默认。
    /// 与 <see cref="BattleEvents"/> 类似的"进程内单例 + 显式注入"模式，
    /// 不引入 MonoBehaviour 依赖，可被纯 C# 类（StressManager/QuirkTriggerSystem 等）直接使用。
    /// </summary>
    public static class RandomProvider
    {
        private static IRandomProvider _current = new UnityRandomProvider();

        /// <summary>当前激活的随机数提供者（永不返回 null）</summary>
        public static IRandomProvider Current
        {
            get
            {
                if (_current == null) _current = new UnityRandomProvider();
                return _current;
            }
        }

        /// <summary>注入自定义提供者（用于测试或特殊场景）</summary>
        public static void Set(IRandomProvider provider)
        {
            _current = provider ?? new UnityRandomProvider();
        }

        /// <summary>恢复为默认 UnityRandomProvider</summary>
        public static void Reset()
        {
            _current = new UnityRandomProvider();
        }
    }
}
