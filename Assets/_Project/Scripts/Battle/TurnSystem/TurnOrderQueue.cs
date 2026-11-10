using System;
using System.Collections.Generic;

public class TurnOrderQueue
{
    private readonly List<BattleUnit> allCombatants = new List<BattleUnit>(8);
    private readonly List<BattleUnit> activeTurnList = new List<BattleUnit>(8);

    private int currentRound;

    // Cache sẵn delegate so sánh để hàm Sort không cấp phát bộ nhớ rác (Zero-GC), tuyệt đối không dùng LINQ OrderBy
    private static readonly Comparison<BattleUnit> SpeedComparison = CompareBySpeedDescending;

    public event Action<int> OnRoundStarted;
    public event Action<IReadOnlyList<BattleUnit>> OnQueueUpdated;

    public int CurrentRound => currentRound;
    public int RemainingTurnsInRound => activeTurnList.Count;
    public IReadOnlyList<BattleUnit> ActiveTurnList => activeTurnList;

    public void Initialize(IReadOnlyList<BattleUnit> heroes, IReadOnlyList<BattleUnit> enemies)
    {
        allCombatants.Clear();
        activeTurnList.Clear();
        currentRound = 0;

        if (heroes != null)
        {
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null)
                {
                    allCombatants.Add(heroes[i]);
                }
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    allCombatants.Add(enemies[i]);
                }
            }
        }
    }

    public void StartNewRound()
    {
        activeTurnList.Clear();

        for (int i = 0; i < allCombatants.Count; i++)
        {
            BattleUnit unit = allCombatants[i];
            if (unit != null && !unit.IsDead)
            {
                activeTurnList.Add(unit);
            }
        }

        // Sắp xếp người có Speed cao hơn đứng trước (index 0)
        activeTurnList.Sort(SpeedComparison);

        currentRound++;
        OnRoundStarted?.Invoke(currentRound);
        OnQueueUpdated?.Invoke(activeTurnList);
    }

    public BattleUnit DequeueNextLivingUnit()
    {
        // Dọn các nhân vật đã bị tiêu diệt khi chưa kịp tới lượt trong hiệp
        for (int i = activeTurnList.Count - 1; i >= 0; i--)
        {
            if (activeTurnList[i] == null || activeTurnList[i].IsDead)
            {
                activeTurnList.RemoveAt(i);
            }
        }

        // Nếu hàng đợi trong hiệp đã hết thì tạo hiệp (Round) mới
        if (activeTurnList.Count == 0)
        {
            StartNewRound();
        }

        if (activeTurnList.Count == 0)
        {
            return null;
        }

        BattleUnit nextUnit = activeTurnList[0];
        activeTurnList.RemoveAt(0);

        OnQueueUpdated?.Invoke(activeTurnList);
        return nextUnit;
    }

    private static int CompareBySpeedDescending(BattleUnit a, BattleUnit b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        int speedCompare = b.Speed.CompareTo(a.Speed);
        if (speedCompare != 0)
        {
            return speedCompare;
        }

        // Nếu bằng Speed thì ưu tiên phe người chơi đi trước
        if (a.IsPlayerUnit != b.IsPlayerUnit)
        {
            return a.IsPlayerUnit ? -1 : 1;
        }

        return 0;
    }
}

