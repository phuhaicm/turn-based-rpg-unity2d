using System.Collections.Generic;

public enum BattleOutcome
{
    None = 0,
    Victory = 1,
    Defeat = 2
}

public class CheckEndConditionState : BattleStateBase
{
    public override string StateName => "CheckEndCondition";

    public CheckEndConditionState(BattleStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        bool anyEnemyAlive = HasAnyLivingUnit(stateMachine.EnemyUnits);
        if (!anyEnemyAlive)
        {
            stateMachine.FinishBattle(BattleOutcome.Victory);
            return;
        }

        bool anyHeroAlive = HasAnyLivingUnit(stateMachine.HeroUnits);
        if (!anyHeroAlive)
        {
            stateMachine.FinishBattle(BattleOutcome.Defeat);
            return;
        }

        // Nếu cả 2 phe đều còn người sống thì chuyển sang lượt kế tiếp
        stateMachine.ChangeState(stateMachine.TurnOrderState);
    }

    private bool HasAnyLivingUnit(IReadOnlyList<BattleUnit> units)
    {
        if (units == null)
        {
            return false;
        }

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && !units[i].IsDead)
            {
                return true;
            }
        }

        return false;
    }
}

