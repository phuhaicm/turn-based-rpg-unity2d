using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    None = 0,
    AttackSlash = 1,
    MagicFireball = 2,
    HealSpell = 3,
    ShieldDefend = 4,
    TakeDamageHit = 5,
    UnitDeath = 6,
    UIButtonClick = 7,
    BattleVictory = 8,
    BattleDefeat = 9
}

[Serializable]
public struct SoundEntry
{
    public SoundType soundType;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume;
}

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "TurnBasedRPG/Audio/Sound Library", order = 3)]
public class SoundLibrary : ScriptableObject
{
    [Header("Background Music")]
    [SerializeField]
    private AudioClip battleBgmClip;

    [SerializeField, Range(0f, 1f)]
    private float bgmVolume = 0.28f;

    [Header("Sound Effects (SFX)")]
    [SerializeField]
    private List<SoundEntry> soundEntries = new List<SoundEntry>();

    // Dùng mảng ánh xạ trực tiếp theo chỉ số Enum (int) để tra cứu O(1) và tránh Boxing Enum (Zero-GC)
    [NonSerialized]
    private SoundEntry[] fastLookupTable;

    [NonSerialized]
    private bool isLookupInitialized;

    public AudioClip BattleBgmClip => battleBgmClip;
    public float BgmVolume => bgmVolume;
    public IReadOnlyList<SoundEntry> SoundEntries => soundEntries;

    public void InitializeLookup()
    {
        int maxEnumIndex = Enum.GetValues(typeof(SoundType)).Length;
        fastLookupTable = new SoundEntry[maxEnumIndex];

        for (int i = 0; i < soundEntries.Count; i++)
        {
            SoundEntry entry = soundEntries[i];
            int index = (int)entry.soundType;
            if (index >= 0 && index < maxEnumIndex)
            {
                fastLookupTable[index] = entry;
            }
        }

        isLookupInitialized = true;
    }

    public bool TryGetSound(SoundType type, out AudioClip clip, out float volume)
    {
        if (!isLookupInitialized || fastLookupTable == null)
        {
            InitializeLookup();
        }

        int index = (int)type;
        if (index > 0 && index < fastLookupTable.Length)
        {
            SoundEntry entry = fastLookupTable[index];
            if (entry.clip != null)
            {
                clip = entry.clip;
                volume = entry.volume > 0f ? entry.volume : 0.85f;
                return true;
            }
        }

        clip = null;
        volume = 0f;
        return false;
    }
}

