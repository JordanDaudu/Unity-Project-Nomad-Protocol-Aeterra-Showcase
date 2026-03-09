using UnityEngine;

/// <summary>
/// Equip animation style used by the Animator.
/// </summary>
public enum EquipType
{
    SideEquipAnimation,
    BackEquipAnimation
}

/// <summary>
/// Hold/pose style used by the Animator.
/// 
/// Note:
/// The numeric values are aligned with Animator layer indices (layer 1..N),
/// so keep these in sync with the Animator Controller.
/// </summary>
public enum  HoldType
{
    CommonHold = 1, // Same starting index as in animator
    LowHold,
    HighHold
}

/// <summary>
/// Visual representation of a weapon in the player's hands.
/// 
/// Used by:
/// - <see cref="PlayerWeaponVisuals"/> to select the correct model for the current weapon
/// - <see cref="PlayerWeaponController"/> to retrieve gunPoint for bullet spawning
/// </summary>
public class WeaponModel : MonoBehaviour
{
    public WeaponType weaponType;

    [Header("Animation")]
    public EquipType equipAnimationType;
    public HoldType holdType;

    [Header("Attachment Points")]
    public Transform gunPoint;

    // Where the left hand should go when holding this weapon (used by IK).
    public Transform holdPoint;
}
