using System;
using UnityEngine;

public class ManaSystem : HaiMonoBehaviour
{
    [Header("Mana Settings")]
    [SerializeField, Min(0)]
    protected int maxMP = 50;

    [SerializeField, Min(0)]
    protected int currentMP = 50;

    public event Action<int, int> OnManaChanged;

    public int CurrentMP => currentMP;
    public int MaxMP => maxMP;
    public float ManaNormalized =>
        maxMP > 0 ? Mathf.Clamp01((float)currentMP / maxMP) : 0f;

    public virtual void Initialize(int initialMaxMP)
    {
        maxMP = Mathf.Max(0, initialMaxMP);
        currentMP = maxMP;

        NotifyManaChanged();
    }

    public bool HasEnoughMana(int manaCost)
    {
        return currentMP >= manaCost;
    }

    public virtual bool TrySpendMana(int manaCost)
    {
        if (manaCost <= 0)
        {
            return true;
        }

        if (!HasEnoughMana(manaCost))
        {
            return false;
        }

        currentMP = Mathf.Clamp(
            currentMP - manaCost,
            0,
            maxMP
        );

        NotifyManaChanged();
        return true;
    }

    public virtual int RestoreMana(int amount)
    {
        if (amount <= 0 || maxMP <= 0)
        {
            return 0;
        }

        int previousMP = currentMP;

        currentMP = Mathf.Clamp(
            currentMP + amount,
            0,
            maxMP
        );

        int actualRestored = currentMP - previousMP;

        if (actualRestored > 0)
        {
            NotifyManaChanged();
        }

        return actualRestored;
    }

    protected void NotifyManaChanged()
    {
        OnManaChanged?.Invoke(currentMP, maxMP);
    }
}
