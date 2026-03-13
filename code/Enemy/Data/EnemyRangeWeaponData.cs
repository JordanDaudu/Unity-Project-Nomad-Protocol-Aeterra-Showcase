using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data describing how an enemy ranged weapon behaves.
/// Stored as a ScriptableObject so different enemies/weapons can reuse presets.
/// </summary>
[CreateAssetMenu(fileName = "New Weapon Data", menuName = "ScriptableObjects/Enemy Data/Range Weapon Data", order = 2)]

public class EnemyRangeWeaponData : ScriptableObject
{
    [Header("Range Weapon Details")]
    [SerializeField] private EnemyRangeWeaponType weaponType;

    [Tooltip("Shots fired per second during a burst.")]
    [Min(0.01f)]
    [SerializeField] private float fireRate = 1f;

    [Tooltip("Minimum bullets fired in one attack action.")]
    [Min(1)]
    [SerializeField] private int minBulletsPerAttack = 1;

    [Tooltip("Maximum bullets fired in one attack action.")]
    [Min(1)]
    [SerializeField] private int maxBulletsPerAttack = 1;

    [Tooltip("Minimum delay before this weapon can attack again.")]
    [Min(0f)]
    [SerializeField] private float minWeaponCooldown = 2f;

    [Tooltip("Maximum delay before this weapon can attack again.")]
    [Min(0f)]
    [SerializeField] private float maxWeaponCooldown = 3f;

    [Header("Bullet Details")]
    [Tooltip("Projectile travel speed.")]
    [Min(0f)]
    [SerializeField] private float bulletSpeed = 20f;

    [Tooltip("Random spread applied to shots.")]
    [Min(0f)]
    [SerializeField] private float weaponSpread = 0.1f;

    // Read-only access for constant weapon settings.
    public EnemyRangeWeaponType WeaponType => weaponType;
    public float FireRate => fireRate;
    public float BulletSpeed => bulletSpeed;
    public float WeaponSpread => weaponSpread;

    public float MinWeaponCooldown => minWeaponCooldown;

    /// <summary>
    /// Returns how many bullets should be fired for this specific attack.
    /// Use once per attack and store the result if consistency matters.
    /// </summary>
    public int RollBulletsPerAttack()
    {
        return Random.Range(minBulletsPerAttack, maxBulletsPerAttack + 1);
    }

    /// <summary>
    /// Returns the cooldown duration after this specific attack.
    /// Use once per attack and store the result if consistency matters.
    /// </summary>
    public float RollWeaponCooldown()
    {
        return Random.Range(minWeaponCooldown, maxWeaponCooldown);
    }

    public Vector3 ApplyWeaponSpread(Vector3 originalDirection)
    {
        float randomizeSpread = Random.Range(-weaponSpread, weaponSpread);
        Quaternion spreadRotation = Quaternion.Euler(randomizeSpread, randomizeSpread, randomizeSpread);

        return spreadRotation * originalDirection;
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep max values from dropping below min values in the inspector.
        if (maxBulletsPerAttack < minBulletsPerAttack)
            maxBulletsPerAttack = minBulletsPerAttack;

        if (maxWeaponCooldown < minWeaponCooldown)
            maxWeaponCooldown = minWeaponCooldown;
    }
#endif
}
