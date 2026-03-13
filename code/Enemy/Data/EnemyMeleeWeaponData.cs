using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data asset describing a melee enemy weapon loadout.
/// </summary>
/// <remarks>
/// Stored as a ScriptableObject so multiple melee enemies can share the same attack presets.
///
/// Key connection:
/// - <see cref="EnemyMeleeWeaponModel"/> can reference this data and expose it through <c>WeaponData</c>
///   for melee AI to read attack lists / tuning.
/// </remarks>
[CreateAssetMenu(fileName = "New Weapon Data", menuName = "ScriptableObjects/Enemy Data/Melee Weapon Data", order = 1)]
public class EnemyMeleeWeaponData : ScriptableObject
{
    [Tooltip("Attack presets this melee weapon can perform.")]
    public List<AttackData_EnemyMelee> attackData;

    [Tooltip("Default turn speed used during attack/ability facing logic.")]
    public float turnSpeed = 10;
}
