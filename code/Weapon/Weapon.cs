using UnityEngine;

/// <summary>
/// Weapon archetype identifiers.
/// Used for inventory matching, model selection, and config lookup.
/// </summary>
public enum WeaponType
{
    Pistol,
    Revolver,
    AutoRifle,
    Shotgun,
    Rifle
}

public enum  ShootType
{
    SemiAuto,
    Auto
}

/// <summary>
/// Runtime weapon instance built from a <see cref="WeaponData"/> asset.
/// 
/// Key concept:
/// - <see cref="WeaponData"/> is static configuration.
/// - <see cref="Weapon"/> is mutable runtime state (ammo counts, burst toggles, spread state, timers).
/// 
/// This class is marked Serializable so it can be inspected in the Unity Inspector
/// when referenced by a MonoBehaviour (e.g., PlayerWeaponController.currentWeapon).
/// </summary>
[System.Serializable] // Makes the class visible in the Unity Inspector

public class Weapon
{
    #region Identity / Config Reference

    public WeaponType weaponType;
    /// <summary>
    /// Back-reference to the data asset this weapon was constructed from.
    /// Used when dropping/picking up weapons so we can rebuild visuals by weapon type.
    /// </summary>
    public WeaponData weaponData { get; private set; }

    #endregion

    #region Regular shot variables
    public ShootType shootType;
    /// <summary>
    /// Number of bullets fired per shot.
    /// In burst mode, this is overwritten to match burst bullets per shot.
    /// </summary>
    public int bulletsPerShot { get; private set; }

    private float defaultFireRate = 1; // Default "regular" fire rate so we can restore after turning burst off.
    public float fireRate = 1; // Bullets fired per second
    private float lastFireTime; // Time when the weapon was last fired
    #endregion
    #region Burst fire variables
    public bool burstActive; // Whether burst fire mode is currently active
    private bool burstAvailable; // Whether burst fire mode is available for this weapon

    private int burstBulletsPerShot = 3; // Number of bullets fired in one burst
    private float burstFireRate = 0.5f; // Bullets fired per second in burst mode
    public float burstFireDelay { get; private set; } // Delay between shots in a burst
    #endregion

    [Header("Magazine details")]
    #region Ammo variables
    public int bulletsInMagazine;
    public int magazineCapacity;
    public int totalReservedAmmo;
    #endregion

    #region Weapon specifics
    public float reloadSpeed { get; private set; } // Time it takes to reload the weapon
    public float equipmentSpeed { get; private set; } // Time it takes to equip the weapon
    public float gunDistance { get; private set; }
    public float cameraDistance { get; private set; }
    #endregion
    #region Weapon Spread variables
    private float baseSpread = 1; // The inherent spread of the weapon
    private float maximumSpread = 3;
    private float currentSpread = 1;

    private float spreadIncreaseRate = 0.15f; // How quickly the spread increases with each shot

    private float lastSpreadUpdateTime; // Time when the spread was last updated
    private float spreadCooldown = 1f; // Time after which the spread starts to decrease
    #endregion

    #region Construction

    public Weapon(WeaponData weaponData)
    {
        bulletsInMagazine = weaponData.bulletsInMagazine;
        magazineCapacity = weaponData.magazineCapacity;
        totalReservedAmmo = weaponData.totalReservedAmmo;

        weaponType = weaponData.weaponType;
        shootType = weaponData.shootType;

        bulletsPerShot = weaponData.bulletsPerShot;
        fireRate = weaponData.fireRate;

        burstAvailable = weaponData.burstAvailable;
        burstActive = weaponData.burstActive;
        burstBulletsPerShot = weaponData.burstBulletsPerShot;
        burstFireRate = weaponData.burstFireRate;
        burstFireDelay = weaponData.burstFireDelay;

        baseSpread = weaponData.baseSpread;
        maximumSpread = weaponData.maximumSpread;
        spreadIncreaseRate = weaponData.spreadIncreaseRate;

        reloadSpeed = weaponData.reloadSpeed;
        equipmentSpeed = weaponData.equipmentSpeed;
        gunDistance = weaponData.gunDistance;
        cameraDistance = weaponData.cameraDistance;

        defaultFireRate = fireRate;

        this.weaponData = weaponData;
    }

    #endregion

    #region Spread methods
    public Vector3 ApplySpread(Vector3 originalDirection)
    {
        UpdateSpread();

        float randomizeSpread = Random.Range(-currentSpread, currentSpread);

        Quaternion spreadRotation = Quaternion.Euler(randomizeSpread, randomizeSpread, randomizeSpread);

        return spreadRotation * originalDirection;
    }

    private void UpdateSpread()
    {
        // If enough time passed since last shot, reset spread to base.
        if (Time.time >= lastSpreadUpdateTime + spreadCooldown)
            currentSpread = baseSpread;
        else
            IncreaseSpread();

        lastSpreadUpdateTime = Time.time;
    }

    private void IncreaseSpread() => currentSpread = Mathf.Clamp(currentSpread + spreadIncreaseRate, baseSpread, maximumSpread);
    #endregion

    #region Burst methods
    public bool IsBurstModeActive()
    {
        if (weaponType == WeaponType.Shotgun)
        {
            burstFireDelay = 0; // Shotguns fire all pellets at once, so we set the fire rate delay to 0 to indicate no delay between shots
            return true; // Shotguns are always in burst mode
        }

        return burstActive;
    }

    public void ToggleBurstMode()
    {
        if (burstAvailable)
            burstActive = !burstActive;

        if (burstActive)
        {
            bulletsPerShot = burstBulletsPerShot;
            fireRate = burstFireRate;
        }
        else
        {
            bulletsPerShot = 1; // Reset to default for semi-auto or auto mode
            fireRate = defaultFireRate; // Reset to default fire rate
        }
    }

    #endregion

    #region Firing

    public bool CanShoot() => HaveEnoughBullets() && ReadyToFire();

    private bool ReadyToFire()
    {
        // Uses a time gate based on fireRate (shots per second).
        if (Time.time >= lastFireTime + (1f / fireRate))
        {
            lastFireTime = Time.time;
            return true;
        }
        return false;
    }

    private bool HaveEnoughBullets() => bulletsInMagazine > 0;

    #endregion

    #region Reload methods
    public bool CanReload()
    {
        if (bulletsInMagazine == magazineCapacity)
            return false; // Magazine is already full

        if (totalReservedAmmo > 0)
        {
            return true;
        }
        return false;
    }
    public void RefillBullets()
    {
        // Return remaining bullets in magazine to reserve before reloading.
        totalReservedAmmo += bulletsInMagazine; // This will add the remaining bullets in the magazine back to the reserve ammo

        int bulletsToReload = magazineCapacity;

        if (bulletsToReload > totalReservedAmmo)
            bulletsToReload = totalReservedAmmo;

        totalReservedAmmo -= bulletsToReload;
        bulletsInMagazine = bulletsToReload;

        // Safety clamp (should not happen, but protects against bad configs).
        if (totalReservedAmmo < 0)
            totalReservedAmmo = 0;
    }
    #endregion
}