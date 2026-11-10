public interface ITurnCombatant
{
    string UnitName { get; }
    int Speed { get; } // Dùng để sắp xếp thứ tự ai đánh trước trong TurnOrderQueue
    bool IsPlayerUnit { get; } // Phân biệt phe người chơi (true) hay phe quái vật (false)
    bool IsDead { get; } // Bỏ qua lượt nếu nhân vật đã gục ngã
}
