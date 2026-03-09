using UnityEngine;

/// <summary>
/// Pooled projectile that travels forward, fades its trail near max distance,
/// and returns itself to <see cref="ObjectPool"/>.
/// 
/// Responsibilities:
/// - Manage its own lifetime using max fly distance
/// - Disable collider/mesh when max distance is reached (trail can finish fading)
/// - Spawn pooled impact FX on collision
/// 
/// Notes:
/// - Bullet instances are expected to be spawned via <see cref="ObjectPool"/>.
/// - The bullet uses TrailRenderer.time as a simple "fade-out timer" and returns to pool when it reaches < 0.
/// - On hit, looks for <see cref="Enemy"/> and optional <see cref="EnemyShield"/> in the collider hierarchy.
/// </summary>
public class Bullet : MonoBehaviour
{
    #region Inspector

    [SerializeField] private GameObject bulletImpactFX;

    #endregion

    #region Components

    private Rigidbody rb;
    private BoxCollider cd;
    private MeshRenderer meshRenderer;
    private TrailRenderer trailRenderer;

    #endregion

    #region Runtime

    private float impactForce;
    private Vector3 startPosition;
    private float flyDistance;
    private bool bulletDisabled;

    #endregion

    #region Unity Callbacks

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cd = GetComponent<BoxCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
    }

    protected virtual void Update()
    {
        FadeTrailIfNeeded();
        DisableBulletIfNeeded();
        ReturnToPoolIfNeeded();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Always spawn impact FX and return the bullet to the pool.
        // Note: Return is delayed in ObjectPool (default 0.001f), so we can still run hit logic below.
        CreateImpactFx(collision);
        ReturnBulletToPool();

        // Shield is checked on the exact hit collider (shield can be a separate child object).
        EnemyShield enemyShield = collision.gameObject.GetComponent<EnemyShield>();

        if (enemyShield != null)
        {
            enemyShield.ReduceDurability();
             return;
        }

        // Enemy is resolved from parent hierarchy (ragdoll bones / hitboxes are usually children).
        Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            // Apply force in the direction of travel.
            Vector3 force = rb.linearVelocity.normalized * impactForce;
            // attachedRigidbody is typically the ragdoll bone Rigidbody that was hit (if ragdoll colliders are active).
            Rigidbody hitRigidBody = collision.collider.attachedRigidbody;

            enemy.GetHit();
            enemy.DeathImpact(force, collision.contacts[0].point, hitRigidBody);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called right after the bullet is spawned from the pool.
    /// Resets runtime state for reuse.
    /// </summary>
    /// <param name="flyDistance">
    /// Maximum travel distance before the bullet disables itself and starts fading out.
    /// </param>
    public void BulletSetup(float flyDistance = 100f, float impactForce = 100f)
    {
        this.impactForce = impactForce;

        bulletDisabled = false;

        // Re-enable collision/visuals (they may have been disabled before returning to pool).
        cd.enabled = true;
        meshRenderer.enabled = true;

        // Reset trail life so the bullet starts with a visible trail.
        trailRenderer.time = .5f;

        startPosition = transform.position;

        // NOTE: +0.5f is the length of the "laser tip" used in PlayerAim.UpdateAimVisuals().
        // This keeps the bullet alive long enough to visually reach the end of the laser.
        this.flyDistance = flyDistance + .5f;
    }

    #endregion

    #region Internal Logic

    private void ReturnToPoolIfNeeded()
    {
        // Once the trail has fully faded, the bullet can safely be returned to the pool.
        if (trailRenderer.time < 0)
            ReturnBulletToPool();
    }

    private void DisableBulletIfNeeded()
    {
        // Disable collision and mesh once we exceeded the max distance.
        // We keep the TrailRenderer active so it can finish fading out naturally.
        if (Vector3.Distance(startPosition, transform.position) > flyDistance && !bulletDisabled)
        {
            cd.enabled = false;
            meshRenderer.enabled = false;
            bulletDisabled = true;
        }
    }

    protected void FadeTrailIfNeeded()
    {
        // Start fading the trail slightly before max distance so the end-of-life looks natural.
        // Magic numbers:
        // - 1.5f: start fade this many meters before flyDistance
        // - 8f: fade speed multiplier
        if (Vector3.Distance(startPosition, transform.position) > flyDistance - 1.5f)
            trailRenderer.time -= 8f * Time.deltaTime;
    }

    protected void ReturnBulletToPool() => ObjectPool.Instance.ReturnObjectToPool(gameObject);

    protected void CreateImpactFx(Collision collision)
    {
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];

            // Impact FX is also pooled to avoid instantiations during shooting.
            GameObject newImpactFx = ObjectPool.Instance.GetObject(bulletImpactFX);
            newImpactFx.transform.position = contact.point;

            // Delay-return to allow the FX to play.
            ObjectPool.Instance.ReturnObjectToPool(newImpactFx, 1f);
        }
    }

    #endregion
}
