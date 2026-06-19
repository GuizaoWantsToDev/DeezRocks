using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int amount;
    }

    public static PoolManager Instance { get; private set; }

    public List<Pool> pools = new List<Pool>();
    public Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.amount; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnPoolObject(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("A pool with the tag: " + tag + " doesn't exist");
            return null;
        }

        GameObject spawnObject = poolDictionary[tag].Dequeue();

        spawnObject.SetActive(true);
        spawnObject.transform.position = position;
        spawnObject.transform.rotation = rotation;

        if (spawnObject.TryGetComponent<IPooledObject>(out IPooledObject pooledObj))
        {
            pooledObj.OnObjectSpawn();
        }

        poolDictionary[tag].Enqueue(spawnObject);

        return spawnObject;
    }
}