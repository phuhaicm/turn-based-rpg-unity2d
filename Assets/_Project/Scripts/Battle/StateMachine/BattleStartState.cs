using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleStartState : BattleStateBase
{
    private static readonly WaitForSeconds IntroWait = new WaitForSeconds(0.5f);

    public override string StateName => "BattleStart";

    public BattleStartState(BattleStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.StartCoroutine(SetupBattleRoutine());
    }

    private IEnumerator SetupBattleRoutine()
    {
        IReadOnlyList<BattleUnit> heroes = stateMachine.HeroUnits;
        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i] != null && heroes[i].CharacterData != null)
            {
                heroes[i].SetupUnit(heroes[i].CharacterData);
            }
        }

        IReadOnlyList<BattleUnit> enemies = stateMachine.EnemyUnits;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].CharacterData != null)
            {
                enemies[i].SetupUnit(enemies[i].CharacterData);
            }
        }

        stateMachine.TurnQueue.Initialize(heroes, enemies);
        stateMachine.SetBattleStatusText("Trận đấu bắt đầu! Đang tính toán tốc độ lượt đi...");

        yield return IntroWait;

        stateMachine.ChangeState(stateMachine.TurnOrderState);
    }
}

