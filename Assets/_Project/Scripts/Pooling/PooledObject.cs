using UnityEngine;

public class PooledObject : HaiMonoBehaviour
{
    [SerializeField]
    private GameObject originPrefab;

    private ObjectPoolManager poolManager;
    private IPoolable[] poolableComponents;
    private bool isInPool;

    public GameObject OriginPrefab => originPrefab;
    public bool IsInPool => isInPool;

    protected override void Awake()
    {
        base.Awake();

        // Gom sẵn danh sách IPoolable 1 lần duy nhất lúc khởi tạo để không gọi GetComponent khi Spawn/Despawn
        poolableComponents = GetComponentsInChildren<IPoolable>(true);
    }

    public void SetupPoolReference(ObjectPoolManager manager, GameObject prefabKey)
    {
        poolManager = manager;
        originPrefab = prefabKey;
    }

    public void NotifySpawned()
    {
        isInPool = false;

        if (poolableComponents == null)
        {
            poolableComponents = GetComponentsInChildren<IPoolable>(true);
        }

        for (int i = 0; i < poolableComponents.Length; i++)
        {
            poolableComponents[i]?.OnSpawnFromPool();
        }
    }

    public void NotifyReturned()
    {
        isInPool = true;

        if (poolableComponents == null)
        {
            return;
        }

        for (int i = 0; i < poolableComponents.Length; i++)
        {
            poolableComponents[i]?.OnReturnToPool();
        }
    }

    public void ReturnToPool()
    {
        if (isInPool)
        {
            return;
        }

        if (poolManager != null)
        {
            poolManager.Despawn(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

