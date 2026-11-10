using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleStateMachine : HaiMonoBehaviour
{
    [Header("Party Units")]
    [SerializeField]
    private List<BattleUnit> heroUnits = new List<BattleUnit>();

    [SerializeField]
    private List<BattleUnit> enemyUnits = new List<BattleUnit>();

    [Header("Debug & Runtime State")]
    [SerializeField]
    private bool showDebugHUD = true;

    [SerializeField]
    private string currentStateName;

    [SerializeField]
    private BattleUnit activeUnit;

    [SerializeField]
    private BattleOutcome battleOutcome = BattleOutcome.None;

    private BattleStateBase currentState;
    private ICombatAction pendingAction;
    private string battleStatusText = "";

    private readonly TurnOrderQueue turnQueue = new TurnOrderQueue();

    // Khởi tạo sẵn toàn bộ các State và Command 1 lần duy nhất để tái sử dụng (Zero-GC)
    private BattleStartState battleStartState;
    private TurnOrderState turnOrderState;
    private PlayerSelectActionState playerSelectActionState;
    private EnemyTurnAIState enemyTurnAIState;
    private ActionExecutionState actionExecutionState;
    private CheckEndConditionState checkEndConditionState;

    private AttackAction cachedAttackAction;
    private DefendAction cachedDefendAction;

    public event Action<BattleStateBase> OnStateChanged;
    public event Action<BattleUnit> OnActiveUnitChanged;
    public event Action<BattleUnit> OnPlayerTurnReady;
    public event Action<BattleOutcome> OnBattleEnded;
    public event Action<string> OnStatusTextChanged;

    public IReadOnlyList<BattleUnit> HeroUnits => heroUnits;
    public IReadOnlyList<BattleUnit> EnemyUnits => enemyUnits;
    public TurnOrderQueue TurnQueue => turnQueue;
    public BattleUnit ActiveUnit => activeUnit;
    public ICombatAction PendingAction => pendingAction;
    public BattleOutcome Outcome => battleOutcome;
    public BattleStateBase CurrentState => currentState;
    public string BattleStatusText => battleStatusText;

    public BattleStartState BattleStartState => battleStartState;
    public TurnOrderState TurnOrderState => turnOrderState;
    public PlayerSelectActionState PlayerSelectActionState => playerSelectActionState;
    public EnemyTurnAIState EnemyTurnAIState => enemyTurnAIState;
    public ActionExecutionState ActionExecutionState => actionExecutionState;
    public CheckEndConditionState CheckEndConditionState => checkEndConditionState;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (heroUnits.Count == 0)
        {
            GameObject heroPartyObj = GameObject.Find("HeroParty");
            if (heroPartyObj != null)
            {
                heroPartyObj.GetComponentsInChildren(true, heroUnits);
            }
        }

        if (enemyUnits.Count == 0)
        {
            GameObject monsterPartyObj = GameObject.Find("MonsterParty");
            if (monsterPartyObj != null)
            {
                monsterPartyObj.GetComponentsInChildren(true, enemyUnits);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        battleStartState = new BattleStartState(this);
        turnOrderState = new TurnOrderState(this);
        playerSelectActionState = new PlayerSelectActionState(this);
        enemyTurnAIState = new EnemyTurnAIState(this);
        actionExecutionState = new ActionExecutionState(this);
        checkEndConditionState = new CheckEndConditionState(this);

        cachedAttackAction = new AttackAction(null, null);
        cachedDefendAction = new DefendAction(null);
    }

    protected override void Start()
    {
        base.Start();

        StartBattle();
    }

    private void Update()
    {
        currentState?.Execute();
    }

    public void StartBattle()
    {
        StopAllCoroutines();
        battleOutcome = BattleOutcome.None;
        pendingAction = null;
        activeUnit = null;

        ChangeState(battleStartState);
    }

    public void ChangeState(BattleStateBase newState)
    {
        if (newState == null)
        {
            return;
        }

        currentState?.Exit();
        currentState = newState;
        currentStateName = currentState.StateName;

        OnStateChanged?.Invoke(currentState);
        currentState.Enter();
    }

    public void SetActiveUnit(BattleUnit unit)
    {
        activeUnit = unit;
        OnActiveUnitChanged?.Invoke(activeUnit);
    }

    public void NotifyPlayerTurnReady(BattleUnit hero)
    {
        OnPlayerTurnReady?.Invoke(hero);
    }

    public void SetBattleStatusText(string text)
    {
        battleStatusText = text;
        OnStatusTextChanged?.Invoke(battleStatusText);
    }

    public ICombatAction CreateOrReuseBasicAttack(BattleUnit actor, IDamageable target)
    {
        return cachedAttackAction.Configure(actor, target);
    }

    public ICombatAction CreateOrReuseDefend(BattleUnit actor)
    {
        return cachedDefendAction.Configure(actor);
    }

    public void SubmitAction(ICombatAction action)
    {
        if (action == null)
        {
            return;
        }

        pendingAction = action;
        ChangeState(actionExecutionState);
    }

    public void ClearPendingAction()
    {
        pendingAction = null;
    }

    public void FinishBattle(BattleOutcome outcome)
    {
        battleOutcome = outcome;
        currentState?.Exit();
        currentState = null;
        currentStateName = outcome == BattleOutcome.Victory ? "Victory" : "Defeat";

        string endMsg = outcome == BattleOutcome.Victory
            ? "CHIẾN THẮNG! Toàn bộ quái vật đã bị tiêu diệt!"
            : "THẤT BẠI! Đội hình anh hùng đã gục ngã...";

        SetBattleStatusText(endMsg);
        OnBattleEnded?.Invoke(battleOutcome);
    }

    public void SetParties(List<BattleUnit> heroes, List<BattleUnit> enemies)
    {
        heroUnits = heroes;
        enemyUnits = enemies;
    }

    private void OnGUI()
    {
        if (!showDebugHUD)
        {
            return;
        }

        // Thanh thông tin phía trên màn hình (không che nhân vật ở giữa sân)
        GUI.Box(new Rect(15, 10, Screen.width - 30, 62), $"ROUND {turnQueue.CurrentRound} | STATE: {currentStateName}");
        GUI.Label(new Rect(30, 32, Screen.width - 60, 22), battleStatusText);
        GUI.Label(new Rect(30, 50, Screen.width - 60, 20), BuildTurnOrderString());

        // Bảng điều khiển lệnh phía dưới màn hình
        float bottomY = Screen.height - 95f;
        GUI.Box(new Rect(15, bottomY, Screen.width - 30, 85), "BẢNG ĐIỀU KHIỂN LƯỢT ĐÁNH (PHASE 3 FSM)");

        if (battleOutcome != BattleOutcome.None)
        {
            if (GUI.Button(new Rect(30, bottomY + 30f, 260, 40), "Chơi lại trận đấu (Restart Battle)"))
            {
                StartBattle();
            }
            return;
        }

        if (currentState == playerSelectActionState && activeUnit != null && !activeUnit.IsDead)
        {
            float btnX = 30f;

            for (int i = 0; i < enemyUnits.Count; i++)
            {
                BattleUnit enemy = enemyUnits[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                if (GUI.Button(new Rect(btnX, bottomY + 30f, 210f, 40f), $"Đánh {enemy.UnitName} (HP:{enemy.Health.CurrentHP})"))
                {
                    SetBattleStatusText($"{activeUnit.UnitName} lao tới chém {enemy.UnitName}!");
                    SubmitAction(CreateOrReuseBasicAttack(activeUnit, enemy));
                    return;
                }

                btnX += 220f;
            }

            if (GUI.Button(new Rect(btnX, bottomY + 30f, 190f, 40f), "Phòng Thủ (-50% DMG, +MP)"))
            {
                SetBattleStatusText($"{activeUnit.UnitName} bật khiên Phòng Thủ!");
                SubmitAction(CreateOrReuseDefend(activeUnit));
            }
        }
        else
        {
            GUI.Label(new Rect(30, bottomY + 38f, 500, 25), "Đang thực thi lượt đi tự động...");
        }
    }

    private string BuildTurnOrderString()
    {
        IReadOnlyList<BattleUnit> queue = turnQueue.ActiveTurnList;
        if (queue == null || queue.Count == 0)
        {
            return "Hàng đợi còn lại trong hiệp: (Sắp sang hiệp mới)";
        }

        string text = "Hàng đợi kế tiếp trong hiệp: ";
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] != null && !queue[i].IsDead)
            {
                text += $"[{queue[i].UnitName} - SPD {queue[i].Speed}]  ";
            }
        }
        return text;
    }
}

