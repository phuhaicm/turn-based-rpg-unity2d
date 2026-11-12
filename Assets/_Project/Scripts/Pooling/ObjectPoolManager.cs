using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PoolPrewarmItem
{
    public GameObject prefab;
    [Min(1)]
    public int preloadCount;
}

public class ObjectPoolManager : HaiMonoBehaviour
{
    private static ObjectPoolManager instance;
    public static ObjectPoolManager Instance => instance;

    [Header("Prewarm Pool Settings")]
    [SerializeField]
    private List<PoolPrewarmItem> prewarmItems = new List<PoolPrewarmItem>();

    // Cấu trúc Dictionary + Queue giúp truy xuất và tái sử dụng Object với độ phức tạp O(1)
    private readonly Dictionary<GameObject, Queue<PooledObject>> poolDictionary =
        new Dictionary<GameObject, Queue<PooledObject>>();

    protected override void Awake()
    {
        base.Awake();

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        PrewarmAllPools();
    }

    public void SetPrewarmItem(GameObject prefab, int count)
    {
        prewarmItems.Clear();
        AddOrUpdatePrewarmItem(prefab, count);
    }

    public void AddOrUpdatePrewarmItem(GameObject prefab, int count)
    {
        if (prefab == null)
        {
            return;
        }

        for (int i = 0; i < prewarmItems.Count; i++)
        {
            if (prewarmItems[i].prefab == prefab)
            {
                prewarmItems[i] = new PoolPrewarmItem
                {
                    prefab = prefab,
                    preloadCount = Mathf.Max(1, count)
                };
                return;
            }
        }

        prewarmItems.Add(new PoolPrewarmItem
        {
            prefab = prefab,
            preloadCount = Mathf.Max(1, count)
        });
    }

    private void PrewarmAllPools()
    {
        for (int i = 0; i < prewarmItems.Count; i++)
        {
            PoolPrewarmItem item = prewarmItems[i];
            if (item.prefab == null)
            {
                continue;
            }

            EnsurePoolCreated(item.prefab, item.preloadCount);
        }
    }

    public void EnsurePoolCreated(GameObject prefab, int preloadCount)
    {
        if (prefab == null)
        {
            return;
        }

        if (!poolDictionary.TryGetValue(prefab, out Queue<PooledObject> queue))
        {
            queue = new Queue<PooledObject>(preloadCount);
            poolDictionary.Add(prefab, queue);
        }

        while (queue.Count < preloadCount)
        {
            PooledObject newObj = CreateNewPooledInstance(prefab);
            newObj.NotifyReturned();
            newObj.gameObject.SetActive(false);
            queue.Enqueue(newObj);
        }
    }

    public PooledObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        if (!poolDictionary.TryGetValue(prefab, out Queue<PooledObject> queue))
        {
            queue = new Queue<PooledObject>(4);
            poolDictionary.Add(prefab, queue);
        }

        PooledObject pooledObj = null;

        while (queue.Count > 0 && pooledObj == null)
        {
            pooledObj = queue.Dequeue();
        }

        // Nếu hàng đợi đã cạn thì mới tạo thêm phần tử mới
        if (pooledObj == null)
        {
            pooledObj = CreateNewPooledInstance(prefab);
        }

        Transform objTransform = pooledObj.transform;
        objTransform.SetPositionAndRotation(position, rotation);

        pooledObj.gameObject.SetActive(true);
        pooledObj.NotifySpawned();

        return pooledObj;
    }

    public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        PooledObject pooledObj = Spawn(prefab, position, rotation);
        if (pooledObj == null)
        {
            return null;
        }

        return pooledObj.GetComponent<T>();
    }

    public void Despawn(PooledObject pooledObj)
    {
        if (pooledObj == null || pooledObj.IsInPool)
        {
            return;
        }

        GameObject prefabKey = pooledObj.OriginPrefab;
        if (prefabKey == null)
        {
            pooledObj.gameObject.SetActive(false);
            return;
        }

        if (!poolDictionary.TryGetValue(prefabKey, out Queue<PooledObject> queue))
        {
            queue = new Queue<PooledObject>(4);
            poolDictionary.Add(prefabKey, queue);
        }

        pooledObj.NotifyReturned();
        pooledObj.gameObject.SetActive(false);
        pooledObj.transform.SetParent(transform, false);

        queue.Enqueue(pooledObj);
    }

    private PooledObject CreateNewPooledInstance(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.name = prefab.name;

        PooledObject pooled = obj.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = obj.AddComponent<PooledObject>();
        }

        pooled.SetupPoolReference(this, prefab);
        return pooled;
    }
}

