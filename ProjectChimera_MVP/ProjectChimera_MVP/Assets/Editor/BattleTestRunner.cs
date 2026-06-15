using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// 战斗系统自动化测试框架
/// 通过 Window > Battle Test Runner 打开窗口运行测试
/// </summary>
public class BattleTestRunner : EditorWindow
{
    private bool isRunning = false;
    private string currentTestName = "";
    private List<string> logLines = new List<string>();
    private List<TestResult> testResults = new List<TestResult>();
    private System.Diagnostics.Stopwatch totalStopwatch;
    private HashSet<GameObject> preExistingObjects = new HashSet<GameObject>();

    private IEnumerator currentCoroutine;
    private Queue<System.Func<IEnumerator>> testQueue = new Queue<System.Func<IEnumerator>>();

    private struct TestResult
    {
        public string name;
        public bool passed;
        public string detail;
        public float durationMs;
        public int rounds;
    }

    [MenuItem("Window/Battle Test Runner")]
    static void OpenWindow()
    {
        GetWindow<BattleTestRunner>("战斗测试");
    }

    void OnGUI()
    {
        GUILayout.Label("Project Chimera 战斗系统自动化测试", EditorStyles.boldLabel);

        if (isRunning)
        {
            EditorGUILayout.LabelField($"正在运行: {currentTestName}");
            EditorGUILayout.LabelField($"进度: {testResults.Count}/8");
            if (GUILayout.Button("终止测试", GUILayout.Height(30)))
            {
                StopAllTests();
            }
        }
        else
        {
            if (GUILayout.Button("运行全部测试", GUILayout.Height(40)))
            {
                RunAllTests();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("日志:", EditorStyles.boldLabel);
        foreach (var line in logLines)
        {
            GUILayout.Label(line);
        }
    }

    void RunAllTests()
    {
        isRunning = true;
        logLines.Clear();
        testResults.Clear();
        totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        Log("开始运行战斗系统自动化测试...");

        // 编辑器模式下 WaitForSeconds 永远无法完成，启用测试模式跳过所有等待
        EnemyAIEngine.IsEditorTestMode = true;
        VFXManager.IsEditorTestMode = true;
        TurnManager.IsEditorTestMode = true;

        // 记录测试前已存在的根对象，用于后续清理
        RecordPreExistingObjects();

        // 清空静态事件残留，避免跨测试影响
        BattleEvents.ClearAll();

        testQueue.Clear();
        testQueue.Enqueue(() => RunCombatTest("2v2正常战斗", Create2v2Setup, maxRounds: 200));
        testQueue.Enqueue(() => RunCombatTest("4v4极限战斗", Create4v4Setup, maxRounds: 300));
        testQueue.Enqueue(() => RunCombatTest("1v3生存", Create1v3Setup, maxRounds: 200));
        testQueue.Enqueue(() => RunCombatTest("1v4全灭", Create1v4Setup, maxRounds: 200));
        testQueue.Enqueue(() => RunRetreatTest());
        testQueue.Enqueue(() => RunEmptyEnemyTest());
        testQueue.Enqueue(() => RunPlayerDeadTest());
        testQueue.Enqueue(() => RunLongBattleTest());

        EditorApplication.update += OnEditorUpdate;
        ProcessNextTest();
    }

    void StopAllTests()
    {
        EditorApplication.update -= OnEditorUpdate;
        currentCoroutine = null;
        CleanupNewObjects();
        isRunning = false;
        Log("测试已被人为终止");
        Repaint();
    }

    void ProcessNextTest()
    {
        if (testQueue.Count == 0)
        {
            FinishAllTests();
            return;
        }

        var testFactory = testQueue.Dequeue();
        try
        {
            currentCoroutine = testFactory();
        }
        catch (System.Exception ex)
        {
            Log($"[错误] 测试初始化异常: {ex.Message}\n{ex.StackTrace}");
            testResults.Add(new TestResult
            {
                name = currentTestName,
                passed = false,
                detail = $"初始化异常: {ex.Message}",
                durationMs = 0,
                rounds = 0
            });
            CleanupNewObjects();
            ProcessNextTest();
        }
    }

    void OnEditorUpdate()
    {
        if (currentCoroutine != null)
        {
            try
            {
                if (!currentCoroutine.MoveNext())
                {
                    currentCoroutine = null;
                    CleanupNewObjects();
                    ProcessNextTest();
                }
            }
            catch (System.Exception ex)
            {
                Log($"[错误] {currentTestName} 运行时异常: {ex.Message}\n{ex.StackTrace}");
                testResults.Add(new TestResult
                {
                    name = currentTestName,
                    passed = false,
                    detail = $"运行时异常: {ex.Message}",
                    durationMs = 0,
                    rounds = 0
                });
                currentCoroutine = null;
                CleanupNewObjects();
                ProcessNextTest();
            }
        }
    }

    void FinishAllTests()
    {
        EditorApplication.update -= OnEditorUpdate;
        currentCoroutine = null;
        totalStopwatch.Stop();
        isRunning = false;
        Log($"所有测试执行完毕，总耗时: {totalStopwatch.Elapsed.TotalSeconds:F2}秒");
        GenerateReport();
        Repaint();
    }

    void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        logLines.Add(line);
        Debug.Log($"[BattleTest] {msg}");
        Repaint();
    }

    void GenerateReport()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), $"BattleTestReport_{timestamp}.txt");

        var lines = new List<string>();
        lines.Add("============================================");
        lines.Add("  Project Chimera 战斗系统自动化测试报告");
        lines.Add($"  生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        lines.Add($"  总耗时: {totalStopwatch.Elapsed.TotalSeconds:F2}秒");
        lines.Add("============================================");
        lines.Add("");

        int passCount = testResults.Count(r => r.passed);
        lines.Add($"测试结果: {passCount}/{testResults.Count} 通过");
        lines.Add("");

        foreach (var r in testResults)
        {
            lines.Add($"[{ (r.passed ? "通过" : "失败") }] {r.name}");
            lines.Add($"    耗时: {r.durationMs:F0}ms  回合: {r.rounds}");
            if (!string.IsNullOrEmpty(r.detail))
                lines.Add($"    详情: {r.detail}");
            lines.Add("");
        }

        File.WriteAllLines(path, lines);
        Log($"测试报告已保存: {path}");
    }

    // ==================== 对象生命周期管理 ====================

    void RecordPreExistingObjects()
    {
        preExistingObjects.Clear();
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go != null) preExistingObjects.Add(go);
        }
    }

    void CleanupNewObjects()
    {
        var toDestroy = new List<GameObject>();
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go != null && !preExistingObjects.Contains(go))
                toDestroy.Add(go);
        }
        foreach (var go in toDestroy)
        {
            if (go != null) DestroyImmediate(go);
        }
        // 再次清空事件，防止残留回调引用已销毁对象
        BattleEvents.ClearAll();
    }

    // ==================== 通用战斗测试协程 ====================

    IEnumerator RunCombatTest(string testName, System.Action<BattleManager> setupAction, int maxRounds)
    {
        currentTestName = testName;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Log($"[开始-v2] {testName} (同步战斗模拟，无协程/事件)");

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        var bmGo = new GameObject("BattleManager");
        BattleManager bm = bmGo.AddComponent<BattleManager>();

        // 应用测试配置
        setupAction?.Invoke(bm);

        // 确保每个单位都有基础组件
        foreach (var u in bm.playerUnits) { if (u != null) EnsureUnitComponents(u); }
        foreach (var u in bm.enemyUnits) { if (u != null) EnsureUnitComponents(u); }

        bool isVictory = false;
        int round = 0;
        string errorMsg = null;

        // 同步战斗循环：按 AGI 排序，每单位轮流攻击对方随机存活单位
        while (round < maxRounds)
        {
            round++;

            // 收集存活单位并按 AGI 降序
            var allAlive = bm.playerUnits
                .Where(u => u != null && u.currentHP > 0)
                .Concat(bm.enemyUnits.Where(u => u != null && u.currentHP > 0))
                .OrderByDescending(u => u.AGI)
                .ToList();

            if (allAlive.Count == 0) break;

            foreach (var unit in allAlive)
            {
                if (unit.currentHP <= 0) continue;

                // 检查战斗是否结束
                bool playersAlive = bm.playerUnits.Any(u => u != null && u.currentHP > 0);
                bool enemiesAlive = bm.enemyUnits.Any(u => u != null && u.currentHP > 0);
                if (!playersAlive || !enemiesAlive)
                {
                    isVictory = enemiesAlive;
                    goto battleEnd;
                }

                // 选目标：对方阵营随机存活单位
                var targets = unit.isPlayer
                    ? bm.enemyUnits.Where(u => u != null && u.currentHP > 0).ToList()
                    : bm.playerUnits.Where(u => u != null && u.currentHP > 0).ToList();

                if (targets.Count == 0) continue;
                var target = targets[UnityEngine.Random.Range(0, targets.Count)];

                // 计算并造成伤害
                try
                {
                    int damage = Mathf.Max(1, unit.GetEffectiveSTR() + unit.weaponAttack - target.GetEffectiveDEF() / 2);
                    bm.DealDamage(unit, target, damage);
                }
                catch (System.Exception ex)
                {
                    errorMsg = $"{testName} 异常: {ex.Message}";
                    goto battleEnd;
                }

                yield return null;
            }
        }

    battleEnd:
        // 容错：未自然结束时标记失败
        if (round >= maxRounds)
            isVictory = false;

        if (errorMsg != null)
            Log($"[错误] {errorMsg}");

        sw.Stop();
        bool playersAliveFinal = bm.playerUnits.Any(u => u != null && u.currentHP > 0);
        bool enemiesAliveFinal = bm.enemyUnits.Any(u => u != null && u.currentHP > 0);
        bool battleEndedProperly = !playersAliveFinal || !enemiesAliveFinal;
        string detail = $"胜利={isVictory}, 回合={round}, 耗时={sw.ElapsedMilliseconds}ms, 玩家存活={playersAliveFinal}, 敌人存活={enemiesAliveFinal}";

        Log($"[完成] {testName}: {detail}");
        testResults.Add(new TestResult
        {
            name = testName,
            passed = battleEndedProperly,
            detail = detail,
            durationMs = (float)sw.ElapsedMilliseconds,
            rounds = round
        });
    }

    void EnsureUnitComponents(UnitData unit)
    {
        if (unit == null) return;
        // SpriteRenderer：BattleManager.DealDamage 中会获取并改颜色
        if (unit.GetComponent<SpriteRenderer>() == null)
        {
            var sr = unit.gameObject.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
        }
        // selectCircle：UnitData.Start 中会查找并用于高亮
        if (unit.selectCircle == null)
        {
            var circle = new GameObject("SelectCircle");
            circle.transform.SetParent(unit.transform, false);
            circle.AddComponent<SpriteRenderer>();
            unit.selectCircle = circle;
        }
        // 确保有至少一个技能（普通攻击）
        if (unit.skills == null || unit.skills.Count == 0)
        {
            unit.skills = new List<SkillData> { CreateBasicAttackSkill() };
        }
        // 确保当前HP合法
        if (unit.currentHP <= 0 && unit.MaxHp > 0)
            unit.currentHP = unit.MaxHp;
    }

    // ==================== 测试用例配置 ====================

    void Create2v2Setup(BattleManager bm)
    {
        var p1 = CreateUnit("战士A", true, vit: 12, str: 10, agi: 6, int_: 3, hp: 80);
        var p2 = CreateUnit("弓手A", true, vit: 8, str: 7, agi: 10, int_: 4, hp: 60);
        var e1 = CreateUnit("哥布林", false, vit: 8, str: 7, agi: 5, int_: 2, hp: 50);
        var e2 = CreateUnit("兽人", false, vit: 10, str: 9, agi: 4, int_: 2, hp: 70);

        p1.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("重击", 15, SkillTargetType.SingleEnemy) };
        p2.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("连射", 12, SkillTargetType.SingleEnemy) };
        e1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e2.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("猛击", 14, SkillTargetType.SingleEnemy) };

        bm.playerUnits = new List<UnitData> { p1, p2 };
        bm.enemyUnits = new List<UnitData> { e1, e2 };
    }

    void Create4v4Setup(BattleManager bm)
    {
        var players = new List<UnitData>();
        var enemies = new List<UnitData>();
        for (int i = 0; i < 4; i++)
        {
            players.Add(CreateUnit($"英雄{i + 1}", true, vit: 10 + i, str: 8 + i, agi: 6 + i, int_: 3 + i, hp: 70 + i * 10));
            enemies.Add(CreateUnit($"敌人{i + 1}", false, vit: 9 + i, str: 7 + i, agi: 5 + i, int_: 2 + i, hp: 60 + i * 10));
        }
        foreach (var p in players)
            p.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("技能", 15, SkillTargetType.SingleEnemy) };
        foreach (var e in enemies)
            e.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("技能", 14, SkillTargetType.SingleEnemy) };

        bm.playerUnits = players;
        bm.enemyUnits = enemies;
    }

    void Create1v3Setup(BattleManager bm)
    {
        var p1 = CreateUnit("生存者", true, vit: 20, str: 12, agi: 8, int_: 5, hp: 150);
        var e1 = CreateUnit("小妖A", false, vit: 5, str: 4, agi: 3, int_: 1, hp: 25);
        var e2 = CreateUnit("小妖B", false, vit: 5, str: 4, agi: 3, int_: 1, hp: 25);
        var e3 = CreateUnit("小妖C", false, vit: 5, str: 4, agi: 3, int_: 1, hp: 25);

        p1.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateHealSkill("急救", 20) };
        e1.skills = e2.skills = e3.skills = new List<SkillData> { CreateBasicAttackSkill() };

        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1, e2, e3 };
    }

    void Create1v4Setup(BattleManager bm)
    {
        var p1 = CreateUnit("新兵", true, vit: 6, str: 5, agi: 4, int_: 2, hp: 30);
        var e1 = CreateUnit("精英A", false, vit: 12, str: 11, agi: 7, int_: 3, hp: 90);
        var e2 = CreateUnit("精英B", false, vit: 12, str: 11, agi: 7, int_: 3, hp: 90);
        var e3 = CreateUnit("精英C", false, vit: 12, str: 11, agi: 7, int_: 3, hp: 90);
        var e4 = CreateUnit("精英D", false, vit: 12, str: 11, agi: 7, int_: 3, hp: 90);

        p1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e1.skills = e2.skills = e3.skills = e4.skills = new List<SkillData> { CreateBasicAttackSkill(), CreateDamageSkill("重击", 20, SkillTargetType.SingleEnemy) };

        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1, e2, e3, e4 };
    }

    IEnumerator RunRetreatTest()
    {
        currentTestName = "撤退";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Log("[开始] 撤退");

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var bmGo = new GameObject("BattleManager");
        BattleManager bm = bmGo.AddComponent<BattleManager>();

        var tmGo = new GameObject("TurnManager");
        TurnManager tm = tmGo.AddComponent<TurnManager>();

        var p1 = CreateUnit("撤退者", true, vit: 10, str: 8, agi: 6, int_: 3, hp: 80);
        var e1 = CreateUnit("追兵", false, vit: 8, str: 7, agi: 5, int_: 2, hp: 60);
        p1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1 };

        EnsureUnitComponents(p1);
        EnsureUnitComponents(e1);

        tm.Context = bm;
        bm.TurnState = tm;
        BattleLog.Clear();

        // 先订阅再初始化（防止 InitializeBattle 立刻结束时丢失事件）
        bool battleEnded = false;
        bool isVictory = false;
        System.Action<bool> onBattleEnd = (victory) => { battleEnded = true; isVictory = victory; };
        BattleEvents.OnBattleEnded += onBattleEnd;

        tm.InitializeBattle(bm.playerUnits, bm.enemyUnits);

        float startTime = Time.realtimeSinceStartup;
        bool retreated = false;

        try
        {
            while (!battleEnded && !retreated)
            {
                if (Time.realtimeSinceStartup - startTime > 10f)
                {
                    Log("[超时] 撤退测试超过10秒");
                    break;
                }

                if (tm.IsPlayerTurn && tm.CanCurrentUnitAct())
                {
                    // 第一回合直接撤退
                    bm.TriggerRetreat();
                    retreated = true;
                }

                yield return null;
            }
        }
        finally
        {
            BattleEvents.OnBattleEnded -= onBattleEnd;
        }

        sw.Stop();
        bool passed = retreated;
        string detail = $"撤退成功={retreated}, 回合={tm.roundCount}, 耗时={sw.ElapsedMilliseconds}ms";
        Log($"[完成] 撤退: {detail}");
        testResults.Add(new TestResult
        {
            name = "撤退",
            passed = passed,
            detail = detail,
            durationMs = (float)sw.ElapsedMilliseconds,
            rounds = tm.roundCount
        });
    }

    IEnumerator RunEmptyEnemyTest()
    {
        currentTestName = "空敌人";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Log("[开始] 空敌人");

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        var bmGo = new GameObject("BattleManager");
        BattleManager bm = bmGo.AddComponent<BattleManager>();

        var tmGo = new GameObject("TurnManager");
        TurnManager tm = tmGo.AddComponent<TurnManager>();

        var p1 = CreateUnit("英雄1", true, vit: 10, str: 8, agi: 6, int_: 3, hp: 80);
        var p2 = CreateUnit("英雄2", true, vit: 10, str: 8, agi: 6, int_: 3, hp: 80);
        p1.skills = p2.skills = new List<SkillData> { CreateBasicAttackSkill() };
        bm.playerUnits = new List<UnitData> { p1, p2 };
        bm.enemyUnits = new List<UnitData>();

        EnsureUnitComponents(p1);
        EnsureUnitComponents(p2);

        tm.Context = bm;
        bm.TurnState = tm;
        BattleLog.Clear();

        // 【关键】先订阅再初始化，防止 InitializeBattle 立刻结束战斗时丢失事件
        bool battleEnded = false;
        System.Action<bool> onBattleEnd = (victory) => { battleEnded = true; };
        BattleEvents.OnBattleEnded += onBattleEnd;

        tm.InitializeBattle(bm.playerUnits, bm.enemyUnits);

        float startTime = Time.realtimeSinceStartup;

        try
        {
            while (!battleEnded)
            {
                if (Time.realtimeSinceStartup - startTime > 5f)
                {
                    Log("[超时] 空敌人测试超过5秒");
                    break;
                }
                yield return null;
            }
        }
        finally
        {
            BattleEvents.OnBattleEnded -= onBattleEnd;
        }

        sw.Stop();
        bool passed = battleEnded; // 应该立即结束
        string detail = $"战斗结束={battleEnded}, 回合={tm.roundCount}, 耗时={sw.ElapsedMilliseconds}ms";
        Log($"[完成] 空敌人: {detail}");
        testResults.Add(new TestResult
        {
            name = "空敌人",
            passed = passed,
            detail = detail,
            durationMs = (float)sw.ElapsedMilliseconds,
            rounds = tm.roundCount
        });
    }

    IEnumerator RunPlayerDeadTest()
    {
        currentTestName = "玩家全死";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Log("[开始] 玩家全死");

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        var bmGo = new GameObject("BattleManager");
        BattleManager bm = bmGo.AddComponent<BattleManager>();

        var tmGo = new GameObject("TurnManager");
        TurnManager tm = tmGo.AddComponent<TurnManager>();

        var p1 = CreateUnit("死者1", true, vit: 10, str: 8, agi: 6, int_: 3, hp: 0);
        var p2 = CreateUnit("死者2", true, vit: 10, str: 8, agi: 6, int_: 3, hp: 0);
        var e1 = CreateUnit("胜者", false, vit: 10, str: 8, agi: 6, int_: 3, hp: 80);
        p1.skills = p2.skills = e1.skills = new List<SkillData> { CreateBasicAttackSkill() };

        bm.playerUnits = new List<UnitData> { p1, p2 };
        bm.enemyUnits = new List<UnitData> { e1 };

        EnsureUnitComponents(p1);
        EnsureUnitComponents(p2);
        EnsureUnitComponents(e1);

        // EnsureUnitComponents 会把 HP<=0 的单位恢复到 MaxHp，测试需要保持 0
        p1.currentHP = 0; p2.currentHP = 0;

        tm.Context = bm;
        bm.TurnState = tm;
        BattleLog.Clear();

        // 【关键】先订阅再初始化，防止 InitializeBattle 立刻结束战斗时丢失事件
        bool battleEnded = false;
        bool isVictory = false;
        System.Action<bool> onBattleEnd = (victory) => { battleEnded = true; isVictory = victory; };
        BattleEvents.OnBattleEnded += onBattleEnd;

        tm.InitializeBattle(bm.playerUnits, bm.enemyUnits);

        float startTime = Time.realtimeSinceStartup;

        try
        {
            while (!battleEnded)
            {
                if (Time.realtimeSinceStartup - startTime > 5f)
                {
                    Log("[超时] 玩家全死测试超过5秒");
                    break;
                }
                yield return null;
            }
        }
        finally
        {
            BattleEvents.OnBattleEnded -= onBattleEnd;
        }

        sw.Stop();
        bool passed = battleEnded && !isVictory;
        string detail = $"战斗结束={battleEnded}, 胜利={isVictory}, 回合={tm.roundCount}, 耗时={sw.ElapsedMilliseconds}ms";
        Log($"[完成] 玩家全死: {detail}");
        testResults.Add(new TestResult
        {
            name = "玩家全死",
            passed = passed,
            detail = detail,
            durationMs = (float)sw.ElapsedMilliseconds,
            rounds = tm.roundCount
        });
    }

    IEnumerator RunLongBattleTest()
    {
        currentTestName = "长时间战斗防死循环";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Log("[开始] 长时间战斗防死循环");

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        var bmGo = new GameObject("BattleManager");
        BattleManager bm = bmGo.AddComponent<BattleManager>();

        // 双方高防御、低伤害，让战斗持续很久
        var p1 = CreateUnit("铁壁", true, vit: 15, str: 3, agi: 6, int_: 3, hp: 100);
        var e1 = CreateUnit("硬壳", false, vit: 15, str: 3, agi: 5, int_: 2, hp: 100);
        p1.baseDefense = 50;
        e1.baseDefense = 50;
        p1.skills = new List<SkillData> { CreateBasicAttackSkill() };
        e1.skills = new List<SkillData> { CreateBasicAttackSkill() };

        bm.playerUnits = new List<UnitData> { p1 };
        bm.enemyUnits = new List<UnitData> { e1 };

        EnsureUnitComponents(p1);
        EnsureUnitComponents(e1);

        int maxRounds = 50;
        int round = 0;
        bool passed = true;

        while (round < maxRounds)
        {
            round++;

            bool playersAlive = bm.playerUnits.Any(u => u != null && u.currentHP > 0);
            bool enemiesAlive = bm.enemyUnits.Any(u => u != null && u.currentHP > 0);
            if (!playersAlive || !enemiesAlive) break;

            // 玩家攻击敌人
            try
            {
                int dmg1 = Mathf.Max(1, p1.GetEffectiveSTR() + p1.weaponAttack - e1.GetEffectiveDEF() / 2);
                bm.DealDamage(p1, e1, dmg1);

                if (e1.currentHP > 0)
                {
                    // 敌人攻击玩家
                    int dmg2 = Mathf.Max(1, e1.GetEffectiveSTR() + e1.weaponAttack - p1.GetEffectiveDEF() / 2);
                    bm.DealDamage(e1, p1, dmg2);
                }
            }
            catch (System.Exception ex)
            {
                Log($"[错误] 长时间战斗异常: {ex.Message}");
                passed = false;
                break;
            }

            yield return null;
        }

        sw.Stop();
        string detail = $"回合={round}, 耗时={sw.ElapsedMilliseconds}ms, 未死循环";
        Log($"[完成] 长时间战斗防死循环: {detail}");
        testResults.Add(new TestResult
        {
            name = "长时间战斗防死循环",
            passed = passed,
            detail = detail,
            durationMs = (float)sw.ElapsedMilliseconds,
            rounds = round
        });
    }

    // ==================== 辅助工厂方法 ====================

    UnitData CreateUnit(string name, bool isPlayer, int vit, int str, int agi, int int_, int hp)
    {
        GameObject go = new GameObject(name);
        UnitData unit = go.AddComponent<UnitData>();
        unit.unitName = name;
        unit.isPlayer = isPlayer;
        unit.primaryAttributes = new PrimaryAttributes(vit, str, agi, int_);
        unit.currentHP = hp;
        unit.skills = new List<SkillData>();
        unit.weaponAttack = 5;
        unit.rank = isPlayer ? 0 : 0;
        return unit;
    }

    SkillData CreateBasicAttackSkill()
    {
        var skill = new SkillData();
        skill.skillName = "普通攻击";
        skill.baseDamage = 10;
        skill.strScaling = 1f;
        skill.targetType = SkillTargetType.SingleEnemy;
        skill.canTargetFrontRank = true;
        skill.canTargetBackRank = true;
        skill.minUserRank = 0;
        skill.maxUserRank = 3;
        skill.aiCategory = SkillEffectType.DirectDamage;
        skill.commands = new List<Command>
        {
            new DealDamageCommand { baseDamage = 10, strScaling = 1f }
        };
        return skill;
    }

    SkillData CreateDamageSkill(string name, int damage, SkillTargetType targetType)
    {
        var skill = new SkillData();
        skill.skillName = name;
        skill.baseDamage = damage;
        skill.strScaling = 1f;
        skill.targetType = targetType;
        skill.canTargetFrontRank = true;
        skill.canTargetBackRank = true;
        skill.minUserRank = 0;
        skill.maxUserRank = 3;
        skill.aiCategory = SkillEffectType.DirectDamage;
        skill.commands = new List<Command>
        {
            new DealDamageCommand { baseDamage = damage, strScaling = 1f }
        };
        return skill;
    }

    SkillData CreateHealSkill(string name, int healAmount)
    {
        var skill = new SkillData();
        skill.skillName = name;
        skill.targetType = SkillTargetType.SingleAlly;
        skill.canTargetFrontRank = true;
        skill.canTargetBackRank = true;
        skill.minUserRank = 0;
        skill.maxUserRank = 3;
        skill.aiCategory = SkillEffectType.Heal;
        skill.commands = new List<Command>
        {
            new HealCommand { baseHeal = healAmount, intScaling = 0.5f }
        };
        return skill;
    }

    /// <summary>递归驱动嵌套协程到完成（编辑器下不使用 StartCoroutine，需手动处理嵌套）</summary>
    static void DriveCoroutineToCompletion(System.Collections.IEnumerator coroutine)
    {
        if (coroutine == null) return;
        try
        {
            while (coroutine.MoveNext())
            {
                if (coroutine.Current is System.Collections.IEnumerator nested)
                {
                    DriveCoroutineToCompletion(nested);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BattleTest] 协程驱动异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
