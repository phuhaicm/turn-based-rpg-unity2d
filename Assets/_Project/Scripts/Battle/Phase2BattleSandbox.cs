using System.Collections;
using UnityEngine;

public class Phase2BattleSandbox : HaiMonoBehaviour
{
    [Header("Battle Units")]
    [SerializeField]
    private BattleUnit heroKnight;

    [SerializeField]
    private BattleUnit heroMage;

    [SerializeField]
    private BattleUnit monsterGoblin;

    [SerializeField]
    private BattleUnit monsterOrc;

    private bool isExecutingAction;
    private string lastActionLog = "Sẵn sàng! Bấm các nút bên dưới để test Đòn đánh & Máu/Mana.";

    private AttackAction cachedAttackAction;
    private DefendAction cachedDefendAction;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (heroKnight == null)
        {
            GameObject obj = GameObject.Find("Hero_Knight");
            if (obj != null)
            {
                heroKnight = obj.GetComponent<BattleUnit>();
            }
        }

        if (heroMage == null)
        {
            GameObject obj = GameObject.Find("Hero_Mage");
            if (obj != null)
            {
                heroMage = obj.GetComponent<BattleUnit>();
            }
        }

        if (monsterGoblin == null)
        {
            GameObject obj = GameObject.Find("Monster_Goblin");
            if (obj != null)
            {
                monsterGoblin = obj.GetComponent<BattleUnit>();
            }
        }

        if (monsterOrc == null)
        {
            GameObject obj = GameObject.Find("Monster_Orc");
            if (obj != null)
            {
                monsterOrc = obj.GetComponent<BattleUnit>();
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        cachedAttackAction = new AttackAction(heroKnight, monsterGoblin);
        cachedDefendAction = new DefendAction(heroKnight);
    }

    public void SetUnits(BattleUnit knight, BattleUnit mage, BattleUnit goblin, BattleUnit orc)
    {
        heroKnight = knight;
        heroMage = mage;
        monsterGoblin = goblin;
        monsterOrc = orc;
    }

    private void RunCombatAction(ICombatAction action, string logMessage)
    {
        if (isExecutingAction || action == null)
        {
            return;
        }

        lastActionLog = logMessage;
        StartCoroutine(ExecuteRoutine(action));
    }

    private IEnumerator ExecuteRoutine(ICombatAction action)
    {
        isExecutingAction = true;

        if (action.Actor != null)
        {
            action.Actor.OnTurnStart();
        }

        yield return action.ExecuteAction();
        isExecutingAction = false;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(15, 15, 430, 310), "PHASE 2 SANDBOX: COMBAT UNIT & COMMAND TESTER");
        GUI.Label(new Rect(30, 45, 400, 40), $"Trạng thái: {lastActionLog}");

        DrawUnitStatus(new Rect(30, 80, 400, 22), heroKnight);
        DrawUnitStatus(new Rect(30, 102, 400, 22), heroMage);
        DrawUnitStatus(new Rect(30, 124, 400, 22), monsterGoblin);
        DrawUnitStatus(new Rect(30, 146, 400, 22), monsterOrc);

        GUI.enabled = !isExecutingAction;

        if (GUI.Button(new Rect(30, 180, 195, 34), "1. Hiệp Sĩ chém Quái"))
        {
            BattleUnit target = (monsterGoblin != null && !monsterGoblin.IsDead) ? monsterGoblin : monsterOrc;
            if (heroKnight != null && target != null && !target.IsDead)
            {
                RunCombatAction(
                    cachedAttackAction.Configure(heroKnight, target),
                    $"{heroKnight.UnitName} lao tới chém {target.UnitName}!"
                );
            }
        }

        if (GUI.Button(new Rect(235, 180, 195, 34), "2. Hiệp Sĩ Bật Khiên Thủ"))
        {
            if (heroKnight != null && !heroKnight.IsDead)
            {
                RunCombatAction(
                    cachedDefendAction.Configure(heroKnight),
                    $"{heroKnight.UnitName} bật Phòng Thủ (-50% sát thương)!"
                );
            }
        }

        if (GUI.Button(new Rect(30, 222, 195, 34), "3. Orc bổ rìu vào Hiệp Sĩ"))
        {
            if (monsterOrc != null && !monsterOrc.IsDead && heroKnight != null && !heroKnight.IsDead)
            {
                RunCombatAction(
                    cachedAttackAction.Configure(monsterOrc, heroKnight),
                    $"{monsterOrc.UnitName} bổ rìu vào {heroKnight.UnitName}!"
                );
            }
        }

        if (GUI.Button(new Rect(235, 222, 195, 34), "4. Pháp Sư Hồi Máu (-15 MP)"))
        {
            if (heroMage != null && !heroMage.IsDead && heroKnight != null && !heroKnight.IsDead)
            {
                if (heroMage.Mana.TrySpendMana(15))
                {
                    int healed = heroKnight.Heal(35);
                    lastActionLog = $"{heroMage.UnitName} tiêu 15 MP, hồi {healed} HP cho {heroKnight.UnitName}!";
                }
                else
                {
                    lastActionLog = $"{heroMage.UnitName} không đủ Mana (Cần 15 MP)!";
                }
            }
        }

        if (GUI.Button(new Rect(30, 268, 400, 36), "Reset lại toàn bộ Máu & Mana"))
        {
            if (heroKnight != null) heroKnight.SetupUnit(heroKnight.CharacterData);
            if (heroMage != null) heroMage.SetupUnit(heroMage.CharacterData);
            if (monsterGoblin != null) monsterGoblin.SetupUnit(monsterGoblin.CharacterData);
            if (monsterOrc != null) monsterOrc.SetupUnit(monsterOrc.CharacterData);
            lastActionLog = "Đã hồi đầy HP/MP cho toàn bộ đội hình!";
        }

        GUI.enabled = true;
    }

    private void DrawUnitStatus(Rect rect, BattleUnit unit)
    {
        if (unit == null || unit.Health == null || unit.Mana == null)
        {
            return;
        }

        string defendTag = unit.IsDefending ? " [THỦ -50% DMG]" : "";
        string deadTag = unit.IsDead ? " [ĐÃ CHẾT]" : "";
        GUI.Label(
            rect,
            $"{unit.UnitName}: HP {unit.Health.CurrentHP}/{unit.Health.MaxHP} | MP {unit.Mana.CurrentMP}/{unit.Mana.MaxMP}{defendTag}{deadTag}"
        );
    }
}
