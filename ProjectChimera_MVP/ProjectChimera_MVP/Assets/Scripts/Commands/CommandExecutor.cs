using System.Collections.Generic;
using UnityEngine;

public static class CommandExecutor
{
    public static void Execute(UnitData attacker, SkillData skill, UnitData target,
                                BattleManager battleManager, List<UnitData> allPlayers, List<UnitData> allEnemies)
    {
        if (attacker == null || skill == null || skill.commands == null || skill.commands.Count == 0)
            return;

        var ctx = new CommandContext
        {
            attacker = attacker,
            selectedTarget = target,
            battleManager = battleManager,
            allPlayers = allPlayers,
            allEnemies = allEnemies
        };

        BattleLog.Add($"[技能] {attacker.unitName} 释放 [{skill.skillName}]");

        foreach (var command in skill.commands)
        {
            if (command == null) continue;

            // 如果目标是死了的单体单位，跳过后续命令
            if (ctx.selectedTarget != null && ctx.selectedTarget.currentHP <= 0 && !(command is ApplyStatusCommand && ((ApplyStatusCommand)command).applyToAllAllies))
            {
                // 允许 AoE 和全体效果继续
                bool isGlobalEffect = (command is ApplyStatusCommand ac && (ac.applyToAllAllies || ac.applyToAllEnemies))
                                   || (command is RemoveStatusCommand rc && (rc.removeFromAllAllies || rc.removeFromAllEnemies))
                                   || (command is DealDamageCommand dc && dc.isAoE);
                if (!isGlobalEffect)
                {
                    BattleLog.Add($"[命令] 目标已死亡，跳过 [{command.GetType().Name}]");
                    continue;
                }
            }

            CommandResult result = command.Execute(ctx);

            if (result.skipRemainingCommands)
                break;
        }
    }
}
