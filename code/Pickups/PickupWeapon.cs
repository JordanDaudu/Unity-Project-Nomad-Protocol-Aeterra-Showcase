using UnityEngine;

/// <summary>
/// Interactable weapon pickup.
/// 
/// Two creation modes:
/// 1) Placed in the scene with a <see cref="WeaponData"/> asset (creates a new runtime <see cref="Weapon"/>)
/// 2) Spawned by dropping a weapon (receives an existing runtime <see cref="Weapon"/>, preserving ammo state)
/// 
/// Notes:
/// - Visual model selection is based on weaponType (see <see cref="BackupWeaponModel"/> children).
/// - This pickup is pooled via <see cref="ObjectPool"/>, so Start() may run multiple times across reuse.
/// </summary>
public class PickupWeapon : Interactable
{
    #region Inspector

    [SerializeField] private WeaponData weaponData;
    [SerializeField] private BackupWeaponModel[] models;

    // Serialized for debugging; runtime value is created from weaponData or injected via SetupPickupWeapon.
    [SerializeField] private Weapon weapon;

    #endregion

    #region Runtime

    // True when this pickup was spawned from an existing runtime weapon (drop), not created from weaponData.
    private bool oldWeapon;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        // If this pickup was not initialized by a drop, build a fresh runtime weapon from the data asset.
        if (oldWeapon == false)
            weapon = new Weapon(weaponData);

        SetupGameObject();
    }

    #endregion

    #region Setup

    /// <summary>
    /// Initializes the pickup to represent an existing runtime weapon (ammo/state preserved).
    /// Called when the player drops a weapon.
    /// </summary>
    public void SetupPickupWeapon(Weapon weapon, Transform transform)
    {
        oldWeapon = true;

        this.weapon = weapon;
        weaponData = weapon.weaponData;

        // Spawn slightly above ground so it is visible and doesn't clip.
        this.transform.position = transform.position + new Vector3(0, .75f, 0);
    }

    [ContextMenu("Update Item Model")]
    public void SetupGameObject()
    {
        gameObject.name = "Pickup_Weapon - " + weaponData.weaponType.ToString();
        SetupWeaponModel();
    }

    private void SetupWeaponModel()
    {
        if (models.Length == 0)
            Debug.LogWarning("No model found for weapon type: " + weaponData.weaponType);

        foreach (BackupWeaponModel model in models)
        {
            model.gameObject.SetActive(false);

            if (model.weaponType == weaponData.weaponType)
            {
                model.gameObject.SetActive(true);

                // Keep base highlighting logic working after model swap.
                UpdateMeshAndMaterial(model.GetComponent<MeshRenderer>());
            }
        }
    }

    #endregion

    #region Interaction

    public override void Interaction()
    {
        // Debug.Log("Picked up weapon: " + weaponData.weaponName);
        weaponController.PickupWeapon(weapon);

        // Return this pickup object to the pool for reuse.
        ObjectPool.Instance.ReturnObjectToPool(gameObject);
    }

    #endregion
}
