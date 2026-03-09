using UnityEngine;

/// <summary>
/// Visual weapon model for melee enemies.
/// </summary>
/// <remarks>
/// This component represents a melee weapon variant that can be selected by
/// <see cref="EnemyVisuals"/> when configuring an <see cref="EnemyMelee"/>.
///
/// It stores the melee weapon type it matches, optional weapon data,
/// and optional trail effects used during melee attacks.
/// </remarks>
public class EnemyMeleeWeaponModel : EnemyWeaponModel
{
    [Header("Melee Weapon")]
    [Tooltip("The melee weapon type represented by this visual model.")]
    [SerializeField] private EnemyMeleeWeaponType weaponType;

    [Tooltip("Optional data asset describing this melee weapon variant.")]
    [SerializeField] private EnemyMeleeWeaponData weaponData;

    [Header("Effects")]
    [Tooltip("Trail effect objects to enable while melee attacks are active.")]
    [SerializeField] private GameObject[] trailEffects;

    /// <summary>
    /// Gets the melee weapon type represented by this model.
    /// </summary>
    public EnemyMeleeWeaponType WeaponType => weaponType;

    /// <summary>
    /// Gets the data associated with this melee weapon model.
    /// </summary>
    public EnemyMeleeWeaponData WeaponData => weaponData;

    /// <summary>
    /// Disables all configured trail effects when the object awakens,
    /// ensuring the weapon starts in an idle visual state.
    /// </summary>
    private void Awake()
    {
        EnableTrailEffect(false);
    }

    /// <summary>
    /// Determines whether this melee weapon model is compatible with the specified enemy.
    /// </summary>
    /// <param name="enemy">The enemy being evaluated.</param>
    /// <returns>
    /// <c>true</c> if the enemy is an <see cref="EnemyMelee"/> and its weapon type
    /// matches this model's <see cref="WeaponType"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsCompatibleWith(Enemy enemy)
    {
        EnemyMelee meleeEnemy = enemy as EnemyMelee;
        return meleeEnemy != null && meleeEnemy.weaponType == weaponType;
    }

    /// <summary>
    /// Enables or disables all configured melee trail effects.
    /// </summary>
    /// <param name="enable">
    /// <c>true</c> to enable trail effects; <c>false</c> to disable them.
    /// </param>
    public override void EnableTrailEffect(bool enable)
    {
        if (trailEffects == null || trailEffects.Length == 0)
            return;

        foreach (GameObject effect in trailEffects)
        {
            if (effect == null)
                continue;

            effect.SetActive(enable);
        }
    }
}