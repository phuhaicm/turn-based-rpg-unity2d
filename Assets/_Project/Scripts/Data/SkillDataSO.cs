using UnityEngine;

public enum SkillTargetType
{
    SingleEnemy = 0,
    AllEnemies = 1,
    SingleAlly = 2,
    AllAllies = 3,
    Self = 4
}

public enum SkillCategory
{
    PhysicalDamage = 0,
    MagicalDamage = 1,
    Heal = 2
}

[CreateAssetMenu(fileName = "Skill_New", menuName = "TurnBasedRPG/Data/Skill Data", order = 1)]
public class SkillDataSO : ScriptableObject
{
    [Header("Skill Identity")]
    [SerializeField]
    private string skillName = "New Skill";

    [SerializeField, TextArea(2, 4)]
    private string description = "Mô tả kỹ năng...";

    [SerializeField]
    private Sprite icon;

    [Header("Combat Settings")]
    [SerializeField, Min(0)]
    private int manaCost = 10;

    [SerializeField]
    private SkillCategory category = SkillCategory.PhysicalDamage;

    [SerializeField]
    private SkillTargetType targetType = SkillTargetType.SingleEnemy;

    [SerializeField, Min(0f)]
    private float damageMultiplier = 1.5f; // Hệ số nhân với ATK của người dùng chiêu

    [SerializeField, Min(0)]
    private int flatBonusPower = 0;

    [Header("VFX & Audio")]
    [SerializeField]
    private GameObject vfxPrefab;

    [SerializeField, Range(0.1f, 3f)]
    private float executionDuration = 0.6f;

    [SerializeField]
    private AudioClip castSound;

    // Dùng Property chỉ đọc (=>) để bảo vệ dữ liệu gốc trong file Asset không bị sửa nhầm lúc Runtime
    public string SkillName => skillName;
    public string Description => description;
    public Sprite Icon => icon;
    public int ManaCost => manaCost;
    public SkillCategory Category => category;
    public SkillTargetType TargetType => targetType;
    public float DamageMultiplier => damageMultiplier;
    public int FlatBonusPower => flatBonusPower;
    public GameObject VfxPrefab => vfxPrefab;
    public float ExecutionDuration => executionDuration;
    public AudioClip CastSound => castSound;

    public int CalculateOutputPower(int casterAttack)
    {
        int scaledPower = Mathf.RoundToInt(casterAttack * damageMultiplier) + flatBonusPower;
        return Mathf.Max(1, scaledPower); // Luôn đảm bảo tác động tối thiểu là 1
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            skillName = name;
        }

        manaCost = Mathf.Max(0, manaCost);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        flatBonusPower = Mathf.Max(0, flatBonusPower);
    }
#endif
}
