using UnityEngine;

/// <summary>
/// Small helper component added to pooled instances.
/// Stores the original prefab reference so <see cref="ObjectPool"/> can return the instance
/// to the correct queue, even when multiple prefab types are pooled.
/// </summary>
public class PooledObject : MonoBehaviour
{
    public GameObject originalPrefab;
}
