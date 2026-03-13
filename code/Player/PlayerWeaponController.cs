using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player weapon runtime logic (shooting, reloading, weapon inventory/slots).
/// 
/// Responsibilities:
/// - Maintain weapon slot inventory and current equipped weapon
/// - Spawn bullets from <see cref="ObjectPool"/> and apply weapon spread
/// - Trigger weapon animations through <see cref="PlayerWeaponVisuals"/>
/// - Handle input for shooting, reload, weapon mode toggle, slot equips, drop
/// 
/// Key connections:
/// - <see cref="PlayerWeaponVisuals"/> handles models, animation layers, IK/rig weights.
/// - <see cref="PlayerAnimationEvents"/> fires animation events that call back into this controller
///   to mark the weapon "ready" and to refill ammo after reload.
/// - <see cref="ObjectPool"/> is used for bullets and dropped weapon pickups.
/// - Weapons are runtime instances (<see cref="Weapon"/>) built from <see cref="WeaponData"/> assets.
/// </summary>
public class PlayerWeaponController : MonoBehaviour
{
    private const float REFERENCE_BULLET_SPEED = 20f;
    // This is the default speed from which our mass formula is derived.

    #region Inspector

    [Header("Starting Weapon")]
    [SerializeField] private WeaponData defaultWeaponData;

    [Header("Bullet details")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletImpactForce = 100;

    [Header("Inventory")]
    [SerializeField] private int maxWeaponSlots = 2;
    [SerializeField] private List<Weapon> weaponSlots;

    [Header("Pickups")]
    [SerializeField] private GameObject weaponPickupPrefab;

    // Present in the scene hierarchy but not currently used in logic (kept for future attachment/organization).
    [SerializeField] private Transform weaponHolder;

    #endregion

    #region Runtime

    private Player player;

    [SerializeField] private Weapon currentWeapon;

    private bool weaponReady;
    private bool isShooting;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        player = GetComponent<Player>();
        AssignInputEvents();

        // Small delay allows other systems (visual rig, models) to initialize before equip animation.
        Invoke(nameof(EquipStartingWeapon), .1f);
    }

    private void Update()
    {
        if (isShooting)
            Shoot();
    }

    #endregion

    #region Slots Management - Pickup, Drop, Equip, Ready Weapon
    private void EquipStartingWeapon()
    {
        // Slot 0 is assumed to exist (configured via Inspector).
        weaponSlots[0] = new Weapon(defaultWeaponData);
        EquipWeapon(0);
    }

    private void EquipWeapon(int i)
    {
        if (i >= weaponSlots.Count)
        {
            Debug.Log("No weapon in this slot!");
            return;
        }

        SetWeaponReady(false);

        currentWeapon = weaponSlots[i];
        // Visual system drives equip animation and will trigger animation events to finish the process.
        player.weaponVisuals.PlayWeaponEquipAnimation();

        // Camera distance is weapon-specific for better readability during gameplay.
        CameraManager.Instance.ChangeCameraDistance(currentWeapon.cameraDistance);
    }

    /// <summary>
    /// Adds a weapon to the inventory, or converts it into ammo if already owned.
    /// </summary>
    public void PickupWeapon(Weapon newWeapon)
    {
        // 1) If the weapon already exists in slots, treat the pickup as ammo (ammo persists across re-pickup).
        if (HasWeaponInSlots(newWeapon.weaponType) != null)
        {
            HasWeaponInSlots(newWeapon.weaponType).totalReservedAmmo += newWeapon.bulletsInMagazine;
            return;
        }

        // 2) If inventory is full, replace current weapon (when picking up a different weapon).
        if (weaponSlots.Count >= maxWeaponSlots && newWeapon.weaponType != currentWeapon.weaponType)
        {
            int weaponIndex = weaponSlots.IndexOf(currentWeapon);

            player.weaponVisuals.SwitchOffWeaponModels();
            weaponSlots[weaponIndex] = newWeapon;

            CreateWeaponOnTheGround();
            EquipWeapon(weaponIndex);
            return;
        }

        // 3) Otherwise, just add it.
        weaponSlots.Add(newWeapon);
        player.weaponVisuals.SwitchOnBackupWeaponModel();
    }

    private void DropWeapon()
    {
        if (HasOnlyOneWeapon())
            return;

        CreateWeaponOnTheGround();

        weaponSlots.Remove(currentWeapon);
        EquipWeapon(0);
    }

    private void CreateWeaponOnTheGround()
    {
        // Dropped weapon is a pooled pickup that stores this runtime Weapon instance (ammo/state preserved).
        GameObject droppedWeapon = ObjectPool.Instance.GetObject(weaponPickupPrefab, transform);
        droppedWeapon.GetComponent<PickupWeapon>()?.SetupPickupWeapon(currentWeapon, transform);
    }

    public void SetWeaponReady(bool ready) => weaponReady = ready;
    public bool WeaponReady() => weaponReady;

    #endregion

    #region Shooting

    private IEnumerator BurstFire()
    {
        SetWeaponReady(false);

        for (int i = 1; i <= currentWeapon.bulletsPerShot; i++)
        {
            FireSingleBullet();
            yield return new WaitForSeconds(currentWeapon.burstFireDelay);

            if (i >= currentWeapon.bulletsPerShot)
                SetWeaponReady(true);
        }
    }

    private void Shoot()
    {
        if (WeaponReady() == false)
            return;

        if (currentWeapon.CanShoot() == false)
        {
            return;
        }

        player.weaponVisuals.PlayFireAnimation();

        // Semi-auto: one shot per click.
        if (currentWeapon.shootType == ShootType.SemiAuto)
            isShooting = false;

        // Burst mode is handled as a coroutine (spreads shots across time).
        if (currentWeapon.IsBurstModeActive() == true)
        {
            StartCoroutine(BurstFire());
            return;
        }

        FireSingleBullet();
        TriggerEnemyDodge(); // Trigger dodge roll of enemies in the bullet's path (called here to sync with fire animation and before spread is applied).
    }

    private void FireSingleBullet()
    {
        currentWeapon.bulletsInMagazine--;

        GameObject newBullet = ObjectPool.Instance.GetObject(bulletPrefab, GunPoint());
        newBullet.transform.rotation = Quaternion.LookRotation(GunPoint().forward);

        Rigidbody rbNewBullet = newBullet.GetComponent<Rigidbody>();

        Bullet bulletScript = newBullet.GetComponent<Bullet>();
        bulletScript.BulletSetup(currentWeapon.gunDistance, bulletImpactForce);

        Vector3 bulletDirections = currentWeapon.ApplySpread(BulletDirection());

        // Mass is inversely proportional to speed so faster bullets don't become "heavier" in physics collisions.
        rbNewBullet.mass = REFERENCE_BULLET_SPEED / bulletSpeed;
        rbNewBullet.linearVelocity = bulletDirections * bulletSpeed;
    }

    #endregion

    #region Reload

    private void Reload()
    {
        SetWeaponReady(false);

        // Refill is applied via animation event to sync with reload timing.
        player.weaponVisuals.PlayReloadAnimation();
    }

    #endregion

    #region Queries / Helpers

    public bool HasOnlyOneWeapon() => weaponSlots.Count <= 1;
    public Weapon HasWeaponInSlots(WeaponType weaponType)
    {
        foreach (Weapon weapon in weaponSlots)
        {
            if (weapon.weaponType == weaponType)
                return weapon;
        }
        return null;
    }

    /// <summary>
    /// Returns the direction bullets should travel based on aim.
    /// In non-precise mode, the Y-axis is flattened unless targeting a <see cref="Target"/>.
    /// </summary>
    public Vector3 BulletDirection()
    {
        Transform aim = player.aim.Aim();

        Vector3 direction = (aim.position - GunPoint().position).normalized;

        if (player.aim.canAimPrecisely() == false && player.aim.Target() == null)
            direction.y = 0;

        return direction;
    }

    public Weapon CurrentWeapon() => currentWeapon;

    /// <summary>
    /// Gun point is owned by the current weapon model in the hierarchy.
    /// </summary>
    public Transform GunPoint() => player.weaponVisuals.CurrentWeaponModel().gunPoint;

    #endregion

    #region Enemy Reactions

    private void TriggerEnemyDodge()
    {
        Vector3 rayOrigin = GunPoint().position;
        Vector3 rayDirection = BulletDirection();

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Mathf.Infinity))
        {
            EnemyMelee enemyMelee = hit.collider.GetComponentInParent<EnemyMelee>();
            if (enemyMelee != null)
                enemyMelee.ActivateDodgeRoll();
        }
    }

    #endregion

    #region Input Events
    private void AssignInputEvents()
    {
        InputSystem_Actions controls = player.controls;
        controls.Player.Attack.performed += context => isShooting = true;
        controls.Player.Attack.canceled += context => isShooting = false;

        controls.Player.EquipSlot1.performed += context => EquipWeapon(0);
        controls.Player.EquipSlot2.performed += context => EquipWeapon(1);
        controls.Player.EquipSlot3.performed += context => EquipWeapon(2);
        controls.Player.EquipSlot4.performed += context => EquipWeapon(3);
        controls.Player.EquipSlot5.performed += context => EquipWeapon(4);

        controls.Player.DropCurrentWeapon.performed += context => DropWeapon();
        controls.Player.Reload.performed += context =>
        {
            // Guarded so we don't restart reload while already reloading.
            if (currentWeapon.CanReload() && WeaponReady())
            {
                Reload();
            }
        };

        controls.Player.ToggleWeaponMode.performed += context => currentWeapon.ToggleBurstMode();
    }

    #endregion
}
