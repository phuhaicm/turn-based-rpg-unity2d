using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : HaiMonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance => instance;

    [Header("Audio References")]
    [SerializeField]
    private SoundLibrary soundLibrary;

    [SerializeField]
    private BattleStateMachine battleStateMachine;

    [SerializeField]
    private AudioSource bgmSource;

    [SerializeField]
    private List<AudioSource> sfxSources = new List<AudioSource>();

    private readonly List<BattleUnit> subscribedUnits = new List<BattleUnit>(8);
    private int currentSfxChannelIndex;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (battleStateMachine == null)
        {
            battleStateMachine = FindFirstObjectByType<BattleStateMachine>();
        }

        if (bgmSource == null)
        {
            Transform bgmTransform = transform.Find("BGM_Channel");
            if (bgmTransform != null)
            {
                bgmSource = bgmTransform.GetComponent<AudioSource>();
            }
        }

        if (sfxSources.Count == 0)
        {
            Transform sfxRoot = transform.Find("SFX_Channels");
            if (sfxRoot != null)
            {
                sfxRoot.GetComponentsInChildren(true, sfxSources);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (soundLibrary != null)
        {
            soundLibrary.InitializeLookup();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeBattleEvents();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeBattleEvents();
    }

    protected override void Start()
    {
        base.Start();

        SubscribeBattleEvents();
        PlayBattleBGM();
    }

    public void ConfigureAudioManager(
        SoundLibrary library,
        BattleStateMachine fsm,
        AudioSource bgm,
        List<AudioSource> sfxList)
    {
        soundLibrary = library;
        battleStateMachine = fsm;
        bgmSource = bgm;
        sfxSources = sfxList;
    }

    public void PlayBattleBGM()
    {
        if (bgmSource == null || soundLibrary == null || soundLibrary.BattleBgmClip == null)
        {
            return;
        }

        bgmSource.clip = soundLibrary.BattleBgmClip;
        bgmSource.volume = soundLibrary.BgmVolume;
        bgmSource.loop = true;

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void PlaySFX(SoundType soundType)
    {
        if (soundLibrary == null || soundType == SoundType.None)
        {
            return;
        }

        if (soundLibrary.TryGetSound(soundType, out AudioClip clip, out float volume))
        {
            PlayClipOnRingBuffer(clip, volume);
        }
    }

    public void PlayCustomClip(AudioClip clip, float volume = 0.85f)
    {
        if (clip == null)
        {
            return;
        }

        PlayClipOnRingBuffer(clip, volume);
    }

    public void PlayUIClick()
    {
        PlaySFX(SoundType.UIButtonClick);
    }

    private void PlayClipOnRingBuffer(AudioClip clip, float volume)
    {
        if (clip == null || sfxSources.Count == 0)
        {
            return;
        }

        // Xoay vòng qua các kênh AudioSource cố định (Ring Buffer) để phát nhiều âm thanh chồng lên nhau mà không sinh GC
        AudioSource channel = sfxSources[currentSfxChannelIndex];
        currentSfxChannelIndex = (currentSfxChannelIndex + 1) % sfxSources.Count;

        if (channel != null)
        {
            channel.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    private void SubscribeBattleEvents()
    {
        UnsubscribeBattleEvents();

        if (battleStateMachine == null)
        {
            return;
        }

        battleStateMachine.OnBattleEnded -= HandleBattleEnded;
        battleStateMachine.OnBattleEnded += HandleBattleEnded;

        RegisterPartyList(battleStateMachine.HeroUnits);
        RegisterPartyList(battleStateMachine.EnemyUnits);
    }

    private void RegisterPartyList(IReadOnlyList<BattleUnit> party)
    {
        if (party == null)
        {
            return;
        }

        for (int i = 0; i < party.Count; i++)
        {
            BattleUnit unit = party[i];
            if (unit == null || subscribedUnits.Contains(unit))
            {
                continue;
            }

            unit.OnUnitDamageTaken -= HandleUnitDamageTaken;
            unit.OnUnitDamageTaken += HandleUnitDamageTaken;

            unit.OnUnitHealed -= HandleUnitHealed;
            unit.OnUnitHealed += HandleUnitHealed;

            unit.OnDefendStateChanged -= HandleDefendStateChanged;
            unit.OnDefendStateChanged += HandleDefendStateChanged;

            unit.OnUnitDied -= HandleUnitDied;
            unit.OnUnitDied += HandleUnitDied;

            subscribedUnits.Add(unit);
        }
    }

    private void UnsubscribeBattleEvents()
    {
        if (battleStateMachine != null)
        {
            battleStateMachine.OnBattleEnded -= HandleBattleEnded;
        }

        for (int i = 0; i < subscribedUnits.Count; i++)
        {
            BattleUnit unit = subscribedUnits[i];
            if (unit != null)
            {
                unit.OnUnitDamageTaken -= HandleUnitDamageTaken;
                unit.OnUnitHealed -= HandleUnitHealed;
                unit.OnDefendStateChanged -= HandleDefendStateChanged;
                unit.OnUnitDied -= HandleUnitDied;
            }
        }

        subscribedUnits.Clear();
    }

    private void HandleUnitDamageTaken(BattleUnit unit, int damage)
    {
        PlaySFX(SoundType.TakeDamageHit);
    }

    private void HandleUnitHealed(BattleUnit unit, int healAmount)
    {
        PlaySFX(SoundType.HealSpell);
    }

    private void HandleDefendStateChanged(BattleUnit unit, bool isDefending)
    {
        if (isDefending)
        {
            PlaySFX(SoundType.ShieldDefend);
        }
    }

    private void HandleUnitDied(BattleUnit unit)
    {
        PlaySFX(SoundType.UnitDeath);
    }

    private void HandleBattleEnded(BattleOutcome outcome)
    {
        if (outcome == BattleOutcome.Victory)
        {
            PlaySFX(SoundType.BattleVictory);
        }
        else if (outcome == BattleOutcome.Defeat)
        {
            PlaySFX(SoundType.BattleDefeat);
        }
    }
}

