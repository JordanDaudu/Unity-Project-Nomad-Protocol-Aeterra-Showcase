using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines how this enemy is allowed to interact with the cover system.
/// </summary>
public enum CoverPerk
{
    /// <summary>This enemy never uses cover.</summary>
    None,

    /// <summary>This enemy may take cover once when entering battle, but will not reposition later.</summary>
    TakeCoverOnce,

    /// <summary>This enemy may take cover initially and later reposition to better cover.</summary>
    Reposition
}

public enum UnstoppablePerk
{
    None,
    Unstoppable
}

public enum GrenadePerk
{
    None,
    CanThrowGrenade
}

/// <summary>
/// Ranged enemy implementation.
/// 
/// In addition to the shared enemy functionality provided by <see cref="Enemy"/>,
/// this class is responsible for:
/// - initializing ranged combat states
/// - selecting a weapon configuration
/// - firing projectiles
/// - acting as the bridge between high-level enemy state logic and the reusable cover system
/// 
/// Cover logic itself is not implemented directly here. Instead, this class delegates
/// all tactical cover evaluation and reservation work to <see cref="EnemyCoverController"/>.
/// This keeps the class focused on combat/state behavior while still exposing a simple API
/// for states such as <see cref="RunToCoverState_Range"/> and <see cref="BattleState_Range"/>.
/// </summary>
[RequireComponent(typeof(EnemyCoverController))]
public class EnemyRange : Enemy
{
    [Header("Enemy Perks")]

    [Tooltip("Controls whether this enemy cannot use cover, can only take cover once, or may also reposition to better cover later.")]
    [SerializeField] private CoverPerk coverPerk = CoverPerk.Reposition;

    [Tooltip("If this enemy has the Unstoppable perk, they will keep advancing no matter what.")]
    [SerializeField] private UnstoppablePerk unstoppablePerk;

    [Tooltip("If this enemy has the CanThrowGrenade perk, they will occasionally throw grenades at the player during battle.")]
    [SerializeField] private GrenadePerk grenadePerk;

    [Header("Cover System")]
    /// <summary>
    /// Stores the last reserved cover transform returned by the cover controller.
    /// 
    /// This is mainly a convenience reference for states and debugging.
    /// In practice, this will usually point to the currently reserved <see cref="CoverPoint"/>.
    /// </summary>
    private Transform lastCover;

    /// <summary>
    /// Reusable component responsible for finding, evaluating, reserving,
    /// and releasing cover points for this enemy.
    /// </summary>
    private EnemyCoverController coverController;

    [Header("Advance Perk")]
    [Tooltip("Movement speed used while advancing toward the player or their last known position." +
        "\nUnstoppable Perk when active overwrite this to UnstoppableAdvanceSpeed (const) everytime")]
    public float advanceSpeed;
    
    private const float UnstoppableAdvanceSpeed = 1;

    [Tooltip("How close the enemy needs to be to a visible player before stopping advance and switching to battle.")]
    public float advanceStoppingDistance = 7f;

    [Tooltip("How close the enemy must get to the player's last known position before considering that position reached.")]
    public float lastKnownPositionArrivalDistance = 0.75f;

    [Tooltip("Delay after leaving Advance before the enemy is allowed to look for a better cover spot again.")]
    public float advanceDuration = 2.5f;

    [Header("Grenade Perk")]
    [Tooltip("Prefab of the grenade projectile this enemy will throw when using the grenade perk.")]
    [SerializeField] private GameObject grenadePrefab;

    [Tooltip("The point from which grenades are thrown. This should ideally be a child transform positioned at the enemy's hand or weapon muzzle for visual consistency.")]
    [SerializeField] private Transform grenadeStartPoint;

    [Tooltip("The force with which the grenade impacts the surrounding area upon explosion. This can be used to control the knockback effect on the player and other objects hit by the explosion.")]
    [SerializeField] private float impactPower;

    [Tooltip("Time between the grenade being thrown and landing (timeToTarget) and it exploding at the target position. This should be tuned together with the timeToTarget to ensure grenades explode at the right moment for player evasion to be possible.")]
    [SerializeField] private float explosionTimer = 0.75f;

    [Tooltip("Time it takes for a thrown grenade to reach the target position.")]
    [SerializeField] private float timeToTarget = 1.2f;

    [Tooltip("Minimum distance required before this enemy is allowed to throw a grenade. Prevents unsafe close-range throws.")]
    [SerializeField] private float grenadeMinThrowDistance = 5f;

    [Tooltip("Maximum distance at which this enemy is allowed to throw a grenade. Prevents throwing at targets that are too far away.")]
    [SerializeField] private float grenadeMaxThrowDistance = 15f;

    [Tooltip("How long after last seeing the player the enemy is still allowed to throw a grenade at their known position.")]
    [SerializeField] private float grenadeKnowledgeWindow = 2.5f;

    [Tooltip("Cooldown between grenade throws.")]
    public float grenadeCooldown;

    private float lastTimeThrewGrenade = -10;

    [Header("Weapon Details")]
    public EnemyRangeWeaponType weaponType;
    public EnemyRangeWeaponData weaponData;
    [Space]
    public float attackDelay;
    public Transform gunPoint;
    public Transform weaponHolder;
    public GameObject bulletPrefab;

    [Header("Aim Details")]
    [SerializeField] private float hiddenAimSpeed = 4f;
    [SerializeField] private float visibleAimSpeed = 20f;
    [SerializeField] private float aimLockRadius = 1.25f;
    [SerializeField] private float requiredAimLockTime = 0.12f;
    [SerializeField] private Transform aim;
    [SerializeField] private Transform playerBody;
    [SerializeField] private List<EnemyRangeWeaponData> availableWeaponData;

    private float timeOnTarget;
    private Transform aimOriginalParent;
    private Vector3 aimOriginalLocalPosition;

    #region States

    public IdleState_Range idleState { get; private set; }
    public MoveState_Range moveState { get; private set; }
    public BattleState_Range battleState { get; private set; }
    public RunToCoverState_Range runToCoverState { get; private set; }
    public AdvanceToPlayer_Range advanceToPlayerState { get; private set; }
    public ThrowGrenadeState_Range throwGrenadeState { get; private set; }
    public DeadState_Range deadState { get; private set; }

    #endregion

    public EnemyCoverController CoverController => coverController;
    public Transform Aim => aim;

    /// <summary>Returns true when this enemy is allowed to take cover at all.</summary>
    public bool CanUseCovers => coverPerk != CoverPerk.None && coverController != null;

    /// <summary>Returns true only if this enemy is allowed to reposition after already taking cover.</summary>
    public bool CanRepositionCover => coverPerk == CoverPerk.Reposition && coverController != null;

    /// <summary>Returns true if this enemy will ignore all cover logic and keep advancing toward the player.</summary>
    public bool IsUnstoppable => unstoppablePerk == UnstoppablePerk.Unstoppable;

    /// <summary>Returns the transform of the currently reserved cover point, if any.</summary>
    public Transform CurrentCoverTransform => coverController != null ? coverController.CurrentCoverTransform : null;

    /// <summary>
    /// Returns true when enough time has passed since the enemy last left Advance state,
    /// allowing cover re-evaluation to happen again.
    /// </summary>
    public bool CanSearchForCoverAfterAdvance => Time.time >= advanceToPlayerState.LastTimeAdvanced + advanceDuration;

    protected override void Awake()
    {
        base.Awake();

        idleState = new IdleState_Range(this, stateMachine, "Idle");
        moveState = new MoveState_Range(this, stateMachine, "Move");
        battleState = new BattleState_Range(this, stateMachine, "Battle");
        runToCoverState = new RunToCoverState_Range(this, stateMachine, "RunToCover");
        advanceToPlayerState = new AdvanceToPlayer_Range(this, stateMachine, "Advance");
        throwGrenadeState = new ThrowGrenadeState_Range(this, stateMachine, "ThrowGrenade");
        deadState = new DeadState_Range(this, stateMachine, "Idle"); // Idle is a placeholder, we using ragdoll

        coverController = GetComponent<EnemyCoverController>();
    }

    protected override void Start()
    {
        base.Start();

        aimOriginalParent = aim != null ? aim.parent : null;
        aimOriginalLocalPosition = aim != null ? aim.localPosition : Vector3.zero;

        if (playerBody == null)
        {
            Player playerComponent = player.GetComponent<Player>();
            playerBody = playerComponent != null ? playerComponent.playerBody : player;
        }

        perception?.SetTarget(player, playerBody);

        InitializeSpeciality();

        stateMachine.Initialize(idleState);

        visuals.SetupLook();
        SetupWeapon();

        if (CanUseCovers)
            coverController.RefreshNearbyCovers(true);
    }

    protected override void Update()
    {
        base.Update();

        stateMachine.currentState.Update();
    }

    protected override void InitializeSpeciality()
    {
        if (IsUnstoppable)
        {
            advanceSpeed = UnstoppableAdvanceSpeed;
            anim.SetFloat("AdvanceAnimIndex", 1); // Animator uses this to select between regular advance and unstoppable advance animation sets.
        }
    }

    public override void GetHit()
    {
        base.GetHit();

        if (currentHealth <= 0 && stateMachine.currentState != deadState)
            stateMachine.ChangeState(deadState);
    }

    /// <summary>
    /// Enters battle mode and decides which state should handle combat behavior.
    /// 
    /// If cover usage is enabled and available, the enemy first transitions into
    /// <see cref="RunToCoverState_Range"/> to reserve and move to a cover point.
    /// Otherwise it goes directly into <see cref="BattleState_Range"/>.
    /// </summary>
    public override void EnterBattleMode()
    {
        if (inBattleMode)
        {
            Debug.LogWarning($"[{name}] Attempted to enter battle mode while already in battle mode. Ignoring.", this);
            return;
        }

        StopAgentImmediately();

        base.EnterBattleMode();

        if (CanUseCovers)
            stateMachine.ChangeState(runToCoverState);
        else
            stateMachine.ChangeState(battleState);
    }

    public override void ExitBattleMode()
    {
        base.ExitBattleMode();
        ReleaseReservedCover();
        RestoreAimToOwner();
        timeOnTarget = 0f;
    }

    protected override void OnResetEnemyStateMachineForReuse()
    {
        RestoreAimToOwner();
        ReleaseReservedCover();

        if (stateMachine.currentState != idleState)
            stateMachine.Initialize(idleState);
    }
    protected override void ResetEnemyForReuse()
    {
        base.ResetEnemyForReuse();

        InitializeSpeciality();
    }

    public void DetachAimFromHierarchy()
    {
        if (aim != null && aim.parent != null)
            aim.parent = null;
    }

    public void RestoreAimToOwner()
    {
        if (aim == null)
            return;

        if (aimOriginalParent != null)
        {
            aim.SetParent(aimOriginalParent);
            aim.localPosition = aimOriginalLocalPosition;
        }
    }

    public bool CanThrowGrenade()
    {
        if (grenadePerk == GrenadePerk.None)
            return false;

        if (!HasRecentTargetKnowledge())
            return false;

        if (perception != null && perception.TimeSinceLastSeen > grenadeKnowledgeWindow)
            return false;

        // float distanceToKnownTarget = Vector3.Distance(GetKnownPlayerPosition(), transform.position);

        // For simplicity, we'll just use the player's current position for distance checks
        float distanceToKnownTarget = Vector3.Distance(player.transform.position, transform.position);

        if (distanceToKnownTarget < grenadeMinThrowDistance)
            return false;

        if (distanceToKnownTarget > grenadeMaxThrowDistance)
            return false;

        return Time.time >= lastTimeThrewGrenade + grenadeCooldown;
    }

    public void ThrowGrenade()
    {
        lastTimeThrewGrenade = Time.time;
        visuals.EnableGrenadeModel(false);

        GameObject newGrenade = ObjectPool.Instance.GetObject(grenadePrefab, grenadeStartPoint);

        EnemyGrenade newGrenadeScript = newGrenade.GetComponent<EnemyGrenade>();


        // If the enemy is already dead when the grenade throw action executes (e.g., due to a delayed throw or simultaneous death),
        // we should still allow the grenade to be thrown at the enemy's current position to avoid wasted throws and maintain consistent behavior.
        if (stateMachine.currentState == deadState)
        {
            newGrenadeScript.SetupGrenade(transform.position, 1, explosionTimer, impactPower);
            return;
        }

        newGrenadeScript.SetupGrenade(player.transform.position, timeToTarget, explosionTimer, impactPower); // Using player's current position for simplicity and reaction time, but could be changed to use last known position if desired.
    }

    #region Cover API Wrappers

    /// <summary>
    /// Asks the cover controller to find and reserve the best available cover point
    /// against the current player position.
    /// 
    /// This is the main entry point used by <see cref="RunToCoverState_Range"/>.
    /// If cover cannot be used, or the player reference is missing, null is returned.
    /// 
    /// On success, <see cref="lastCover"/> is updated to the reserved point transform.
    /// </summary>
    public Transform AttemptToFindCover()
    {
        // If cover usage is disabled or the player reference is missing, ensure no cover is reserved and return null.
        if (!CanUseCovers || player == null)
        {
            lastCover = null;
            return null;
        }

        Debug.Log($"[{name}] Requesting cover controller to find cover. Player position: {player.position}", this);

        lastCover = coverController.AttemptToFindCover(player);
        return lastCover;
    }

    /// <summary>
    /// Attempts to reserve the best cover point against the player and returns it through an out parameter.
    /// 
    /// This is a slightly more explicit version of <see cref="AttemptToFindCover"/> that also returns
    /// whether the reservation succeeded.
    /// 
    /// On success:
    /// - the cover controller reserves the selected point
    /// - <see cref="lastCover"/> is updated
    /// - <paramref name="reservedCover"/> receives the reserved point transform
    /// </summary>
    public bool TryReserveBestCover(out Transform reservedCover)
    {
        reservedCover = null;

        if (!CanUseCovers || player == null)
        {
            lastCover = null;
            return false;
        }

        bool success = coverController.TryReserveBestCover(player, out CoverPoint reservedPoint);

        lastCover = success && reservedPoint != null ? reservedPoint.transform : null;
        reservedCover = lastCover;

        return success;
    }

    /// <summary>
    /// Periodically asks the cover controller whether a meaningfully better cover point exists.
    /// 
    /// This is mainly used during <see cref="BattleState_Range"/> so the enemy can occasionally
    /// reposition instead of staying forever in the same spot.
    /// 
    /// A reposition only succeeds if the controller determines that:
    /// - the current cover became invalid, or
    /// - another cover point is better by at least the configured improvement threshold
    /// 
    /// On success:
    /// - the new point is immediately reserved
    /// - <see cref="lastCover"/> is updated
    /// - <paramref name="reservedCover"/> receives the new cover transform
    /// </summary>
    public bool TryReserveBetterCover(out Transform reservedCover)
    {
        reservedCover = null;

        if (!CanRepositionCover || player == null)
            return false;

        bool success = coverController.TryReserveBetterCover(player, out CoverPoint reservedPoint);

        if (!success || reservedPoint == null)
            return false;

        lastCover = reservedPoint.transform;
        reservedCover = lastCover;
        return true;
    }

    /// <summary>
    /// Releases the currently reserved cover point, if one exists.
    /// 
    /// This should be called when the enemy no longer intends to hold that cover slot,
    /// such as on death, disable, or a major state change where cover is no longer relevant.
    /// </summary>
    public void ReleaseReservedCover()
    {
        if (coverController != null)
            coverController.ReleaseCurrentCover();

        lastCover = null;
    }

    #endregion

    public void FireSingleBullet()
    {
        anim.SetTrigger("Shoot");

        Vector3 bulletDirection = (GetCurrentAimTargetPosition() - gunPoint.position).normalized;
        Vector3 bulletDirectionWithSpread = weaponData.ApplyWeaponSpread(bulletDirection);

        GameObject newBullet = ObjectPool.Instance.GetObject(bulletPrefab, gunPoint);
        newBullet.transform.rotation = Quaternion.LookRotation(bulletDirectionWithSpread);

        newBullet.GetComponent<EnemyBullet>().BulletSetup();

        Rigidbody rbNewBullet = newBullet.GetComponent<Rigidbody>();
        rbNewBullet.mass = 20 / weaponData.BulletSpeed;
        rbNewBullet.linearVelocity = bulletDirectionWithSpread * weaponData.BulletSpeed;
    }

    private void SetupWeapon()
    {
        List<EnemyRangeWeaponData> filteredData = new List<EnemyRangeWeaponData>();

        foreach (EnemyRangeWeaponData currentWeaponData in availableWeaponData)
        {
            if (currentWeaponData.WeaponType == weaponType)
                filteredData.Add(currentWeaponData);
        }

        if (filteredData.Count > 0)
        {
            int randomIndex = Random.Range(0, filteredData.Count);
            weaponData = filteredData[randomIndex];
        }
        else
            Debug.LogWarning("No weapon data found for the specified weapon type: " + weaponType);

        // Set the gun point based on the current weapon model
        if (visuals.currentWeaponModel != null && visuals.currentWeaponModel.TryGetComponent(out EnemyRangeWeaponModel rangeWeaponModel))
            gunPoint = rangeWeaponModel.GunPoint;
        else
            Debug.LogWarning("Failed to get GunPoint from current weapon model.", this);
    }

    #region Aim

    public void UpdateAimPosition()
    {
        if (aim == null)
            return;

        Vector3 targetPosition = GetCurrentAimTargetPosition();
        float aimSpeed = CanSeePlayer() ? visibleAimSpeed : hiddenAimSpeed;

        aim.position = Vector3.MoveTowards(aim.position, targetPosition, aimSpeed * Time.deltaTime);

        if (Vector3.Distance(aim.position, targetPosition) <= aimLockRadius)
            timeOnTarget += Time.deltaTime;
        else
            timeOnTarget = 0f;
    }

    public Vector3 GetCurrentAimTargetPosition()
    {
        if (perception != null && perception.HadVisualContact)
            return perception.KnownTargetPosition;

        if (playerBody != null)
            return playerBody.position;

        return player.position;
    }

    public bool IsAimOnPlayer()
    {
        return Vector3.Distance(aim.position, GetCurrentAimTargetPosition()) <= aimLockRadius;
    }

    public bool CanFireAtPlayer()
    {
        return CanSeePlayer() && IsAimOnPlayer() && timeOnTarget >= requiredAimLockTime;
    }

    #endregion
}
