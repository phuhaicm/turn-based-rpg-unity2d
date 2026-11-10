using System.Collections;
using UnityEngine;

public class DefendAction : ICombatAction
{
    private BattleUnit actor;

    private static readonly WaitForSeconds DefendPoseWait = new WaitForSeconds(0.35f);

    public string ActionName => "Defend";
    public BattleUnit Actor => actor;

    public DefendAction(BattleUnit actor)
    {
        this.actor = actor;
    }

    public DefendAction Configure(BattleUnit newActor)
    {
        actor = newActor;
        return this;
    }

    public IEnumerator ExecuteAction()
    {
        if (actor == null || actor.IsDead)
        {
            yield break;
        }

        // Bật khiên chắn giảm 50% sát thương nhận vào trong 1 vòng
        actor.SetDefending(true);

        // Hồi phục nhẹ 15% MaxMP khi chọn Phòng Thủ
        if (actor.Mana != null && actor.Mana.MaxMP > 0)
        {
            int manaRegen = Mathf.Max(5, Mathf.RoundToInt(actor.Mana.MaxMP * 0.15f));
            actor.Mana.RestoreMana(manaRegen);
        }

        yield return DefendPoseWait;
    }
}
