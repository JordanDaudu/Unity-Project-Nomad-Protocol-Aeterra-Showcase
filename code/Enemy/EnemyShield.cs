using UnityEngine;

/// <summary>
/// Simple durability-based shield for melee enemy variants.
///
/// Behavior:
/// - When active, bullets should hit this shield collider first (see Bullet.cs).
/// - Each bullet reduces durability.
/// - When durability reaches 0, the shield disables itself and the enemy switches to non-shield animations.
/// 
/// Key connections:
/// - Bullet checks for <see cref="EnemyShield"/> on the hit collider before applying damage to <see cref="Enemy"/>.
/// - EnemyMelee initializes and restores durability for shield variants.
/// </summary>
public class EnemyShield : MonoBehaviour
{
    [SerializeField] private int durability;

    private int currentDurability;
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();

        if (enemy == null)
            Debug.LogError($"[{nameof(EnemyShield)}] No Enemy found in parent hierarchy of {name}", this);
    }

    private void OnEnable()
    {
        // Pool-safe: re-enable restores durability.
        currentDurability = durability;
    }

    public void RestoreDurability() => currentDurability = durability;

    public void ReduceDurability()
    {
        currentDurability--;

        if (currentDurability <= 0)
        {
            gameObject.SetActive(false);

            // Animation parameter used by EnemyMelee animator to choose shielded/non-shielded chase set.
            enemy.anim.SetFloat("ChaseIndex", 0); // Switch back to the non-shielded animation
            Debug.Log("Shield broken!");
        }
    }
}
