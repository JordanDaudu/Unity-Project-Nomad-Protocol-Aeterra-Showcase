using System;
using UnityEngine;

/// <summary>
/// Physics-based grenade thrown by ranged enemies.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Compute a ballistic launch velocity to hit a target in a given time.</description></item>
/// <item><description>Count down to detonation after being thrown.</description></item>
/// <item><description>Spawn a pooled explosion effect and return itself to the pool.</description></item>
/// <item><description>Apply an explosion force to the player (currently impulse-only, no damage).</description></item>
/// </list>
///
/// Key connections:
/// - Spawned/configured by <see cref="EnemyRange.ThrowGrenade"/> (called from <see cref="ThrowGrenadeState_Range"/> via animation event).
/// - Uses <see cref="ObjectPool"/> for both the grenade object and explosion FX.
/// </remarks>
public class EnemyGrenade : MonoBehaviour
{
    #region Inspector

    [Header("Explosion Settings")]
    [Tooltip("Prefab for the explosion effect to spawn when the grenade detonates.")]
    [SerializeField] private GameObject explosionEffectPrefab;

    [Tooltip("Radius of the explosion effect and damage area.")]
    [SerializeField] private float explosionRadius = 5f;

    [Tooltip("Multiplier for the vertical component of the explosion force, allowing for stronger upward blasts.")]
    [SerializeField] private float upwardsMultiplier = 1f;

    #endregion

    #region Inspector

    private Rigidbody rb;
    private float timer;
    private float impactPower;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0f)
            Explode();
    }

    private void OnEnable()
    {

    }

    #endregion

    #region Public API

    /// <summary>
    /// Configures the grenade right after it is spawned/thrown.
    /// </summary>
    /// <param name="target">World position the grenade should land near.</param>
    /// <param name="timeToTarget">Flight time (seconds) used to compute the launch arc.</param>
    /// <param name="countdown">Detonation delay after landing (added on top of flight time).</param>
    /// <param name="impactPower">Explosion force magnitude applied to rigidbodies in range.</param>
    public void SetupGrenade(Vector3 target, float timeToTarget, float countdown, float impactPower)
    {
        rb.linearVelocity = CalculateLaunchVelocity(target, timeToTarget);
        timer = countdown + timeToTarget;
        this.impactPower = impactPower;
    }

    #endregion

    #region Ballistics Calculation

    /// <summary>
    /// Calculates the initial velocity required to launch a projectile from the current position
    /// to a target point so it arrives after the specified travel time.
    /// </summary>
    /// <param name="target">The world position the projectile should reach.</param>
    /// <param name="timeToTarget">The desired time, in seconds, for the projectile to reach the target.</param>
    /// <returns>
    /// A launch velocity vector that combines:
    /// - horizontal speed needed to reach the target in the given time
    /// - vertical speed needed to counter gravity and reach the target's height
    /// </returns>
    /// <remarks>
    /// This calculation assumes constant gravity and no air resistance.
    /// The horizontal velocity is calculated separately from the vertical velocity:
    /// horizontal motion is linear, while vertical motion accounts for gravitational acceleration.
    /// </remarks>
    public Vector3 CalculateLaunchVelocity(Vector3 target, float timeToTarget)
    {
        Vector3 direction = target - transform.position;
        Vector3 directionXZ = new Vector3(direction.x, 0f, direction.z);

        Vector3 velocityXZ = directionXZ / timeToTarget;

        float velocityY = (direction.y - (Physics.gravity.y * Mathf.Pow(timeToTarget, 2)) / 2) / timeToTarget;

        Vector3 launchVelocity = velocityXZ + Vector3.up * velocityY;

        return launchVelocity;
    }

    #endregion

    #region Explosion

    private void Explode()
    {
        GameObject newFx = ObjectPool.Instance.GetObject(explosionEffectPrefab, transform);

        ObjectPool.Instance.ReturnObjectToPool(gameObject);
        ObjectPool.Instance.ReturnObjectToPool(newFx, 1);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddExplosionForce(impactPower, transform.position, explosionRadius, upwardsMultiplier, ForceMode.Impulse);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    #endregion
}
