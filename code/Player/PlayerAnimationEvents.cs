using UnityEngine;

/// <summary>
/// Animation Event receiver for player weapon animations.
/// 
/// This component is meant to live on the animated weapon/player rig object so animation clips
/// can call these methods via Unity Animation Events.
/// 
/// Key idea:
/// - Visual timeline (animations) owns timing.
/// - Gameplay timeline (weapon ready, ammo refill, model swap) is triggered by these callbacks.
/// </summary>
public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerWeaponVisuals visualController;
    private PlayerWeaponController weaponController;

    private void Start()
    {
        visualController = GetComponentInParent<PlayerWeaponVisuals>();
        weaponController = GetComponentInParent<PlayerWeaponController>();
    }

    /// <summary>
    /// Called at the end of the reload animation.
    /// Refills bullets and re-enables weapon firing.
    /// </summary>
    public void ReloadIsOver()
    {
        visualController.MaximizeRigWeight();
        weaponController.CurrentWeapon().RefillBullets();

        weaponController.SetWeaponReady(true);
    }

    /// <summary>
    /// Called when we want to restore rig/IK weights after an animation transition.
    /// </summary>
    public void ReturnRig()
    {
        visualController.MaximizeRigWeight();
        visualController.MaximizeLeftHandIKWeight();
    }

    /// <summary>
    /// Called at the end of the weapon equip animation.
    /// NOTE: Method name kept as-is because it is referenced by animation events.
    /// </summary>
    public void WeaponEquipingIsOver()
    {
        weaponController.SetWeaponReady(true);
    }

    /// <summary>
    /// Called at the exact animation frame where the weapon model should switch.
    /// </summary>
    public void SwitchOnWeaponModel()
    {
        visualController.SwitchOnCurrentWeaponModel();
    }
}
