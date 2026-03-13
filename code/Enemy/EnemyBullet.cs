using UnityEngine;

/// <summary>
/// Bullet fired by ranged enemies.
/// </summary>
/// <remarks>
/// This projectile inherits from <see cref="Bullet"/> so it reuses pooling, trail fade, and lifetime behavior,
/// but it overrides collision handling to apply enemy-specific hit rules (e.g., damage to the player).
///
/// Key connections:
/// - Spawned by <see cref="EnemyRange"/> when shooting.
/// - On collision, searches for <see cref="Player"/> in the hit hierarchy.
/// </remarks>
public class EnemyBullet : Bullet
{
    protected override void OnCollisionEnter(Collision collision)
    {
        CreateImpactFx();
        ReturnBulletToPool();

        Player player = collision.gameObject.GetComponentInParent<Player>();

        if (player != null)
        {
            // TODO (gameplay): apply damage/knockback to the player.
            Debug.Log("Player hit by enemy bullet!");
        }
    }
}
