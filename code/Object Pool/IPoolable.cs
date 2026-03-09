using UnityEngine;

/// <summary>
/// Optional lifecycle callbacks for objects managed by an <see cref="ObjectPool"/>.
/// 
/// Purpose:
/// - Lets pooled objects reset themselves when they are reused
/// - Keeps reset/cleanup logic inside the pooled object (Enemy, projectile, VFX, etc.)
/// - Avoids hardcoding type-specific logic inside the pool
/// 
/// Typical uses:
/// - OnSpawnedFromPool():
///   Reset health/state, re-enable components, restore visuals/materials, clear timers
/// - OnReturnedToPool():
///   Stop sounds/particles/coroutines, cleanup temporary state before deactivation
/// 
/// Notes:
/// - This does NOT replace <see cref="PooledObject"/>.
/// - <see cref="PooledObject"/> stores the original prefab reference (queue identity),
///   while <see cref="IPoolable"/> provides behavior hooks (lifecycle callbacks).
/// - Any pooled object can implement this interface if it needs custom reset/cleanup logic.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Called by the pool immediately after the object is taken from the pool and activated.
    /// Use this to restore a valid "freshly spawned" state.
    /// </summary>
    void OnSpawnedFromPool();

    /// <summary>
    /// Called by the pool immediately before the object is deactivated and returned to the pool.
    /// Use this to cleanup temporary runtime state.
    /// </summary>
    void OnReturnedToPool();
}