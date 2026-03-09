using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Visual/animation layer for weapons.
/// 
/// Responsibilities:
/// - Manage which weapon model GameObject is active (current + backup models)
/// - Drive animation triggers/parameters (fire, reload, equip)
/// - Manage Animation Rigging weights (rig + left hand IK) for smooth transitions
/// 
/// Notes:
/// - Timing is coordinated with animation events (see <see cref="PlayerAnimationEvents"/>).
/// - Weapon models are discovered in children at runtime (GetComponentsInChildren) to keep setup flexible.
/// </summary>
public class PlayerWeaponVisuals : MonoBehaviour
{
    #region Components

    private Player player;
    private Animator anim;
    private Rig rig;

    #endregion

    #region Inspector

    [SerializeField] private WeaponModel[] weaponModels;
    [SerializeField] private BackupWeaponModel[] backupWeaponModels;

    [Header("Rig")]
    [SerializeField] private float rigWeightIncreaseRate;

    [Header("Left hand IK")]
    [SerializeField] private float leftHandIKWeightIncreaseRate;
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform leftHandIK_Target;

    #endregion

    #region Runtime

    private bool shouldIncrease_RigWeight;
    private bool shouldIncrease_LeftHandIKWeight;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        player = GetComponent<Player>();
        anim = GetComponentInChildren<Animator>();
        rig = GetComponentInChildren<Rig>();

        // Load models from children (inactive included) so each weapon can be toggled by type.
        weaponModels = GetComponentsInChildren<WeaponModel>(true);
        backupWeaponModels = GetComponentsInChildren<BackupWeaponModel>(true);
    }

    private void Update()
    {
        UpdateRigWeight();
        UpdateLeftHandIKWeight();
    }

    #endregion

    #region Animation Triggers

    public void PlayFireAnimation() => anim.SetTrigger("Fire");

    public void PlayReloadAnimation()
    {

        float reloadSpeed = player.weapon.CurrentWeapon().reloadSpeed;

        anim.SetFloat("ReloadSpeed", reloadSpeed);
        anim.SetTrigger("Reload");

        // Reduce rig weight during the reload animation for cleaner transitions.
        ReduceRigWeight();
    }


    public void PlayWeaponEquipAnimation()
    {
        WeaponModel model = CurrentWeaponModel();
        if (model == null)
        {
            Debug.LogError($"No WeaponModel found for weaponType={player.weapon.CurrentWeapon().weaponType}. " +
                           $"Check weaponModels in hierarchy / weaponType assignments.");
            return;
        }

        EquipType equipType = model.equipAnimationType;

        float equipmentSpeed = player.weapon.CurrentWeapon().equipmentSpeed;

        // Reset IK and rig so equip animation can blend in naturally.
        leftHandIK.weight = 0;
        ReduceRigWeight();

        anim.SetTrigger("EquipWeapon");
        anim.SetFloat("EquipType", ((float)equipType));
        anim.SetFloat("EquipSpeed", equipmentSpeed);
    }

    #endregion

    #region Model Switching

    /// <summary>
    /// Called from an animation event so the model swap happens at the correct frame.
    /// </summary>
    public void SwitchOnCurrentWeaponModel()
    {
        int animationIndex = (int)CurrentWeaponModel().holdType;

        // Important: turn off all weapon models first to avoid overlapping meshes.
        SwitchOffWeaponModels(); // Important to stay here so animation method is timed correctly with the model switch

        SwitchOffBackupWeaponModels();

        // If we have more than one weapon, show a backup weapon model on the body.
        if (player.weapon.HasOnlyOneWeapon() == false)
            SwitchOnBackupWeaponModel();

        SwitchAnimationLayer(animationIndex);
        CurrentWeaponModel().gameObject.SetActive(true);
        AttachLeftHand();
    }


    public void SwitchOffWeaponModels()
    {
        for (int i = 0; i < weaponModels.Length; i++)
        {
            weaponModels[i].gameObject.SetActive(false);
        }
    }

    public void SwitchOnBackupWeaponModel()
    {
        SwitchOffBackupWeaponModels();

        BackupWeaponModel lowHangWeapon = null;
        BackupWeaponModel backHangWeapon = null;
        BackupWeaponModel sideHangWeapon = null;

        foreach (BackupWeaponModel backupWeaponModel in backupWeaponModels)
        {
            // Never show the current weapon as a backup.
            if (backupWeaponModel.weaponType == player.weapon.CurrentWeapon().weaponType)
                continue;

            // Only display weapons the player actually owns.
            if (player.weapon.HasWeaponInSlots(backupWeaponModel.weaponType) != null)
            {
                if (backupWeaponModel.HangTypeIs(HangType.LowBackHang) && lowHangWeapon == null)
                    lowHangWeapon = backupWeaponModel;

                else if (backupWeaponModel.HangTypeIs(HangType.BackHang) && backHangWeapon == null)
                    backHangWeapon = backupWeaponModel;

                else if (backupWeaponModel.HangTypeIs(HangType.SideHang) && sideHangWeapon == null)
                    sideHangWeapon = backupWeaponModel;
            }
        }

        lowHangWeapon?.Activate(true);
        backHangWeapon?.Activate(true);
        sideHangWeapon?.Activate(true);
    }

    private void SwitchOffBackupWeaponModels()
    {
        foreach (BackupWeaponModel backupWeaponModel in backupWeaponModels)
        {
            backupWeaponModel.Activate(false);
        }
    }

    private void SwitchAnimationLayer(int  layerIndex)
    {
        // Layer 0 is the base layer and should always be active for core animations (idle, walk, run).
        // Weapon-specific actions (fire, reload, equip) are on separate layers to avoid conflicts and allow for smooth blending.
        for (int i = 1; i < anim.layerCount; i++)
        {
            anim.SetLayerWeight(i, 0);
        }

        anim.SetLayerWeight(layerIndex, 1);
    }
    public WeaponModel CurrentWeaponModel()
    {
        WeaponModel weaponModel = null;

        WeaponType weaponType = player.weapon.CurrentWeapon().weaponType;

        for (int i = 0; i < weaponModels.Length; i++)
        {
            if (weaponModels[i].weaponType == weaponType)
            {
                weaponModel = weaponModels[i];
                break;
            }
        }

        return weaponModel;
    }

    #endregion

    #region Animation Rigging Methods
    private void AttachLeftHand()
    {
        // WeaponModel.holdPoint stores where the left hand should be placed for this weapon.
        Transform targetTranform = CurrentWeaponModel().holdPoint;

        leftHandIK_Target.localPosition = targetTranform.localPosition;
        leftHandIK_Target.localRotation = targetTranform.localRotation;
    }
    private void UpdateLeftHandIKWeight()
    {
        if (shouldIncrease_LeftHandIKWeight)
        {
            leftHandIK.weight += leftHandIKWeightIncreaseRate * Time.deltaTime;

            if (leftHandIK.weight >= 1)
                shouldIncrease_LeftHandIKWeight = false;
        }
    }

    private void UpdateRigWeight()
    {
        if (shouldIncrease_RigWeight)
        {
            rig.weight += rigWeightIncreaseRate * Time.deltaTime;

            if (rig.weight >= 1)
                shouldIncrease_RigWeight = false;
        }
    }

    private void ReduceRigWeight()
    {
        // Magic number: low but non-zero weight helps avoid harsh snapping between animations.
        rig.weight = .15f;
    }
    public void MaximizeRigWeight() => shouldIncrease_RigWeight = true;
    public void MaximizeLeftHandIKWeight() => shouldIncrease_LeftHandIKWeight = true;

    #endregion
}