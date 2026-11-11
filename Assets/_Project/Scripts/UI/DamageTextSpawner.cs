using System.Collections.Generic;
using UnityEngine;

public class DamageTextSpawner : HaiMonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BattleStateMachine stateMachine;

    [SerializeField]
    private ObjectPoolManager poolManager;

    [SerializeField]
    private GameObject damageTextPrefab;

    [Header("Spawn Offset")]
    [SerializeField]
    private Vector3 spawnOffset = new Vector3(0f, 0.45f, 0f);

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (stateMachine == null)
        {
            stateMachine = FindFirstObjectByType<BattleStateMachine>();
        }

        if (poolManager == null)
        {
            poolManager = FindFirstObjectByType<ObjectPoolManager>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        RegisterAllUnits();
    }

    protected override void Start()
    {
        base.Start();

        RegisterAllUnits();

        if (poolManager != null && damageTextPrefab != null)
        {
            poolManager.EnsurePoolCreated(damageTextPrefab, 8);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        UnregisterAllUnits();
    }

    public void SetupSpawner(BattleStateMachine fsm, ObjectPoolManager pool, GameObject prefab)
    {
        stateMachine = fsm;
        poolManager = pool;
        damageTextPrefab = prefab;
    }

    private void RegisterAllUnits()
    {
        if (stateMachine == null)
        {
            return;
        }

        RegisterUnitList(stateMachine.HeroUnits);
        RegisterUnitList(stateMachine.EnemyUnits);
    }

    private void RegisterUnitList(IReadOnlyList<BattleUnit> units)
    {
        if (units == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            // Hủy đăng ký trước khi đăng ký để tránh nhân đôi sự kiện
            unit.OnUnitDamageTaken -= HandleUnitDamageTaken;
            unit.OnUnitDamageTaken += HandleUnitDamageTaken;

            unit.OnUnitHealed -= HandleUnitHealed;
            unit.OnUnitHealed += HandleUnitHealed;
        }
    }

    private void UnregisterAllUnits()
    {
        if (stateMachine == null)
        {
            return;
        }

        UnregisterUnitList(stateMachine.HeroUnits);
        UnregisterUnitList(stateMachine.EnemyUnits);
    }

    private void UnregisterUnitList(IReadOnlyList<BattleUnit> units)
    {
        if (units == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            unit.OnUnitDamageTaken -= HandleUnitDamageTaken;
            unit.OnUnitHealed -= HandleUnitHealed;
        }
    }

    private void HandleUnitDamageTaken(BattleUnit unit, int damageAmount)
    {
        SpawnFloatingText(unit, damageAmount, false);
    }

    private void HandleUnitHealed(BattleUnit unit, int healAmount)
    {
        SpawnFloatingText(unit, healAmount, true);
    }

    private void SpawnFloatingText(BattleUnit unit, int amount, bool isHeal)
    {
        if (unit == null || damageTextPrefab == null || poolManager == null)
        {
            return;
        }

        Vector3 spawnPos = unit.HitPosition + spawnOffset;
        BattleFloatingDamageText floatingText = poolManager.Spawn<BattleFloatingDamageText>(
            damageTextPrefab,
            spawnPos,
            Quaternion.identity
        );

        if (floatingText != null)
        {
            floatingText.Setup(amount, isHeal);
        }
    }
}

