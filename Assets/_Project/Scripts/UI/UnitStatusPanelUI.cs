using UnityEngine;
using UnityEngine.UI;

public class UnitStatusPanelUI : HaiMonoBehaviour
{
    [Header("Target Unit")]
    [SerializeField]
    private BattleUnit boundUnit;

    [Header("UI Elements")]
    [SerializeField]
    private Image panelBackground;

    [SerializeField]
    private Image portraitImage;

    [SerializeField]
    private Text unitNameText;

    [SerializeField]
    private SmoothBarUI hpSmoothBar;

    [SerializeField]
    private SmoothBarUI mpSmoothBar;

    private readonly Color normalBgColor = new Color(0.12f, 0.15f, 0.22f, 0.92f);
    private readonly Color activeTurnBgColor = new Color(0.28f, 0.36f, 0.14f, 0.96f);
    private readonly Color deadBgColor = new Color(0.18f, 0.1f, 0.1f, 0.7f);

    public BattleUnit BoundUnit => boundUnit;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (panelBackground == null)
        {
            panelBackground = GetComponent<Image>();
        }

        if (unitNameText == null)
        {
            Transform nameObj = transform.Find("UnitNameText");
            if (nameObj != null)
            {
                unitNameText = nameObj.GetComponent<Text>();
            }
        }

        if (portraitImage == null)
        {
            Transform iconObj = transform.Find("PortraitIcon");
            if (iconObj != null)
            {
                portraitImage = iconObj.GetComponent<Image>();
            }
        }

        if (hpSmoothBar == null)
        {
            Transform hpObj = transform.Find("HPBar");
            if (hpObj != null)
            {
                hpSmoothBar = hpObj.GetComponent<SmoothBarUI>();
            }
        }

        if (mpSmoothBar == null)
        {
            Transform mpObj = transform.Find("MPBar");
            if (mpObj != null)
            {
                mpSmoothBar = mpObj.GetComponent<SmoothBarUI>();
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        SubscribeUnitEvents();
    }

    protected override void Start()
    {
        base.Start();

        if (boundUnit != null)
        {
            BindUnit(boundUnit);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        UnsubscribeUnitEvents();
    }

    public void ConfigurePanelElements(
        BattleUnit unit,
        Image bg,
        Image portrait,
        Text nameLabel,
        SmoothBarUI hpBar,
        SmoothBarUI mpBar)
    {
        boundUnit = unit;
        panelBackground = bg;
        portraitImage = portrait;
        unitNameText = nameLabel;
        hpSmoothBar = hpBar;
        mpSmoothBar = mpBar;
    }

    public void BindUnit(BattleUnit unit)
    {
        UnsubscribeUnitEvents();
        boundUnit = unit;

        if (boundUnit == null)
        {
            return;
        }

        SubscribeUnitEvents();
        RefreshAllInitialValues();
    }

    public void SetActiveTurnHighlight(bool isActiveTurn)
    {
        if (panelBackground == null)
        {
            return;
        }

        if (boundUnit != null && boundUnit.IsDead)
        {
            panelBackground.color = deadBgColor;
            return;
        }

        panelBackground.color = isActiveTurn ? activeTurnBgColor : normalBgColor;
    }

    private void SubscribeUnitEvents()
    {
        if (boundUnit == null)
        {
            return;
        }

        if (boundUnit.Health != null)
        {
            boundUnit.Health.OnHealthChanged -= HandleHealthChanged;
            boundUnit.Health.OnHealthChanged += HandleHealthChanged;
        }

        if (boundUnit.Mana != null)
        {
            boundUnit.Mana.OnManaChanged -= HandleManaChanged;
            boundUnit.Mana.OnManaChanged += HandleManaChanged;
        }

        boundUnit.OnDefendStateChanged -= HandleDefendChanged;
        boundUnit.OnDefendStateChanged += HandleDefendChanged;

        boundUnit.OnUnitDied -= HandleUnitDied;
        boundUnit.OnUnitDied += HandleUnitDied;
    }

    private void UnsubscribeUnitEvents()
    {
        if (boundUnit == null)
        {
            return;
        }

        if (boundUnit.Health != null)
        {
            boundUnit.Health.OnHealthChanged -= HandleHealthChanged;
        }

        if (boundUnit.Mana != null)
        {
            boundUnit.Mana.OnManaChanged -= HandleManaChanged;
        }

        boundUnit.OnDefendStateChanged -= HandleDefendChanged;
        boundUnit.OnUnitDied -= HandleUnitDied;
    }

    private void RefreshAllInitialValues()
    {
        RefreshNameLabel();

        if (portraitImage != null && boundUnit.CharacterData != null)
        {
            portraitImage.sprite = boundUnit.CharacterData.PortraitIcon;
        }

        if (hpSmoothBar != null && boundUnit.Health != null)
        {
            hpSmoothBar.SetValueImmediate(boundUnit.Health.CurrentHP, boundUnit.Health.MaxHP);
        }

        if (mpSmoothBar != null && boundUnit.Mana != null)
        {
            mpSmoothBar.SetValueImmediate(boundUnit.Mana.CurrentMP, boundUnit.Mana.MaxMP);
        }
    }

    private void HandleHealthChanged(int currentHP, int maxHP)
    {
        if (hpSmoothBar != null)
        {
            hpSmoothBar.UpdateBarValue(currentHP, maxHP);
        }
    }

    private void HandleManaChanged(int currentMP, int maxMP)
    {
        if (mpSmoothBar != null)
        {
            mpSmoothBar.UpdateBarValue(currentMP, maxMP);
        }
    }

    private void HandleDefendChanged(BattleUnit unit, bool isDefending)
    {
        RefreshNameLabel();
    }

    private void HandleUnitDied(BattleUnit unit)
    {
        RefreshNameLabel();
        if (panelBackground != null)
        {
            panelBackground.color = deadBgColor;
        }
    }

    private void RefreshNameLabel()
    {
        if (unitNameText == null || boundUnit == null)
        {
            return;
        }

        if (boundUnit.IsDead)
        {
            unitNameText.text = $"{boundUnit.UnitName} [HẠ GỤC]";
            return;
        }

        string shieldTag = boundUnit.IsDefending ? " [THỦ]" : "";
        unitNameText.text = $"{boundUnit.UnitName} (SPD {boundUnit.Speed}){shieldTag}";
    }
}

