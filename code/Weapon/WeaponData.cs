using UnityEngine;

/// <summary>
/// Static weapon configuration (ScriptableObject).
/// 
/// This asset defines "default" values for a weapon type (fire rate, spread, magazine sizes, etc.).
/// At runtime, the game creates a mutable <see cref="Weapon"/> instance from this data.
/// 
/// Important:
/// - Avoid modifying these fields at runtime. Runtime state (ammo, burst active, etc.) lives in <see cref="Weapon"/>.
/// </summary>
[CreateAssetMenu(fileName = "New Weapon Data", menuName = "ScriptableObjects/Weapon Data", order = 1)]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public WeaponType weaponType;

    [Header("Magazine details")]
    public int bulletsInMagazine;
    public int magazineCapacity;
    public int totalReservedAmmo;

    [Header("Regular Shot")]
    public ShootType shootType;
    public int bulletsPerShot = 1; // Number of bullets fired in one shot
    [Min(0f)] public float fireRate;

    [Header("Burst Shot")]
    public bool burstAvailable; // Whether burst fire mode is available for this weapon
    public bool burstActive; // Whether burst fire mode is currently active

    public int burstBulletsPerShot; // Number of bullets fired in one burst
    public float burstFireRate; // Bullets fired per second in burst mode
    public float burstFireDelay = 0.1f; // Delay between shots in a burst

    [Header("Weapon Spread")]
    public float baseSpread = 1; // The inherent spread of the weapon
    public float maximumSpread = 3;

    public float spreadIncreaseRate = 0.15f; // How quickly the spread increases with each shot

    [Header("Weapon Specifics")]
    [Range(1f, 3f)]
    public float reloadSpeed = 1f; // Time it takes to reload the weapon
    [Range(1f, 3f)]
    public float equipmentSpeed = 1f; // Time it takes to equip the weapon
    [Range(4f, 8f)]
    public float gunDistance = 4f;
    [Range(4f, 8f)]
    public float cameraDistance = 6f;
}