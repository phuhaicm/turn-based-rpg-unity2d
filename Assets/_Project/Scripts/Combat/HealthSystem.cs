using System;
using UnityEngine;

public class HealthSystem : HaiMonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField, Min(1)]
    protected int maxHP = 100;

    [SerializeField, Min(0)]
    protected int currentHP = 100;

    protected bool isDead;

    public event Action<int> OnDamageTaken;
    public event Action<int> OnHealed;
    public event Action<int, int> OnHealthChanged; // Hàm con không gọi trực tiếp Invoke được nên dùng qua NotifyHealthChanged
    public event Action OnDied;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public float HealthNormalized =>
        maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f; // Tính tỉ lệ máu từ 0.0 đến 1.0, toán tử 3 ngôi chống lỗi chia cho 0
    public bool IsDead => isDead;

    public virtual void Initialize(int initialMaxHP)
    {
        maxHP = Mathf.Max(1, initialMaxHP);
        currentHP = maxHP;
        isDead = false;

        NotifyHealthChanged(); // Phát sự kiện cập nhật thanh máu UI ngay khi khởi tạo
    }

    public virtual int TakeDamage(int damageAmount)
    {
        if (isDead)
        {
            return 0;
        }

        if (damageAmount <= 0)
        {
            return 0;
        }

        int previousHP = currentHP;

        currentHP = Mathf.Clamp(
            currentHP - damageAmount,
            0,
            maxHP
        );

        int appliedDamage = previousHP - currentHP;

        if (appliedDamage <= 0)
        {
            return 0;
        }

        // Máu đã thật sự giảm nên mới phát event
        OnDamageTaken?.Invoke(appliedDamage);
        NotifyHealthChanged();

        if (currentHP <= 0)
        {
            Die();
        }

        return appliedDamage;
    }

    public virtual int Heal(int healAmount)
    {
        if (isDead)
        {
            return 0;
        }

        if (healAmount <= 0)
        {
            return 0;
        }

        int previousHP = currentHP;

        currentHP = Mathf.Clamp(
            currentHP + healAmount,
            0,
            maxHP
        );

        int actualHealed = currentHP - previousHP;

        if (actualHealed <= 0)
        {
            return 0;
        }

        OnHealed?.Invoke(actualHealed);
        NotifyHealthChanged();

        return actualHealed;
    }

    protected void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    protected virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        OnDied?.Invoke();
    }
}
