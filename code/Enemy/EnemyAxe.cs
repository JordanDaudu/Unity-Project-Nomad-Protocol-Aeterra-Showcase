using UnityEngine;

/// <summary>
/// Pooled projectile used by axe-throw melee variants.
///
/// Behavior:
/// - Rotates the visual for readability.
/// - For an initial "aiming" window (timer > 0), it constantly updates direction toward the player.
/// - After the window expires, it continues flying in the last computed direction.
/// - On trigger hit with a <see cref="Bullet"/> or <see cref="Player"/>, it spawns impact FX and returns to pool.
/// 
/// Key connections:
/// - Spawned by <see cref="AbilityState_Melee.AbilityTrigger"/> via <see cref="ObjectPool"/>.
/// - Returned to <see cref="ObjectPool"/> on impact.
/// </summary>
public class EnemyAxe : MonoBehaviour
{
    [SerializeField] private GameObject impactFX;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform axeVisual;

    private Vector3 direction;
    private Transform player;
    private float flySpeed;
    private float rotationSpeed;

    private float timer = 1; /// Remaining time where the axe still "tracks" the player before committing to its direction.

    private void Update()
    {
        // Spin only the visual mesh (not the whole projectile) for a clean "throw" look.
        axeVisual.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        timer -= Time.deltaTime;

        // During the aim window, keep updating direction toward the player.
        if (timer > 0)
            direction = player.position + Vector3.up - transform.position;

        rb.linearVelocity = direction.normalized * flySpeed;

        // Align forward with velocity for consistent visual orientation.
        transform.forward = rb.linearVelocity;
    }

    /// <summary>
    /// Called after the axe is spawned from the pool.
    /// </summary>
    public void AxeSetup(float flySpeed, Transform player, float timer)
    {
        rotationSpeed = 1600;

        this.flySpeed = flySpeed;
        this.player = player;
        this.timer = timer;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Axe reacts to bullets (player can shoot it) and to player contact.
        Bullet bullet = other.GetComponent<Bullet>();
        Player player = other.GetComponent<Player>();

        if (bullet != null || player != null)
        {
            GameObject newFx = ObjectPool.Instance.GetObject(impactFX, transform);

            ObjectPool.Instance.ReturnObjectToPool(gameObject);
            ObjectPool.Instance.ReturnObjectToPool(newFx, 1f);
        }
    }
}
