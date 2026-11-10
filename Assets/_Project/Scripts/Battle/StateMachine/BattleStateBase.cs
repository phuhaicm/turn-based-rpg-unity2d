public abstract class BattleStateBase
{
    protected readonly BattleStateMachine stateMachine;

    public abstract string StateName { get; }

    protected BattleStateBase(BattleStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void Enter()
    {
    }

    public virtual void Execute()
    {
    }

    public virtual void Exit()
    {
    }
}

