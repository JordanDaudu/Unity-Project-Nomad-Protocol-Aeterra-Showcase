using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple generic prefab-based object pool.
/// 
/// Responsibilities:
/// - Keep a Queue per prefab type
/// - Provide pooled instances via <see cref="GetObject"/>
/// - Return objects via <see cref="ReturnObjectToPool"/> (supports delayed return)
/// 
/// Notes:
/// - Pooled instances receive a <see cref="PooledObject"/> component at creation time.
///   That component stores the original prefab key so the pool knows which queue to return to.
/// - This pool is scene-persistent (DontDestroyOnLoad) so pooled objects can be reused across scenes.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    #region Inspector

    [SerializeField] private int poolSize = 10;

    [Header("To Initialize")]
    [SerializeField] private GameObject weaponPickup;
    [SerializeField] private GameObject ammoPickup;

    #endregion

    #region Runtime

    // Key: prefab reference. Value: pooled instances for that prefab.
    private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the pool across scenes
        }
        else
            Destroy(gameObject); // Ensure only one instance exists
    }

    private void Start()
    {
        // Optional pre-warm for common prefabs.
        initializeNewPool(weaponPickup);
        initializeNewPool(ammoPickup);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets a pooled instance for the requested prefab.
    /// Creates a new pool on demand if needed.
    /// </summary>
    public GameObject GetObject(GameObject prefab)
    {
        if (poolDictionary.ContainsKey(prefab) == false)
            initializeNewPool(prefab);

        if (poolDictionary[prefab].Count == 0)
        {
            // Pool exhausted: we create a new instance to avoid gameplay stalls.
            // If this happens often, consider increasing poolSize for that prefab.
            Debug.LogWarning("Pool for " + prefab.name + " is empty! Consider increasing the pool size.");
            CreateNewObject(prefab);
        }

        GameObject objectToGet = poolDictionary[prefab].Dequeue();
        objectToGet.SetActive(true); // Activate the object before returning it
        objectToGet.transform.SetParent(null); // Detach from pool parent for normal world usage.

        if (objectToGet.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnSpawnedFromPool(); // Let the object reset itself if it implements IPoolable

        return objectToGet;
    }

    /// <summary>
    /// Returns an object back to the pool.
    /// Delay is useful for FX/particles that should finish playing.
    /// </summary>
    public void ReturnObjectToPool(GameObject objectToReturn, float delay = .001f)
    {
        StartCoroutine(DelayReturn(objectToReturn, delay));
    }

    #endregion

    #region Internal Logic

    private IEnumerator DelayReturn(GameObject objectToReturn, float delay)
    {
        yield return new WaitForSeconds(delay);

        ReturnToPool(objectToReturn);
    }

    private void ReturnToPool(GameObject objectToReturn)
    {
        // PooledObject stores which prefab queue this instance belongs to.
        GameObject originalPrefab = objectToReturn.GetComponent<PooledObject>().originalPrefab;

        if (objectToReturn.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnReturnedToPool(); // Let the object cleanup itself if it implements IPoolable

        objectToReturn.SetActive(false); // Deactivate the object before returning it to the pool
        objectToReturn.transform.SetParent(transform); // Set the pool as the parent for organization

        poolDictionary[originalPrefab].Enqueue(objectToReturn);
    }

    private void initializeNewPool(GameObject prefab)
    {
        poolDictionary[prefab] = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObject(prefab);
        }
    }

    private void CreateNewObject(GameObject prefab)
    {
        GameObject newObject = Instantiate(prefab, transform);

        // Store the prefab key so returns always go to the correct queue.
        newObject.AddComponent<PooledObject>().originalPrefab = prefab;
        newObject.SetActive(false); // Deactivate the object until it's needed

        poolDictionary[prefab].Enqueue(newObject);
    }

    #endregion
}
