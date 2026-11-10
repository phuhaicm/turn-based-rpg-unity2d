using System.Collections;

public class ActionExecutionState : BattleStateBase
{
    public override string StateName => "ActionExecution";

    public ActionExecutionState(BattleStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.StartCoroutine(RunActionRoutine());
    }

    private IEnumerator RunActionRoutine()
    {
        ICombatAction action = stateMachine.PendingAction;

        if (action != null)
        {
            yield return action.ExecuteAction();
        }

        stateMachine.ClearPendingAction();
        stateMachine.ChangeState(stateMachine.CheckEndConditionState);
    }
}

