using UnityEngine;

public enum WeaponHoldType_EnemyRange { Common, LowHold, HighHold }

/// <summary>
/// Visual weapon model for ranged enemies.
/// </summary>
/// <remarks>
/// This component represents a ranged weapon variant that can be selected by
/// <see cref="EnemyVisuals"/> when configuring an <see cref="EnemyRange"/>.
/// </remarks>
public class EnemyRangeWeaponModel : EnemyWeaponModel
{
    [Header("Ranged Weapon")]
    [Tooltip("The ranged weapon type represented by this visual model.")]
    [SerializeField] private EnemyRangeWeaponType weaponType;

    [Header("Animation")]
    [SerializeField] private WeaponHoldType_EnemyRange weaponHoldType;

    public EnemyRangeWeaponType WeaponType => weaponType;
    public override int AnimationLayerIndex => (int)(weaponHoldType);

    /// <summary>
    /// Determines whether this ranged weapon model is compatible with the specified enemy.
    /// </summary>
    /// <param name="enemy">The enemy being evaluated.</param>
    /// <returns>
    /// <c>true</c> if the enemy is an <see cref="EnemyRange"/> and its weapon type
    /// matches this model's <see cref="WeaponType"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsCompatibleWith(Enemy enemy)
    {
        EnemyRange rangedEnemy = enemy as EnemyRange;
        return rangedEnemy != null && rangedEnemy.weaponType == weaponType;
    }
}