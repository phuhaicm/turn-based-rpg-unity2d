using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillAction : ICombatAction
{
    private BattleUnit actor;
    private SkillDataSO skillData;

    // Khởi tạo sẵn danh sách mục tiêu tối đa 8 đơn vị để tái sử dụng, tránh new List mỗi lần tung chiêu (Zero-GC)
    private readonly List<IDamageable> targets = new List<IDamageable>(8);
    private readonly float moveSpeed;
    private readonly float castOffsetDistance;

    private static readonly WaitForSeconds CastPrepareWait = new WaitForSeconds(0.25f);
    private static readonly WaitForSeconds ImpactWait = new WaitForSeconds(0.35f);
    private static readonly WaitForSeconds PostActionWait = new WaitForSeconds(0.15f);

    public string ActionName => skillData != null ? skillData.SkillName : "Skill";
    public BattleUnit Actor => actor;
    public SkillDataSO SkillData => skillData;
    public IReadOnlyList<IDamageable> Targets => targets;

    public SkillAction(float moveSpeed = 16f, float castOffsetDistance = 1.4f)
    {
        this.moveSpeed = moveSpeed;
        this.castOffsetDistance = castOffsetDistance;
    }

    public SkillAction ConfigureSingleTarget(BattleUnit newActor, SkillDataSO newSkill, IDamageable singleTarget)
    {
        actor = newActor;
        skillData = newSkill;
        targets.Clear();

        if (singleTarget != null && !singleTarget.IsDead)
        {
            targets.Add(singleTarget);
        }

        return this;
    }

    public SkillAction ConfigureMultiTarget(BattleUnit newActor, SkillDataSO newSkill, IReadOnlyList<BattleUnit> groupTargets)
    {
        actor = newActor;
        skillData = newSkill;
        targets.Clear();

        if (groupTargets != null)
        {
            for (int i = 0; i < groupTargets.Count; i++)
            {
                BattleUnit unit = groupTargets[i];
                if (unit != null && !unit.IsDead)
                {
                    targets.Add(unit);
                }
            }
        }

        return this;
    }

    public IEnumerator ExecuteAction()
    {
        if (actor == null || actor.IsDead || skillData == null || targets.Count == 0)
        {
            yield break;
        }

        // Kiểm tra và trừ Mana qua Try-Pattern, nếu không đủ MP thì hủy chiêu
        if (actor.Mana != null && !actor.Mana.TrySpendMana(skillData.ManaCost))
        {
            yield break;
        }

        Vector3 startPosition = actor.OriginalPosition;
        Vector3 castPosition = CalculateCastPosition(startPosition);

        // Bước 1: Di chuyển tới vị trí tung chiêu
        yield return MoveUnitRoutine(actor.transform, castPosition);

        actor.PlayAttackAnimation();
        PlaySkillAudio();

        yield return CastPrepareWait;

        // Bước 2: Tính toán lượng sát thương hoặc hồi máu từ SkillDataSO
        int outputPower = skillData.CalculateOutputPower(actor.CurrentAttack);

        // Bước 3: Tác động lên toàn bộ mục tiêu trong danh sách (hỗ trợ cả đánh đơn và đánh lan AOE)
        for (int i = 0; i < targets.Count; i++)
        {
            IDamageable target = targets[i];
            if (target == null || target.IsDead)
            {
                continue;
            }

            if (skillData.VfxPrefab != null && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Spawn(skillData.VfxPrefab, target.HitPosition, Quaternion.identity);
            }

            if (skillData.Category == SkillCategory.Heal)
            {
                target.Heal(outputPower);
            }
            else
            {
                target.TakeDamage(outputPower);
            }
        }

        yield return ImpactWait;

        // Bước 4: Quay về vị trí đội hình ban đầu
        yield return MoveUnitRoutine(actor.transform, startPosition);
        yield return PostActionWait;
    }

    private void PlaySkillAudio()
    {
        if (AudioManager.Instance == null || skillData == null)
        {
            return;
        }

        if (skillData.CastSound != null)
        {
            AudioManager.Instance.PlayCustomClip(skillData.CastSound);
            return;
        }

        if (skillData.Category == SkillCategory.MagicalDamage)
        {
            AudioManager.Instance.PlaySFX(SoundType.MagicFireball);
        }
        else if (skillData.Category == SkillCategory.PhysicalDamage)
        {
            AudioManager.Instance.PlaySFX(SoundType.AttackSlash);
        }
    }

    private Vector3 CalculateCastPosition(Vector3 startPosition)
    {
        // Chiêu đánh thường vật lý đơn mục tiêu thì lao tới sát mục tiêu
        if (skillData.Category == SkillCategory.PhysicalDamage && skillData.TargetType == SkillTargetType.SingleEnemy && targets.Count == 1)
        {
            Vector3 targetPos = targets[0].HitPosition;
            targetPos.y = startPosition.y;
            Vector3 dir = (targetPos - startPosition).normalized;
            return targetPos - dir * castOffsetDistance;
        }

        // Chiêu phép AOE hoặc Hồi máu thì bước nhẹ lên phía trước 1 đoạn để niệm chú
        float stepDirection = actor.IsPlayerUnit ? 1f : -1f;
        return startPosition + new Vector3(stepDirection * 0.85f, 0f, 0f);
    }

    private IEnumerator MoveUnitRoutine(Transform unitTransform, Vector3 destination)
    {
        while ((unitTransform.position - destination).sqrMagnitude > 0.002f)
        {
            unitTransform.position = Vector3.MoveTowards(
                unitTransform.position,
                destination,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        unitTransform.position = destination;
    }
}

