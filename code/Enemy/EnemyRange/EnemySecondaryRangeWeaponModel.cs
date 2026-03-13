using UnityEngine;

/// <summary>
/// Marker component for identifying a ranged enemy's secondary weapon visual.
/// </summary>
/// <remarks>
/// This is intentionally lightweight: it exists so systems like <see cref="EnemyVisuals"/> can find a
/// secondary weapon model (e.g., a grenade-throw weapon swap) without hard-coding specific transforms.
///
/// Key connection:
/// - <see cref="EnemyVisuals.EnableSecondaryWeaponModel"/> searches for this component when toggling the secondary model.
/// </remarks>
public class EnemySecondaryRangeWeaponModel : MonoBehaviour
{
    public EnemyRangeWeaponType weaponType;
}
