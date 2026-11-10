public class TurnOrderState : BattleStateBase
{
    public override string StateName => "TurnOrder";

    public TurnOrderState(BattleStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        BattleUnit nextUnit = stateMachine.TurnQueue.DequeueNextLivingUnit();

        if (nextUnit == null)
        {
            stateMachine.ChangeState(stateMachine.CheckEndConditionState);
            return;
        }

        stateMachine.SetActiveUnit(nextUnit);
        nextUnit.OnTurnStart(); // Tắt khiên Phòng Thủ của vòng trước nếu có

        // Phân nhánh dựa trên phe của nhân vật đang tới lượt
        if (nextUnit.IsPlayerUnit)
        {
            stateMachine.ChangeState(stateMachine.PlayerSelectActionState);
        }
        else
        {
            stateMachine.ChangeState(stateMachine.EnemyTurnAIState);
        }
    }
}

