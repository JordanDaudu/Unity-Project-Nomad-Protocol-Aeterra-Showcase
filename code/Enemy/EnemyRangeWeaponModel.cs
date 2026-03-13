using System;
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

    [Header("Gun Points")]
    [Tooltip("The point from which bullets are fired. This transform is used to position and orient bullets when shooting.")]
    [SerializeField] private Transform gunPoint;

    [Header("IK")]
    [Tooltip("If true, this weapon model will reposition the enemy's left hand/elbow IK targets.")]
    [SerializeField] private bool requiresLeftHandIK;

    [Tooltip("Target transform used to place the left hand for this weapon model.")]
    [SerializeField] private Transform leftHandTarget;

    [Tooltip("Target transform used to place the left elbow for this weapon model.")]
    [SerializeField] private Transform leftElbowTarget;

    [Header("Runtime Visual Roots")]
    [Tooltip("The normal weapon visual shown during regular combat.")]
    [SerializeField] private GameObject primaryVisualRoot;

    [Tooltip("The temporary alternate visual shown during animation swaps, such as grenade throws.")]
    [SerializeField] private GameObject secondaryVisualRoot;

    public EnemyRangeWeaponType WeaponType => weaponType;
    public override int AnimationLayerIndex => (int)(weaponHoldType);
    public Transform GunPoint => gunPoint;

    // IK requirement is defined per weapon model, not per enemy class.
    public override bool RequiresLeftHandIK => requiresLeftHandIK;
    public override Transform LeftHandTarget => leftHandTarget;
    public override Transform LeftElbowTarget => leftElbowTarget;

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