using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleFeedbackManager : HaiMonoBehaviour
{
    [Header("System References")]
    [SerializeField]
    private BattleStateMachine battleStateMachine;

    [SerializeField]
    private ObjectPoolManager poolManager;

    [SerializeField]
    private BattleCameraShake cameraShake;

    [Header("Pooled VFX Prefabs")]
    [SerializeField]
    private GameObject slashHitVfxPrefab;

    [SerializeField]
    private GameObject healAuraVfxPrefab;

    [SerializeField]
    private GameObject shieldGuardVfxPrefab;

    private readonly List<BattleUnit> subscribedUnits = new List<BattleUnit>(8);

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (battleStateMachine == null)
        {
            battleStateMachine = FindFirstObjectByType<BattleStateMachine>();
        }

        if (poolManager == null)
        {
            poolManager = FindFirstObjectByType<ObjectPoolManager>();
        }

        if (cameraShake == null)
        {
            cameraShake = FindFirstObjectByType<BattleCameraShake>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeFeedbackEvents();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeFeedbackEvents();
    }

    protected override void Start()
    {
        base.Start();
        SubscribeFeedbackEvents();
    }

    public void ConfigureFeedbackManager(
        BattleStateMachine fsm,
        ObjectPoolManager pool,
        BattleCameraShake shake,
        GameObject slashPrefab,
        GameObject healPrefab,
        GameObject shieldPrefab)
    {
        battleStateMachine = fsm;
        poolManager = pool;
        cameraShake = shake;
        slashHitVfxPrefab = slashPrefab;
        healAuraVfxPrefab = healPrefab;
        shieldGuardVfxPrefab = shieldPrefab;
    }

    private void SubscribeFeedbackEvents()
    {
        UnsubscribeFeedbackEvents();

        if (battleStateMachine == null)
        {
            return;
        }

        RegisterParty(battleStateMachine.HeroUnits);
        RegisterParty(battleStateMachine.EnemyUnits);
    }

    private void RegisterParty(IReadOnlyList<BattleUnit> party)
    {
        if (party == null)
        {
            return;
        }

        for (int i = 0; i < party.Count; i++)
        {
            BattleUnit unit = party[i];
            if (unit == null || subscribedUnits.Contains(unit))
            {
                continue;
            }

            unit.OnUnitDamageTaken -= HandleUnitDamageTaken;
            unit.OnUnitDamageTaken += HandleUnitDamageTaken;

            unit.OnUnitHealed -= HandleUnitHealed;
            unit.OnUnitHealed += HandleUnitHealed;

            unit.OnDefendStateChanged -= HandleDefendStateChanged;
            unit.OnDefendStateChanged += HandleDefendStateChanged;

            subscribedUnits.Add(unit);
        }
    }

    private void UnsubscribeFeedbackEvents()
    {
        for (int i = 0; i < subscribedUnits.Count; i++)
        {
            BattleUnit unit = subscribedUnits[i];
            if (unit != null)
            {
                unit.OnUnitDamageTaken -= HandleUnitDamageTaken;
                unit.OnUnitHealed -= HandleUnitHealed;
                unit.OnDefendStateChanged -= HandleDefendStateChanged;
            }
        }

        subscribedUnits.Clear();
    }

    private void HandleUnitDamageTaken(BattleUnit unit, int damage)
    {
        if (unit == null)
        {
            return;
        }

        SpawnVFX(slashHitVfxPrefab, unit.HitPosition);

        if (cameraShake != null)
        {
            // Sát thương lớn từ 35 trở lên sẽ rung màn hình mạnh hơn để tạo lực va chạm (Game Feel)
            if (damage >= 35)
            {
                cameraShake.Shake(0.22f, 0.25f);
            }
            else
            {
                cameraShake.Shake(0.14f, 0.14f);
            }
        }
    }

    private void HandleUnitHealed(BattleUnit unit, int healAmount)
    {
        if (unit == null)
        {
            return;
        }

        SpawnVFX(healAuraVfxPrefab, unit.HitPosition);
    }

    private void HandleDefendStateChanged(BattleUnit unit, bool isDefending)
    {
        if (unit == null || !isDefending)
        {
            return;
        }

        SpawnVFX(shieldGuardVfxPrefab, unit.HitPosition);
    }

    private void SpawnVFX(GameObject vfxPrefab, Vector3 position)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        ObjectPoolManager activePool = poolManager != null ? poolManager : ObjectPoolManager.Instance;
        if (activePool != null)
        {
            activePool.Spawn(vfxPrefab, position, Quaternion.identity);
        }
    }
}

