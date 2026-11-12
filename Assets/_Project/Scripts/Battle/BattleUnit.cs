using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(ManaSystem))]
public class BattleUnit : HaiMonoBehaviour, IDamageable, ITurnCombatant
{
    [Header("Character Data")]
    [SerializeField]
    private CharacterDataSO characterData;

    [Header("Components")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private HealthSystem healthSystem;

    [SerializeField]
    private ManaSystem manaSystem;

    [Header("World-Space Bars")]
    [SerializeField]
    private Transform hpBarFill;

    [SerializeField]
    private Transform mpBarFill;

    [SerializeField]
    private Transform hitPoint;

    [Header("Runtime State")]
    [SerializeField]
    private bool isDefending;

    private Vector3 originalPosition;
    private Vector3 originalScale = Vector3.one;
    private Color originalColor = Color.white;
    private Coroutine hitFlashCoroutine;
    private Coroutine scalePunchCoroutine;

    // Khởi tạo sẵn 1 lần để dùng lại trong Coroutine, tránh sinh rác bộ nhớ (Zero-GC)
    private static readonly WaitForSeconds HitFlashWait = new WaitForSeconds(0.12f);

    // Băm sẵn tên biến Animator ra số nguyên (int) 1 lần duy nhất để không tốn CPU băm chuỗi lúc Runtime
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
    private static readonly int DieTriggerHash = Animator.StringToHash("Die");
    private static readonly int DefendBoolHash = Animator.StringToHash("IsDefending");

    public event Action<BattleUnit> OnUnitDied;
    public event Action<BattleUnit, bool> OnDefendStateChanged;
    public event Action<BattleUnit, int> OnUnitDamageTaken;
    public event Action<BattleUnit, int> OnUnitHealed;

    public string UnitName => characterData != null ? characterData.CharacterName : name;
    public int Speed => characterData != null ? characterData.Speed : 10;
    public bool IsPlayerUnit => characterData != null && characterData.IsPlayerHero;
    public bool IsDead => healthSystem != null && healthSystem.IsDead;

    public Vector3 HitPosition =>
        hitPoint != null ? hitPoint.position : transform.position + new Vector3(0f, 0.8f, 0f);

    public CharacterDataSO CharacterData => characterData;
    public HealthSystem Health => healthSystem;
    public ManaSystem Mana => manaSystem;
    public bool IsDefending => isDefending;
    public Vector3 OriginalPosition => originalPosition;

    public int CurrentAttack => characterData != null ? characterData.BaseATK : 10;
    public int CurrentDefense => characterData != null ? characterData.BaseDEF : 0;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
        }

        if (manaSystem == null)
        {
            manaSystem = GetComponent<ManaSystem>();
        }

        if (hpBarFill == null)
        {
            Transform foundHp = transform.Find("OverheadBars/HPBar_BG/HPBar_Fill");
            if (foundHp != null)
            {
                hpBarFill = foundHp;
            }
        }

        if (mpBarFill == null)
        {
            Transform foundMp = transform.Find("OverheadBars/MPBar_BG/MPBar_Fill");
            if (foundMp != null)
            {
                mpBarFill = foundMp;
            }
        }

        if (hitPoint == null)
        {
            Transform foundHit = transform.Find("HitPoint");
            if (foundHit != null)
            {
                hitPoint = foundHit;
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        originalPosition = transform.position;
        originalScale = transform.localScale;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Luôn hủy đăng ký (-=) trước khi đăng ký (+=) để tránh bị gọi trùng sự kiện
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnHealthChanged += HandleHealthChanged;

            healthSystem.OnDamageTaken -= HandleDamageTaken;
            healthSystem.OnDamageTaken += HandleDamageTaken;

            healthSystem.OnHealed -= HandleUnitHealed;
            healthSystem.OnHealed += HandleUnitHealed;

            healthSystem.OnDied -= HandleUnitDied;
            healthSystem.OnDied += HandleUnitDied;
        }

        if (manaSystem != null)
        {
            manaSystem.OnManaChanged -= HandleManaChanged;
            manaSystem.OnManaChanged += HandleManaChanged;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // Bắt buộc hủy đăng ký khi tắt Object để tránh rò rỉ bộ nhớ (Memory Leak)
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnDamageTaken -= HandleDamageTaken;
            healthSystem.OnHealed -= HandleUnitHealed;
            healthSystem.OnDied -= HandleUnitDied;
        }

        if (manaSystem != null)
        {
            manaSystem.OnManaChanged -= HandleManaChanged;
        }
    }

    protected override void Start()
    {
        base.Start();

        if (characterData != null)
        {
            SetupUnit(characterData);
        }
    }

    public void SetupUnit(CharacterDataSO data)
    {
        if (data == null)
        {
            return;
        }

        characterData = data;
        isDefending = false;
        originalPosition = transform.position;
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            if (characterData.CharacterSprite != null)
            {
                spriteRenderer.sprite = characterData.CharacterSprite;
            }

            spriteRenderer.color = Color.white;
            originalColor = Color.white;
        }

        if (animator != null && characterData.AnimatorController != null)
        {
            animator.runtimeAnimatorController = characterData.AnimatorController;
        }

        if (healthSystem != null)
        {
            healthSystem.Initialize(characterData.MaxHP);
        }

        if (manaSystem != null)
        {
            manaSystem.Initialize(characterData.MaxMP);
        }
    }

    public void OnTurnStart()
    {
        if (isDefending)
        {
            SetDefending(false);
        }
    }

    public void SetDefending(bool defending)
    {
        isDefending = defending;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(DefendBoolHash, isDefending);
        }

        if (spriteRenderer != null && !IsDead)
        {
            spriteRenderer.color = isDefending ? new Color(0.75f, 0.9f, 1f, 1f) : originalColor;
        }

        OnDefendStateChanged?.Invoke(this, isDefending);
    }

    public void PlayAttackAnimation()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(AttackTriggerHash);
        }

        TriggerScalePunch(new Vector3(1.18f, 0.88f, 1f), 0.14f);
    }

    public int TakeDamage(int rawDamage)
    {
        if (IsDead || healthSystem == null)
        {
            return 0;
        }

        int mitigatedDamage = Mathf.Max(1, rawDamage - CurrentDefense);

        // Nếu đang bật khiên Phòng Thủ thì giảm 50% sát thương nhận vào
        if (isDefending)
        {
            mitigatedDamage = Mathf.Max(1, Mathf.RoundToInt(mitigatedDamage * 0.5f));
        }

        int actualDamage = healthSystem.TakeDamage(mitigatedDamage);
        return actualDamage;
    }

    public int Heal(int healAmount)
    {
        if (IsDead || healthSystem == null)
        {
            return 0;
        }

        return healthSystem.Heal(healAmount);
    }

    private void HandleHealthChanged(int currentHP, int maxHP)
    {
        if (hpBarFill == null || maxHP <= 0)
        {
            return;
        }

        Vector3 scale = hpBarFill.localScale;
        scale.x = Mathf.Clamp01((float)currentHP / maxHP);
        hpBarFill.localScale = scale;
    }

    private void HandleManaChanged(int currentMP, int maxMP)
    {
        if (mpBarFill == null)
        {
            return;
        }

        Vector3 scale = mpBarFill.localScale;
        scale.x = maxMP > 0 ? Mathf.Clamp01((float)currentMP / maxMP) : 0f;
        mpBarFill.localScale = scale;
    }

    private void HandleDamageTaken(int damage)
    {
        OnUnitDamageTaken?.Invoke(this, damage);

        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (animator != null && animator.runtimeAnimatorController != null && !IsDead)
        {
            animator.SetTrigger(HitTriggerHash);
        }

        TriggerScalePunch(new Vector3(0.84f, 1.16f, 1f), 0.15f);

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private void HandleUnitHealed(int healAmount)
    {
        OnUnitHealed?.Invoke(this, healAmount);

        if (gameObject.activeInHierarchy)
        {
            TriggerScalePunch(new Vector3(1.12f, 1.12f, 1f), 0.18f);
        }
    }

    private void TriggerScalePunch(Vector3 multiplier, float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (scalePunchCoroutine != null)
        {
            StopCoroutine(scalePunchCoroutine);
            transform.localScale = originalScale;
        }

        scalePunchCoroutine = StartCoroutine(ScalePunchRoutine(multiplier, duration));
    }

    private IEnumerator ScalePunchRoutine(Vector3 multiplier, float duration)
    {
        Vector3 punchedScale = new Vector3(
            originalScale.x * multiplier.x,
            originalScale.y * multiplier.y,
            originalScale.z
        );

        transform.localScale = punchedScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(punchedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        scalePunchCoroutine = null;
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);
            yield return HitFlashWait;

            if (!IsDead)
            {
                spriteRenderer.color = isDefending ? new Color(0.75f, 0.9f, 1f, 1f) : originalColor;
            }
        }

        hitFlashCoroutine = null;
    }

    private void HandleUnitDied()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(DieTriggerHash);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        }

        transform.localScale = originalScale;
        OnUnitDied?.Invoke(this);
    }
}
