using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Defines a weapon type + amount range for ammo box contents.
/// </summary>
[System.Serializable]
public struct AmmoData
{
    public WeaponType weaponType;
    [Range(10, 100)] public int minAmount;
    [Range(10, 100)] public int maxAmount;
}
public enum AmmoBoxType
{
    SmallBox,
    BigBox
}

/// <summary>
/// Interactable ammo box pickup.
/// 
/// Behavior:
/// - On interaction, iterates its configured ammo entries and adds ammo to any matching weapon the player owns.
/// - If the player doesn't own a weapon type, that entry is ignored.
/// - Returns itself to <see cref="ObjectPool"/> after use.
/// </summary>
public class PickupAmmo : Interactable
{
    #region Inspector

    [SerializeField] private AmmoBoxType ammoBoxType;

    [SerializeField] private List<AmmoData> smallBoxAmmo;
    [SerializeField] private List<AmmoData> bigBoxAmmo;

    // Different meshes for small/big boxes (index matches AmmoBoxType).
    [SerializeField] private GameObject[] boxModel;

    #endregion

    #region Unity Callbacks

    private void Start() => SetupBoxModel();

    #endregion

    #region Interaction

    public override void Interaction()
    {
        // Select the appropriate loot table.
        List<AmmoData> currentAmmoList = smallBoxAmmo;

        if (ammoBoxType == AmmoBoxType.BigBox)
            currentAmmoList = bigBoxAmmo;

        foreach (AmmoData ammo in currentAmmoList)
        {
            // Only add ammo to weapons that exist in the player's inventory.
            Weapon weapon = weaponController.HasWeaponInSlots(ammo.weaponType);

            AddBulletsToWeapon(weapon, GetRandomAmmoAmount(ammo));
        }

        ObjectPool.Instance.ReturnObjectToPool(gameObject);
    }

    #endregion

    #region Internal Logic

    private int GetRandomAmmoAmount(AmmoData ammoData)
    {
        // Ensure minAmount is less than or equal to maxAmount
        float min = Mathf.Min(ammoData.minAmount, ammoData.maxAmount);
        float max = Mathf.Max(ammoData.minAmount, ammoData.maxAmount);

        // Unity's int Range is max-exclusive; using float here keeps current behavior (inclusive-ish after rounding).
        float randomAmmoAmount = Random.Range(min, max);

        return Mathf.RoundToInt(randomAmmoAmount);
    }

    private void AddBulletsToWeapon(Weapon weapon, int ammount)
    {
        if (weapon == null)
            return;

        weapon.totalReservedAmmo += ammount;
    }   

    private void SetupBoxModel()
    {
        for (int i = 0; i < boxModel.Length; i++)
        {
            boxModel[i].SetActive(false);

            if (i == (int)ammoBoxType)
            {
                boxModel[i].SetActive(true);

                // Keep base highlighting logic working after model swap.
                UpdateMeshAndMaterial(boxModel[i].GetComponent<MeshRenderer>());
            }
        }
    }

    #endregion
}