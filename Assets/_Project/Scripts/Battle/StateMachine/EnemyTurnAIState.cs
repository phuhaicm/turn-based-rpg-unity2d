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

        stateMachine.SetBattleStatusText($"Lượt của {activeEnemy.UnitName} (SPD {activeEnemy.Speed}) - Quái đang chọn mục tiêu...");
        yield return AIThinkWait;

        BattleUnit targetHero = FindWeakestLivingHero(stateMachine.HeroUnits);
        if (targetHero == null)
        {
            stateMachine.ChangeState(stateMachine.CheckEndConditionState);
            yield break;
        }

        stateMachine.SetBattleStatusText($"{activeEnemy.UnitName} tấn công {targetHero.UnitName}!");
        ICombatAction aiAction = stateMachine.CreateOrReuseBasicAttack(activeEnemy, targetHero);
        stateMachine.SubmitAction(aiAction);
    }

    private BattleUnit FindWeakestLivingHero(IReadOnlyList<BattleUnit> heroes)
    {
        BattleUnit bestTarget = null;
        int lowestHP = int.MaxValue;

        for (int i = 0; i < heroes.Count; i++)
        {
            BattleUnit hero = heroes[i];
            if (hero == null || hero.IsDead || hero.Health == null)
            {
                continue;
            }

            if (hero.Health.CurrentHP < lowestHP)
            {
                lowestHP = hero.Health.CurrentHP;
                bestTarget = hero;
            }
        }

        return bestTarget;
    }
}

