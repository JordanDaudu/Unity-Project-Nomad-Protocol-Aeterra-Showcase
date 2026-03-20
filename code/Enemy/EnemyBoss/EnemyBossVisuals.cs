using UnityEngine;

/// <summary>
/// Boss-specific VFX and readability helpers.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Manage the flamethrower "battery" visuals (charge/discharge over time).</description></item>
/// <item><description>Play telegraph/impact/explosion particle systems for the jump attack.</description></item>
/// <item><description>Toggle weapon trail objects for high-impact actions (attacks, jump attack, ability windup).</description></item>
/// </list>
///
/// Design notes:
/// - Jump attack particle systems are detached from the boss hierarchy so they remain in world space.
///   This prevents the telegraph from moving if the boss moves/rotates after starting the FX.
/// - Battery visuals scale only on the Y axis to simulate draining/recharging.
///
/// Key connections:
/// - Read cooldown and durations from <see cref="EnemyBoss"/>.
/// - Called by boss states (<see cref="AbilityState_Boss"/>, <see cref="AttackState_Boss"/>, <see cref="JumpAttackState_Boss"/>).
/// </remarks>
public class EnemyBossVisuals : MonoBehaviour
{
    #region Dependencies

    private EnemyBoss enemy;

    #endregion

    #region Inspector

    [Header("Batteries Settings")]
    [SerializeField] private GameObject[] batteries;

    [Tooltip("Initial Y scale of each battery object as authored in the prefab.")]
    [SerializeField] private float initialBatteryScaleY = 0.2f;

    [Header("Jump Attack Settings")]
    [SerializeField] private Vector3 landingOffset;
    [SerializeField] private ParticleSystem landingZoneFX;
    [SerializeField] private ParticleSystem impactZoneFX;
    [SerializeField] private ParticleSystem explosionZoneFX;

    [Header("Weapon FX")]
    [SerializeField] private GameObject[] weaponTrails;

    #endregion

    #region Runtime

    private float dischargeSpeed;
    private float rechargeSpeed;
    private bool isRecharging;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        enemy = GetComponent<EnemyBoss>();

        if (landingZoneFX == null || impactZoneFX == null || explosionZoneFX == null)
            Debug.LogError("One or more ParticleSystems for jump attack FX are not assigned in EnemyBossVisuals!");

        // NOTE:
        // Vector3 is a struct and cannot be null. "Unassigned" in inspector means it is left at (0,0,0).
        if (enemy.bossWeaponType == BossWeaponType.Hammer && landingOffset == Vector3.zero)
            Debug.LogError("Landing offset is not assigned in EnemyBossVisuals for Hammer boss!");

        // Detach FX so they stay in world space and don't move with the boss.
        landingZoneFX.transform.parent = null;
        landingZoneFX.Stop();

        impactZoneFX.transform.parent = null;
        impactZoneFX.Stop();

        explosionZoneFX.transform.parent = null;
        explosionZoneFX.Stop();

        ResetBatteries();
    }

    private void Update()
    {
        UpdateBatteriesScale();
    }

    #endregion

    #region Weapon FX

    /// <summary>
    /// Enables/disables all configured weapon trail GameObjects.
    /// </summary>
    /// <remarks>
    /// Kept lowerCamelCase because existing states call this method.
    /// </remarks>
    public void enableWeaponTrail(bool active)
    {
        if (weaponTrails == null || weaponTrails.Length == 0)
        {
            Debug.LogWarning("No weapon trails assigned to EnemyBossVisuals!");
            return;
        }

        foreach (GameObject trail in weaponTrails)
        {
            trail.gameObject.SetActive(active);
        }
    }

    #endregion

    #region Battery Visuals

    /// <summary>
    /// Resets battery visuals to full and begins the recharge mode.
    /// </summary>
    /// <remarks>
    /// Recharge speed is derived from the boss ability cooldown.
    /// Discharge speed is derived from flamethrower duration.
    /// </remarks>
    public void ResetBatteries()
    {
        isRecharging = true;

        rechargeSpeed = initialBatteryScaleY / enemy.abilityCooldown;
        dischargeSpeed = initialBatteryScaleY / (enemy.flameThrowerDuration * .75f);

        foreach (GameObject battery in batteries)
        {
            battery.SetActive(true);
        }
    }

    /// <summary>
    /// Switches batteries into discharge mode (used when flamethrower activates).
    /// </summary>
    public void DischargeBatteries() => isRecharging = false;

    private void UpdateBatteriesScale()
    {
        if (batteries.Length <= 0)
        {
            if (enemy.bossWeaponType == BossWeaponType.FlameThrower)
                Debug.LogWarning("No batteries assigned to EnemyBossVisuals!");
            return;
        }

        foreach (GameObject battery in batteries)
        {
            if (battery.activeSelf)
            {
                float scaleChange = (isRecharging ? rechargeSpeed : -dischargeSpeed) * Time.deltaTime;
                float newScaleY = Mathf.Clamp(battery.transform.localScale.y + scaleChange, 0f, initialBatteryScaleY);

                battery.transform.localScale = new Vector3(battery.transform.localScale.x, newScaleY, battery.transform.localScale.z);

                if (newScaleY <= 0f)
                {
                    battery.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region Jump Attack FX

    public void PlayLandingZoneFX(Vector3 target)
    {
        Vector3 predictedImpactZone = GetPredictedImpactPosition(target);

        landingZoneFX.transform.position = predictedImpactZone;
        landingZoneFX.Clear();

        var mainModule = landingZoneFX.main;
        mainModule.startLifetime = enemy.travelTimeToTarget * 2f; // Longer than travel time so it stays visible until landing.

        landingZoneFX.Play();
    }

    public void PlayOnLandingFX(Vector3 target)
    {
        PlayImpactZoneFX(target);
        PlayExplosionZoneFX(target);
    }

    private void PlayImpactZoneFX(Vector3 target)
    {
        impactZoneFX.transform.position = target;
        impactZoneFX.Clear();
        impactZoneFX.Play();
    }

    private void PlayExplosionZoneFX(Vector3 target)
    {
        explosionZoneFX.transform.position = target;
        explosionZoneFX.Clear();
        explosionZoneFX.Play();
    }

    /// <summary>
    /// Computes a predicted world position for impact FX.
    /// </summary>
    /// <remarks>
    /// For hammer bosses, the boss may land with an offset relative to its root target.
    /// This keeps the telegraph (landing zone) aligned with the eventual impact location.
    /// </remarks>
    private Vector3 GetPredictedImpactPosition(Vector3 rootLandingTarget)
    {
        if (enemy.bossWeaponType != BossWeaponType.Hammer)
            return rootLandingTarget;

        if (landingOffset == Vector3.zero)
            return rootLandingTarget;

        Vector3 direction = rootLandingTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return rootLandingTarget;

        Quaternion predictedRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        Vector3 worldOffset = predictedRotation * landingOffset;
        worldOffset.y = 0f;

        return rootLandingTarget + worldOffset;
    }

    #endregion
}