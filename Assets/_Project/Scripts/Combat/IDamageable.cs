using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }

    // Lấy tọa độ nổ hiệu ứng VFX và Damage Text mà không cần ép kiểu hay gọi GetComponent
    Vector3 HitPosition { get; }

    int TakeDamage(int rawDamage);
    int Heal(int healAmount);
}
