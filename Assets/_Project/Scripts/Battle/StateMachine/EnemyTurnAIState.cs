using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnAIState : BattleStateBase
{
    private static readonly WaitForSeconds AIThinkWait = new WaitForSeconds(0.55f);

    public override string StateName => "EnemyTurnAI";

    public EnemyTurnAIState(BattleStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.StartCoroutine(EnemyThinkRoutine());
    }

    private IEnumerator EnemyThinkRoutine()
    {
        BattleUnit activeEnemy = stateMachine.ActiveUnit;
        if (activeEnemy == null || activeEnemy.IsDead)
        {
            stateMachine.ChangeState(stateMachine.TurnOrderState);
            yield break;
        }

        stateMachine.SetBattleStatusText($"Lượt của {activeEnemy.UnitName} (SPD {activeEnemy.Speed}) - AI đang tính toán nước đi...");
        yield return AIThinkWait;

        BattleUnit targetHero = FindWeakestLivingUnit(stateMachine.HeroUnits);
        if (targetHero == null)
        {
            stateMachine.ChangeState(stateMachine.CheckEndConditionState);
            yield break;
        }

        // Ưu tiên kiểm tra xem AI có kỹ năng phù hợp và đủ Mana để tung chiêu không
        ICombatAction chosenSkillAction = TryChooseSkillAction(activeEnemy, targetHero);
        if (chosenSkillAction != null)
        {
            stateMachine.SubmitAction(chosenSkillAction);
            yield break;
        }

        // Nếu hết Mana hoặc không dùng kỹ năng thì đánh thường vào Hero yếu máu nhất
        stateMachine.SetBattleStatusText($"{activeEnemy.UnitName} đánh thường vào {targetHero.UnitName}!");
        ICombatAction basicAttack = stateMachine.CreateOrReuseBasicAttack(activeEnemy, targetHero);
        stateMachine.SubmitAction(basicAttack);
    }

    private ICombatAction TryChooseSkillAction(BattleUnit enemy, BattleUnit weakestHero)
    {
        if (enemy.CharacterData == null || enemy.Mana == null)
        {
            return null;
        }

        IReadOnlyList<SkillDataSO> skills = enemy.CharacterData.Skills;
        if (skills == null || skills.Count == 0)
        {
            return null;
        }

        // Tỉ lệ 65% ưu tiên tung kỹ năng khi còn Mana để AI vừa nguy hiểm vừa có tính ngẫu nhiên
        if (Random.value > 0.65f)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDataSO skill = skills[i];
            if (skill == null || !enemy.Mana.HasEnoughMana(skill.ManaCost))
            {
                continue;
            }

            if (skill.Category == SkillCategory.Heal)
            {
                BattleUnit hurtAlly = FindWeakestLivingUnit(stateMachine.EnemyUnits);
                if (hurtAlly != null && hurtAlly.Health.HealthNormalized < 0.6f)
                {
                    stateMachine.SetBattleStatusText($"{enemy.UnitName} dùng [{skill.SkillName}] hồi máu cho {hurtAlly.UnitName}!");
                    return stateMachine.CreateOrReuseSingleSkill(enemy, skill, hurtAlly);
                }
                continue;
            }

            if (skill.TargetType == SkillTargetType.AllEnemies)
            {
                stateMachine.SetBattleStatusText($"{enemy.UnitName} tung tuyệt kỹ AOE [{skill.SkillName}] lên toàn đội Hero!");
                return stateMachine.CreateOrReuseMultiSkill(enemy, skill, stateMachine.HeroUnits);
            }

            stateMachine.SetBattleStatusText($"{enemy.UnitName} dùng kỹ năng [{skill.SkillName}] (-{skill.ManaCost} MP) lên {weakestHero.UnitName}!");
            return stateMachine.CreateOrReuseSingleSkill(enemy, skill, weakestHero);
        }

        return null;
    }

    private BattleUnit FindWeakestLivingUnit(IReadOnlyList<BattleUnit> units)
    {
        BattleUnit bestTarget = null;
        int lowestHP = int.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || unit.IsDead || unit.Health == null)
            {
                continue;
            }

            if (unit.Health.CurrentHP < lowestHP)
            {
                lowestHP = unit.Health.CurrentHP;
                bestTarget = unit;
            }
        }

        return bestTarget;
    }
}
