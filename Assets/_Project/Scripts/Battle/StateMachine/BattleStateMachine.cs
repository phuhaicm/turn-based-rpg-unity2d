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
    private SkillDataSO selectedSkillForTargeting;
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
    private SkillAction cachedSkillAction;

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
        cachedSkillAction = new SkillAction();
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
        selectedSkillForTargeting = null;
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
        selectedSkillForTargeting = null;
        OnActiveUnitChanged?.Invoke(activeUnit);
    }

    public void NotifyPlayerTurnReady(BattleUnit hero)
    {
        selectedSkillForTargeting = null;
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

    public ICombatAction CreateOrReuseSingleSkill(BattleUnit actor, SkillDataSO skill, IDamageable target)
    {
        return cachedSkillAction.ConfigureSingleTarget(actor, skill, target);
    }

    public ICombatAction CreateOrReuseMultiSkill(BattleUnit actor, SkillDataSO skill, IReadOnlyList<BattleUnit> groupTargets)
    {
        return cachedSkillAction.ConfigureMultiTarget(actor, skill, groupTargets);
    }

    public void SubmitAction(ICombatAction action)
    {
        if (action == null)
        {
            return;
        }

        selectedSkillForTargeting = null;
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

        // Thanh thông tin phía trên màn hình
        GUI.Box(new Rect(15, 10, Screen.width - 30, 62), $"ROUND {turnQueue.CurrentRound} | STATE: {currentStateName}");
        GUI.Label(new Rect(30, 32, Screen.width - 60, 22), battleStatusText);
        GUI.Label(new Rect(30, 50, Screen.width - 60, 20), BuildTurnOrderString());

        // Bảng điều khiển phía dưới màn hình (2 hàng nút cho Đánh thường + Kỹ năng)
        float panelHeight = 130f;
        float bottomY = Screen.height - panelHeight - 10f;
        string headerTitle = activeUnit != null
            ? $"LƯỢT CỦA: {activeUnit.UnitName} (HP: {activeUnit.Health.CurrentHP}/{activeUnit.Health.MaxHP} | MP: {activeUnit.Mana.CurrentMP}/{activeUnit.Mana.MaxMP})"
            : "BẢNG ĐIỀU KHIỂN CHIẾN ĐẤU (PHASE 4: SKILLS & AI)";

        GUI.Box(new Rect(15, bottomY, Screen.width - 30, panelHeight), headerTitle);

        if (battleOutcome != BattleOutcome.None)
        {
            if (GUI.Button(new Rect(30, bottomY + 40f, 260, 42), "Chơi lại trận đấu (Restart Battle)"))
            {
                StartBattle();
            }
            return;
        }

        if (currentState == playerSelectActionState && activeUnit != null && !activeUnit.IsDead)
        {
            if (selectedSkillForTargeting != null)
            {
                DrawSkillTargetSelectionGUI(bottomY);
            }
            else
            {
                DrawMainActionMenuGUI(bottomY);
            }
        }
        else
        {
            GUI.Label(new Rect(30, bottomY + 50f, 500, 25), "Đang thực thi hành động...");
        }
    }

    private void DrawMainActionMenuGUI(float bottomY)
    {
        // Hàng 1: Đánh thường & Phòng thủ
        float row1Y = bottomY + 30f;
        float btnX = 30f;

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            BattleUnit enemy = enemyUnits[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            if (GUI.Button(new Rect(btnX, row1Y, 215f, 38f), $"Đánh {enemy.UnitName} (HP:{enemy.Health.CurrentHP})"))
            {
                SetBattleStatusText($"{activeUnit.UnitName} lao tới chém {enemy.UnitName}!");
                SubmitAction(CreateOrReuseBasicAttack(activeUnit, enemy));
                return;
            }

            btnX += 225f;
        }

        if (GUI.Button(new Rect(btnX, row1Y, 195f, 38f), "Phòng Thủ (-50% DMG, +MP)"))
        {
            SetBattleStatusText($"{activeUnit.UnitName} bật khiên Phòng Thủ!");
            SubmitAction(CreateOrReuseDefend(activeUnit));
            return;
        }

        // Hàng 2: Danh sách Kỹ năng (SkillDataSO) của nhân vật đang tới lượt
        if (activeUnit.CharacterData == null || activeUnit.CharacterData.Skills == null)
        {
            return;
        }

        float row2Y = bottomY + 76f;
        float skillBtnX = 30f;
        IReadOnlyList<SkillDataSO> skills = activeUnit.CharacterData.Skills;

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDataSO skill = skills[i];
            if (skill == null)
            {
                continue;
            }

            bool enoughMana = activeUnit.Mana != null && activeUnit.Mana.HasEnoughMana(skill.ManaCost);
            GUI.enabled = enoughMana;

            string targetTag = skill.TargetType == SkillTargetType.AllEnemies ? " [AOE]" : "";
            string manaWarning = enoughMana ? $"{skill.ManaCost} MP" : $"Thiếu MP ({skill.ManaCost})";
            string label = $"Kỹ năng: {skill.SkillName}{targetTag} ({manaWarning})";

            if (GUI.Button(new Rect(skillBtnX, row2Y, 310f, 38f), label))
            {
                GUI.enabled = true;
                HandleSkillButtonClicked(skill);
                return;
            }

            GUI.enabled = true;
            skillBtnX += 320f;
        }
    }

    private void HandleSkillButtonClicked(SkillDataSO skill)
    {
        if (skill.TargetType == SkillTargetType.AllEnemies)
        {
            SetBattleStatusText($"{activeUnit.UnitName} niệm chú [{skill.SkillName}] tấn công toàn bộ phe địch!");
            SubmitAction(CreateOrReuseMultiSkill(activeUnit, skill, enemyUnits));
            return;
        }

        if (skill.TargetType == SkillTargetType.AllAllies)
        {
            SetBattleStatusText($"{activeUnit.UnitName} dùng [{skill.SkillName}] cho toàn bộ đồng minh!");
            SubmitAction(CreateOrReuseMultiSkill(activeUnit, skill, heroUnits));
            return;
        }

        if (skill.TargetType == SkillTargetType.Self)
        {
            SetBattleStatusText($"{activeUnit.UnitName} dùng [{skill.SkillName}] lên bản thân!");
            SubmitAction(CreateOrReuseSingleSkill(activeUnit, skill, activeUnit));
            return;
        }

        // Nếu là kỹ năng Đơn mục tiêu (SingleEnemy hoặc SingleAlly) thì mở bước chọn mục tiêu
        selectedSkillForTargeting = skill;
        SetBattleStatusText($"Chọn mục tiêu cho kỹ năng [{skill.SkillName}] (-{skill.ManaCost} MP):");
    }

    private void DrawSkillTargetSelectionGUI(float bottomY)
    {
        float rowY = bottomY + 42f;
        float btnX = 30f;

        IReadOnlyList<BattleUnit> candidateList =
            selectedSkillForTargeting.TargetType == SkillTargetType.SingleAlly ? heroUnits : enemyUnits;

        for (int i = 0; i < candidateList.Count; i++)
        {
            BattleUnit target = candidateList[i];
            if (target == null || target.IsDead)
            {
                continue;
            }

            string targetLabel = $"{target.UnitName} (HP: {target.Health.CurrentHP}/{target.Health.MaxHP})";
            if (GUI.Button(new Rect(btnX, rowY, 240f, 42f), targetLabel))
            {
                SkillDataSO chosenSkill = selectedSkillForTargeting;
                SetBattleStatusText($"{activeUnit.UnitName} thi triển [{chosenSkill.SkillName}] lên {target.UnitName}!");
                SubmitAction(CreateOrReuseSingleSkill(activeUnit, chosenSkill, target));
                return;
            }

            btnX += 250f;
        }

        if (GUI.Button(new Rect(btnX + 10f, rowY, 140f, 42f), "Quay lại (Hủy)"))
        {
            selectedSkillForTargeting = null;
            SetBattleStatusText($"Lượt của {activeUnit.UnitName} - Hãy chọn lệnh!");
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
