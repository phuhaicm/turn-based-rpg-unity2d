using System.Collections;

public interface ICombatAction
{
    string ActionName { get; }
    BattleUnit Actor { get; }

    // Mọi hành động chiến đấu đều chạy tuần tự qua Coroutine này (Command Pattern)
    IEnumerator ExecuteAction();
}
