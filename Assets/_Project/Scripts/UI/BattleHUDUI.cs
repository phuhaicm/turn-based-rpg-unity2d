using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUI : HaiMonoBehaviour
{
    [Header("Core References")]
    [SerializeField]
    private BattleStateMachine stateMachine;

    [Header("Top Banner UI")]
    [SerializeField]
    private Text roundAndStateText;

    [SerializeField]
    private Text battleStatusText;

    [SerializeField]
    private Text turnOrderQueueText;

    [Header("Unit Status Panels")]
    [SerializeField]
    private List<UnitStatusPanelUI> unitStatusPanels = new List<UnitStatusPanelUI>();

    [Header("Command & Skill Menu UI")]
    [SerializeField]
    private GameObject commandPanelRoot;

    [SerializeField]
    private Text activeHeroHeaderText;

    [SerializeField]
    private GameObject mainActionButtonsGroup;

    [SerializeField]
    private Button basicAttackButton;

    [SerializeField]
    private Button defendButton;

    [SerializeField]
    private List<Button> skillButtons = new List<Button>();

    [SerializeField]
    private List<Text> skillButtonLabels = new List<Text>();

    [Header("Target Selection UI")]
    [SerializeField]
    private GameObject targetSelectionGroup;

    [SerializeField]
    private List<Button> targetButtons = new List<Button>();

    [SerializeField]
    private List<Text> targetButtonLabels = new List<Text>();

    [SerializeField]
    private Button cancelTargetButton;

    [Header("End Battle Overlay UI")]
    [SerializeField]
    private GameObject endBattlePanel;

    [SerializeField]
    private Text endBattleTitleText;

    [SerializeField]
    private Button restartBattleButton;

    // Dùng StringBuilder khởi tạo sẵn để ghép chuỗi Turn Order không sinh rác bộ nhớ (Zero-GC)
    private readonly StringBuilder turnOrderBuilder = new StringBuilder(160);
    private readonly List<BattleUnit> validTargetBuffer = new List<BattleUnit>(4);

    private bool isSelectingBasicAttackTarget;
    private SkillDataSO pendingSingleTargetSkill;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (stateMachine == null)
        {
            stateMachine = FindFirstObjectByType<BattleStateMachine>();
        }

        if (unitStatusPanels.Count == 0)
        {
            GetComponentsInChildren(true, unitStatusPanels);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        BindButtonClicks();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= HandleStateChanged;
            stateMachine.OnStateChanged += HandleStateChanged;

            stateMachine.OnActiveUnitChanged -= HandleActiveUnitChanged;
            stateMachine.OnActiveUnitChanged += HandleActiveUnitChanged;

            stateMachine.OnPlayerTurnReady -= HandlePlayerTurnReady;
            stateMachine.OnPlayerTurnReady += HandlePlayerTurnReady;

            stateMachine.OnStatusTextChanged -= HandleStatusTextChanged;
            stateMachine.OnStatusTextChanged += HandleStatusTextChanged;

            stateMachine.OnBattleEnded -= HandleBattleEnded;
            stateMachine.OnBattleEnded += HandleBattleEnded;

            if (stateMachine.TurnQueue != null)
            {
                stateMachine.TurnQueue.OnRoundStarted -= HandleRoundStarted;
                stateMachine.TurnQueue.OnRoundStarted += HandleRoundStarted;

                stateMachine.TurnQueue.OnQueueUpdated -= HandleQueueUpdated;
                stateMachine.TurnQueue.OnQueueUpdated += HandleQueueUpdated;
            }
        }
    }

    protected override void Start()
    {
        base.Start();

        if (stateMachine != null)
        {
            // Tắt HUD OnGUI tạm thời để nhường chỗ cho Canvas Battle HUD chính thức
            stateMachine.SetShowDebugHUD(false);
        }

        if (endBattlePanel != null)
        {
            endBattlePanel.SetActive(false);
        }

        RefreshTopBanner();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= HandleStateChanged;
            stateMachine.OnActiveUnitChanged -= HandleActiveUnitChanged;
            stateMachine.OnPlayerTurnReady -= HandlePlayerTurnReady;
            stateMachine.OnStatusTextChanged -= HandleStatusTextChanged;
            stateMachine.OnBattleEnded -= HandleBattleEnded;

            if (stateMachine.TurnQueue != null)
            {
                stateMachine.TurnQueue.OnRoundStarted -= HandleRoundStarted;
                stateMachine.TurnQueue.OnQueueUpdated -= HandleQueueUpdated;
            }
        }
    }

    public void ConfigureHUDReferences(
        BattleStateMachine fsm,
        Text roundStateTxt,
        Text statusTxt,
        Text queueTxt,
        List<UnitStatusPanelUI> panels,
        GameObject cmdRoot,
        Text heroHeaderTxt,
        GameObject mainGroup,
        Button atkBtn,
        Button defBtn,
        List<Button> sBtns,
        List<Text> sLabels,
        GameObject targetGroup,
        List<Button> tBtns,
        List<Text> tLabels,
        Button cancelBtn,
        GameObject endPanel,
        Text endTitleTxt,
        Button restartBtn)
    {
        stateMachine = fsm;
        roundAndStateText = roundStateTxt;
        battleStatusText = statusTxt;
        turnOrderQueueText = queueTxt;
        unitStatusPanels = panels;
        commandPanelRoot = cmdRoot;
        activeHeroHeaderText = heroHeaderTxt;
        mainActionButtonsGroup = mainGroup;
        basicAttackButton = atkBtn;
        defendButton = defBtn;
        skillButtons = sBtns;
        skillButtonLabels = sLabels;
        targetSelectionGroup = targetGroup;
        targetButtons = tBtns;
        targetButtonLabels = tLabels;
        cancelTargetButton = cancelBtn;
        endBattlePanel = endPanel;
        endBattleTitleText = endTitleTxt;
        restartBattleButton = restartBtn;
    }

    private void BindButtonClicks()
    {
        if (basicAttackButton != null)
        {
            basicAttackButton.onClick.RemoveAllListeners();
            basicAttackButton.onClick.AddListener(OnBasicAttackButtonClicked);
        }

        if (defendButton != null)
        {
            defendButton.onClick.RemoveAllListeners();
            defendButton.onClick.AddListener(OnDefendButtonClicked);
        }

        if (cancelTargetButton != null)
        {
            cancelTargetButton.onClick.RemoveAllListeners();
            cancelTargetButton.onClick.AddListener(OnCancelTargetClicked);
        }

        if (restartBattleButton != null)
        {
            restartBattleButton.onClick.RemoveAllListeners();
            restartBattleButton.onClick.AddListener(OnRestartBattleClicked);
        }

        for (int i = 0; i < skillButtons.Count; i++)
        {
            int skillIndex = i;
            if (skillButtons[i] != null)
            {
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
            }
        }

        for (int i = 0; i < targetButtons.Count; i++)
        {
            int targetIndex = i;
            if (targetButtons[i] != null)
            {
                targetButtons[i].onClick.RemoveAllListeners();
                targetButtons[i].onClick.AddListener(() => OnTargetButtonClicked(targetIndex));
            }
        }
    }

    private void HandleStateChanged(BattleStateBase newState)
    {
        RefreshTopBanner();

        bool isPlayerTurn = stateMachine != null && newState == stateMachine.PlayerSelectActionState;
        if (commandPanelRoot != null)
        {
            commandPanelRoot.SetActive(isPlayerTurn);
        }
    }

    private void HandleActiveUnitChanged(BattleUnit activeUnit)
    {
        for (int i = 0; i < unitStatusPanels.Count; i++)
        {
            UnitStatusPanelUI panel = unitStatusPanels[i];
            if (panel != null)
            {
                panel.SetActiveTurnHighlight(panel.BoundUnit == activeUnit);
            }
        }
    }

    private void HandlePlayerTurnReady(BattleUnit activeHero)
    {
        isSelectingBasicAttackTarget = false;
        pendingSingleTargetSkill = null;

        if (endBattlePanel != null)
        {
            endBattlePanel.SetActive(false);
        }

        if (commandPanelRoot != null)
        {
            commandPanelRoot.SetActive(true);
        }

        ShowMainActionButtons(activeHero);
    }

    private void HandleStatusTextChanged(string status)
    {
        if (battleStatusText != null)
        {
            battleStatusText.text = status;
        }
    }

    private void HandleRoundStarted(int round)
    {
        RefreshTopBanner();
    }

    private void HandleQueueUpdated(IReadOnlyList<BattleUnit> queue)
    {
        RefreshTurnOrderText(queue);
    }

    private void HandleBattleEnded(BattleOutcome outcome)
    {
        if (commandPanelRoot != null)
        {
            commandPanelRoot.SetActive(false);
        }

        if (endBattlePanel != null)
        {
            endBattlePanel.SetActive(true);
        }

        if (endBattleTitleText != null)
        {
            endBattleTitleText.text = outcome == BattleOutcome.Victory
                ? "CHIẾN THẮNG! TOÀN BỘ QUÁI ĐÃ BỊ HẠ GỤC"
                : "THẤT BẠI! ĐỘI HÌNH ANH HÙNG ĐÃ NGÃ XUỐNG";

            endBattleTitleText.color = outcome == BattleOutcome.Victory
                ? new Color(1f, 0.85f, 0.25f, 1f)
                : new Color(1f, 0.35f, 0.35f, 1f);
        }
    }

    private void ShowMainActionButtons(BattleUnit activeHero)
    {
        if (activeHero == null)
        {
            return;
        }

        if (activeHeroHeaderText != null)
        {
            activeHeroHeaderText.text = $"LỆNH CỦA: {activeHero.UnitName.ToUpper()}";
        }

        if (mainActionButtonsGroup != null)
        {
            mainActionButtonsGroup.SetActive(true);
        }

        if (targetSelectionGroup != null)
        {
            targetSelectionGroup.SetActive(false);
        }

        IReadOnlyList<SkillDataSO> skills = activeHero.CharacterData != null
            ? activeHero.CharacterData.Skills
            : null;

        for (int i = 0; i < skillButtons.Count; i++)
        {
            Button btn = skillButtons[i];
            if (btn == null)
            {
                continue;
            }

            if (skills != null && i < skills.Count && skills[i] != null)
            {
                SkillDataSO skill = skills[i];
                btn.gameObject.SetActive(true);

                bool hasMana = activeHero.Mana != null && activeHero.Mana.HasEnoughMana(skill.ManaCost);
                btn.interactable = hasMana;

                if (i < skillButtonLabels.Count && skillButtonLabels[i] != null)
                {
                    string aoeTag = skill.TargetType == SkillTargetType.AllEnemies ? " [AOE]" : "";
                    skillButtonLabels[i].text = $"{skill.SkillName}{aoeTag} ({skill.ManaCost} MP)";
                }
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void OnBasicAttackButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (stateMachine == null || stateMachine.ActiveUnit == null)
        {
            return;
        }

        isSelectingBasicAttackTarget = true;
        pendingSingleTargetSkill = null;
        OpenTargetSelection(stateMachine.EnemyUnits, "CHỌN QUÁI ĐỂ ĐÁNH THƯỜNG:");
    }

    private void OnDefendButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (stateMachine == null || stateMachine.ActiveUnit == null)
        {
            return;
        }

        BattleUnit actor = stateMachine.ActiveUnit;
        stateMachine.SetBattleStatusText($"{actor.UnitName} bật khiên Phòng Thủ (-50% sát thương & hồi MP)!");
        stateMachine.SubmitAction(stateMachine.CreateOrReuseDefend(actor));
    }

    private void OnSkillButtonClicked(int skillIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (stateMachine == null || stateMachine.ActiveUnit == null)
        {
            return;
        }

        BattleUnit actor = stateMachine.ActiveUnit;
        IReadOnlyList<SkillDataSO> skills = actor.CharacterData != null ? actor.CharacterData.Skills : null;

        if (skills == null || skillIndex < 0 || skillIndex >= skills.Count)
        {
            return;
        }

        SkillDataSO skill = skills[skillIndex];
        if (skill == null)
        {
            return;
        }

        if (skill.TargetType == SkillTargetType.AllEnemies)
        {
            stateMachine.SetBattleStatusText($"{actor.UnitName} tung [{skill.SkillName}] tấn công toàn bộ phe địch!");
            stateMachine.SubmitAction(stateMachine.CreateOrReuseMultiSkill(actor, skill, stateMachine.EnemyUnits));
            return;
        }

        if (skill.TargetType == SkillTargetType.AllAllies)
        {
            stateMachine.SetBattleStatusText($"{actor.UnitName} dùng [{skill.SkillName}] cho toàn đội!");
            stateMachine.SubmitAction(stateMachine.CreateOrReuseMultiSkill(actor, skill, stateMachine.HeroUnits));
            return;
        }

        if (skill.TargetType == SkillTargetType.Self)
        {
            stateMachine.SetBattleStatusText($"{actor.UnitName} dùng [{skill.SkillName}] lên bản thân!");
            stateMachine.SubmitAction(stateMachine.CreateOrReuseSingleSkill(actor, skill, actor));
            return;
        }

        isSelectingBasicAttackTarget = false;
        pendingSingleTargetSkill = skill;

        IReadOnlyList<BattleUnit> candidates = skill.TargetType == SkillTargetType.SingleAlly
            ? stateMachine.HeroUnits
            : stateMachine.EnemyUnits;

        OpenTargetSelection(candidates, $"CHỌN MỤC TIÊU CHO [{skill.SkillName.ToUpper()}]:");
    }

    private void OpenTargetSelection(IReadOnlyList<BattleUnit> candidates, string headerPrompt)
    {
        validTargetBuffer.Clear();

        if (candidates != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && !candidates[i].IsDead)
                {
                    validTargetBuffer.Add(candidates[i]);
                }
            }
        }

        if (activeHeroHeaderText != null)
        {
            activeHeroHeaderText.text = headerPrompt;
        }

        if (mainActionButtonsGroup != null)
        {
            mainActionButtonsGroup.SetActive(false);
        }

        if (targetSelectionGroup != null)
        {
            targetSelectionGroup.SetActive(true);
        }

        for (int i = 0; i < targetButtons.Count; i++)
        {
            Button btn = targetButtons[i];
            if (btn == null)
            {
                continue;
            }

            if (i < validTargetBuffer.Count)
            {
                BattleUnit targetUnit = validTargetBuffer[i];
                btn.gameObject.SetActive(true);

                if (i < targetButtonLabels.Count && targetButtonLabels[i] != null)
                {
                    targetButtonLabels[i].text = $"{targetUnit.UnitName} (HP: {targetUnit.Health.CurrentHP})";
                }
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void OnTargetButtonClicked(int targetIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (stateMachine == null || stateMachine.ActiveUnit == null)
        {
            return;
        }

        if (targetIndex < 0 || targetIndex >= validTargetBuffer.Count)
        {
            return;
        }

        BattleUnit actor = stateMachine.ActiveUnit;
        BattleUnit chosenTarget = validTargetBuffer[targetIndex];

        if (isSelectingBasicAttackTarget)
        {
            stateMachine.SetBattleStatusText($"{actor.UnitName} lao tới chém {chosenTarget.UnitName}!");
            stateMachine.SubmitAction(stateMachine.CreateOrReuseBasicAttack(actor, chosenTarget));
            return;
        }

        if (pendingSingleTargetSkill != null)
        {
            SkillDataSO skill = pendingSingleTargetSkill;
            stateMachine.SetBattleStatusText($"{actor.UnitName} thi triển [{skill.SkillName}] lên {chosenTarget.UnitName}!");
            stateMachine.SubmitAction(stateMachine.CreateOrReuseSingleSkill(actor, skill, chosenTarget));
        }
    }

    private void OnCancelTargetClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (stateMachine != null && stateMachine.ActiveUnit != null)
        {
            ShowMainActionButtons(stateMachine.ActiveUnit);
        }
    }

    private void OnRestartBattleClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
        }

        if (endBattlePanel != null)
        {
            endBattlePanel.SetActive(false);
        }

        if (stateMachine != null)
        {
            stateMachine.StartBattle();
        }
    }

    private void RefreshTopBanner()
    {
        if (stateMachine == null)
        {
            return;
        }

        if (roundAndStateText != null && stateMachine.TurnQueue != null)
        {
            string stateName = stateMachine.CurrentState != null ? stateMachine.CurrentState.StateName : "End";
            roundAndStateText.text = $"HIỆP {stateMachine.TurnQueue.CurrentRound}  |  TRẠNG THÁI: {stateName}";
        }

        if (stateMachine.TurnQueue != null)
        {
            RefreshTurnOrderText(stateMachine.TurnQueue.ActiveTurnList);
        }
    }

    private void RefreshTurnOrderText(IReadOnlyList<BattleUnit> queue)
    {
        if (turnOrderQueueText == null)
        {
            return;
        }

        turnOrderBuilder.Clear();
        turnOrderBuilder.Append("Hàng đợi lượt kế tiếp: ");

        if (queue == null || queue.Count == 0)
        {
            turnOrderBuilder.Append("(Chuẩn bị sang hiệp mới)");
            turnOrderQueueText.text = turnOrderBuilder.ToString();
            return;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            BattleUnit unit = queue[i];
            if (unit != null && !unit.IsDead)
            {
                turnOrderBuilder.Append('[');
                turnOrderBuilder.Append(unit.UnitName);
                turnOrderBuilder.Append(" SPD:");
                turnOrderBuilder.Append(unit.Speed);
                turnOrderBuilder.Append("]  ");
            }
        }

        turnOrderQueueText.text = turnOrderBuilder.ToString();
    }
}

