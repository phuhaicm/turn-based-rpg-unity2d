using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PooledObject))]
public class PooledParticleVFX : HaiMonoBehaviour, IPoolable
{
    [Header("VFX Settings")]
    [SerializeField]
    private ParticleSystem mainParticleSystem;

    [SerializeField]
    private PooledObject pooledObject;

    [SerializeField, Min(0.1f)]
    private float lifeTime = 0.85f;

    private Coroutine despawnCoroutine;
    private WaitForSeconds cachedLifeTimeWait;
    private float cachedLifeTimeValue = -1f;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        if (mainParticleSystem == null)
        {
            mainParticleSystem = GetComponent<ParticleSystem>();
            if (mainParticleSystem == null)
            {
                mainParticleSystem = GetComponentInChildren<ParticleSystem>(true);
            }
        }

        if (pooledObject == null)
        {
            pooledObject = GetComponent<PooledObject>();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        RefreshWaitCache();
    }

    public void ConfigureVFX(ParticleSystem particle, PooledObject pooled, float duration)
    {
        mainParticleSystem = particle;
        pooledObject = pooled;
        lifeTime = Mathf.Max(0.1f, duration);
        RefreshWaitCache();
    }

    private void RefreshWaitCache()
    {
        // Chỉ khởi tạo WaitForSeconds khi thời gian sống thay đổi để tránh cấp phát rác (Zero-GC)
        if (cachedLifeTimeWait == null || Mathf.Abs(cachedLifeTimeValue - lifeTime) > 0.001f)
        {
            cachedLifeTimeValue = lifeTime;
            cachedLifeTimeWait = new WaitForSeconds(lifeTime);
        }
    }

    public void OnSpawnFromPool()
    {
        RefreshWaitCache();

        if (mainParticleSystem != null)
        {
            mainParticleSystem.Clear(true);
            mainParticleSystem.Play(true);
        }

        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
        }

        despawnCoroutine = StartCoroutine(AutoDespawnRoutine());
    }

    public void OnReturnToPool()
    {
        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
            despawnCoroutine = null;
        }

        if (mainParticleSystem != null)
        {
            mainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator AutoDespawnRoutine()
    {
        yield return cachedLifeTimeWait;

        despawnCoroutine = null;
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

