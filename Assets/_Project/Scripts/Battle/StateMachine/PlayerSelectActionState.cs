public class PlayerSelectActionState : BattleStateBase
{
    public override string StateName => "PlayerSelectAction";

    public PlayerSelectActionState(BattleStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        BattleUnit activeHero = stateMachine.ActiveUnit;
        if (activeHero == null || activeHero.IsDead)
        {
            stateMachine.ChangeState(stateMachine.TurnOrderState);
            return;
        }

        stateMachine.SetBattleStatusText($"Lượt của {activeHero.UnitName} (SPD {activeHero.Speed}) - Hãy chọn lệnh!");
        stateMachine.NotifyPlayerTurnReady(activeHero);
    }
}

