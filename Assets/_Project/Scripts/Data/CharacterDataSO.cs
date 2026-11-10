using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Char_New", menuName = "TurnBasedRPG/Data/Character Data", order = 0)]
public class CharacterDataSO : ScriptableObject
{
    [Header("Character Identity")]
    [SerializeField]
    private string characterName = "New Character";

    [SerializeField]
    private bool isPlayerHero = true;

    [SerializeField]
    private Sprite characterSprite;

    [SerializeField]
    private Sprite portraitIcon;

    [SerializeField]
    private RuntimeAnimatorController animatorController;

    [Header("Base Stats")]
    [SerializeField, Min(1)]
    private int maxHP = 100;

    [SerializeField, Min(0)]
    private int maxMP = 50;

    [SerializeField, Min(1)]
    private int baseATK = 15;

    [SerializeField, Min(0)]
    private int baseDEF = 5;

    [SerializeField, Min(1)]
    private int speed = 10;

    [Header("Skills")]
    [SerializeField]
    private List<SkillDataSO> skills = new List<SkillDataSO>();

    public string CharacterName => characterName;
    public bool IsPlayerHero => isPlayerHero;
    public Sprite CharacterSprite => characterSprite;
    public Sprite PortraitIcon => portraitIcon != null ? portraitIcon : characterSprite;
    public RuntimeAnimatorController AnimatorController => animatorController;

    public int MaxHP => maxHP;
    public int MaxMP => maxMP;
    public int BaseATK => baseATK;
    public int BaseDEF => baseDEF;
    public int Speed => speed;

    // Trả về IReadOnlyList để bên ngoài chỉ đọc được danh sách mà không thể Add/Clear làm hỏng file Asset, đồng thời không tốn GC
    public IReadOnlyList<SkillDataSO> Skills => skills;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            characterName = name;
        }

        maxHP = Mathf.Max(1, maxHP);
        maxMP = Mathf.Max(0, maxMP);
        baseATK = Mathf.Max(1, baseATK);
        baseDEF = Mathf.Max(0, baseDEF);
        speed = Mathf.Max(1, speed);
    }
#endif
}
