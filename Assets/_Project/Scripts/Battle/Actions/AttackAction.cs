using System.Collections;
using UnityEngine;

public class AttackAction : ICombatAction
{
    private BattleUnit actor;
    private IDamageable target;
    private readonly float dashSpeed;
    private readonly float meleeStopDistance;

    // Cache sẵn WaitForSeconds để không phải new mới mỗi lượt đánh (Zero-GC)
    private static readonly WaitForSeconds ImpactWait = new WaitForSeconds(0.18f);
    private static readonly WaitForSeconds PostActionWait = new WaitForSeconds(0.12f);

    public string ActionName => "Attack";
    public BattleUnit Actor => actor;
    public IDamageable Target => target;

    public AttackAction(BattleUnit actor, IDamageable target, float dashSpeed = 18f, float meleeStopDistance = 1.2f)
    {
        this.actor = actor;
        this.target = target;
        this.dashSpeed = dashSpeed;
        this.meleeStopDistance = meleeStopDistance;
    }

    public AttackAction Configure(BattleUnit newActor, IDamageable newTarget)
    {
        actor = newActor;
        target = newTarget;
        return this; // Tái sử dụng lại đối tượng AttackAction cũ thay vì new mới
    }

    public IEnumerator ExecuteAction()
    {
        if (actor == null || actor.IsDead || target == null || target.IsDead)
        {
            yield break;
        }

        Vector3 startPosition = actor.OriginalPosition;
        Vector3 targetPosition = target.HitPosition;
        targetPosition.y = startPosition.y;

        Vector3 direction = (targetPosition - startPosition).normalized;
        Vector3 strikePosition = targetPosition - direction * meleeStopDistance;

        // Lao tới trước mặt mục tiêu
        yield return MoveUnitRoutine(actor.transform, strikePosition);

        actor.PlayAttackAnimation();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.AttackSlash);
        }

        // Trừ máu thông qua interface IDamageable
        int rawDamage = actor.CurrentAttack;
        target.TakeDamage(rawDamage);

        yield return ImpactWait;

        // Lùi về vị trí đội hình ban đầu
        yield return MoveUnitRoutine(actor.transform, startPosition);

        yield return PostActionWait;
    }

    private IEnumerator MoveUnitRoutine(Transform unitTransform, Vector3 destination)
    {
        while ((unitTransform.position - destination).sqrMagnitude > 0.002f)
        {
            unitTransform.position = Vector3.MoveTowards(
                unitTransform.position,
                destination,
                dashSpeed * Time.deltaTime
            );
            yield return null;
        }

        unitTransform.position = destination;
    }
}
