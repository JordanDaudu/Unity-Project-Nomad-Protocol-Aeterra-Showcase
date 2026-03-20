using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Base enemy type shared by all enemy variants.
///
/// Owns:
/// - Core references: Animator, NavMeshAgent, Player Transform
/// - Shared combat state: health, battle mode
/// - Shared helpers: facing player / steering target, patrol points
/// - Animation-event relays: manual movement/rotation toggles, trigger forwarding to FSM states
/// - Pool reset contract via <see cref="IPoolable"/>
///
/// Key connections:
/// - Specific enemies (e.g., <see cref="EnemyMelee"/>) create and drive an <see cref="EnemyStateMachine"/>.
/// - <see cref="EnemyAnimationEvents"/> calls into this class, which forwards events to the current state.
/// - <see cref="Bullet"/> calls <see cref="GetHit"/> and <see cref="DeathImpact"/>.
/// - Death visuals are composed via <see cref="EnemyRagdoll"/> + <see cref="EnemyDeathDissolve"/>.
/// </summary>
[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, IPoolable
{
    #region Health

    [Tooltip("Maximum health this enemy has when fully alive.")]
    [SerializeField] protected int maxHealth = 20;

    [Tooltip("Current runtime health. Useful for debugging in play mode.")]
    [SerializeField] protected int currentHealth;

    #endregion

    #region AI Tuning

    [Header("Idle data")]

    [Tooltip("How long the enemy stays idle before choosing its next non-combat action, such as patrolling.")]
    public float idleTime;

    [Tooltip("Combat distance threshold. If the player is farther than this, the enemy may stop holding its current combat position and advance instead.")]
    public float combatRange;

    [Header("Move data")]

    [Tooltip("Movement speed used for slow navigation, usually patrol, search, or cautious movement.")]
    public float walkSpeed = 1.5f;

    [Tooltip("Movement speed used for fast navigation, usually chasing or running to cover.")]
    public float runSpeed = 3f;

    [Tooltip("How quickly the enemy rotates toward a target on the Y axis. Higher values turn faster.")]
    public float turnSpeed;

    #endregion

    #region Patrol

    [Tooltip("Patrol points visited in order while the enemy is out of combat. These transforms are hidden at runtime after their positions are cached.")]
    [SerializeField] private Transform[] patrolPoints;
    private Vector3[] patrolPointsPosition;
    private int currentPatrolIndex;

    #endregion

    #region Animation Flags (Driven by Animation Events)

    private bool manualMovement; // When true, the current state will apply positional movement in Update (instead of NavMeshAgent).
    private bool manualRotation; // When true, the current state will apply rotation toward target in Update.

    #endregion

    #region Cached References
    public EnemyRagdoll ragdoll { get; private set; }
    public EnemyDeathDissolve deathDissolve { get; private set; }
    public Transform player {  get; private set; }
    public Animator anim { get; private set; }
    public NavMeshAgent agent { get; private set; }
    public EnemyStateMachine stateMachine { get; private set; }
    public EnemyPerception perception { get; private set; }

    [Header("Enemy Visuals")]
    public EnemyVisuals visuals { get; private set; }

    #endregion

    #region Runtime State
    /// <summary>
    /// When true, enemy has entered combat behavior (chasing/attacking) instead of patrol.
    /// </summary>
    public bool inBattleMode { get; private set; }

    public bool IsDead => isDead;
    private bool isDead;

    #endregion

    #region Events

    public event Action<Enemy> BattleModeEntered;
    public event Action<Enemy> BattleModeExited;
    public event Action<Enemy> Died;

    #endregion

    #region Unity Callbacks

    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        stateMachine = new EnemyStateMachine(); // Each enemy owns a dedicated FSM instance.

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        // NOTE:
        // This assumes there is a GameObject named "Player" in the scene.
        // In the future, should be replaced with a more robust player reference system
        player = GameObject.Find("Player").GetComponent<Transform>();

        ragdoll = GetComponent<EnemyRagdoll>();
        deathDissolve = GetComponent<EnemyDeathDissolve>();
        visuals = GetComponent<EnemyVisuals>();
        perception = GetComponent<EnemyPerception>();
    }

    protected virtual void Start()
    {
        InitializePatrolPoints();
        perception?.SetTarget(player);
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        perception?.TickPerception(inBattleMode);

        // Base enemy does not tick the state machine directly.
        // Subclasses (EnemyMelee, etc.) call: stateMachine.currentState.Update();
        if (ShouldEnterBattleMode())
            EnterBattleMode();
    }

    protected virtual void InitializeSpeciality()
    {
        // Optional override for specific enemy types to initialize speciality
    }

    #endregion

    #region Combat State

    /// <summary>
    /// Checks if the player is within aggresionRange and enters battle mode once.
    /// </summary>
    protected bool ShouldEnterBattleMode()
    {
        if (isDead)
            return false;

        if (inBattleMode)
            return false;

        if (perception != null)
            return perception.IsTargetVisible;

        return IsPlayerInCombatRange();
    }

    public virtual void EnterBattleMode()
    {
        if (isDead || inBattleMode)
            return;

        inBattleMode = true;
        BattleModeEntered?.Invoke(this);
    }

    public virtual void ExitBattleMode()
    {
        if (!inBattleMode)
            return;

        inBattleMode = false;
        BattleModeExited?.Invoke(this);
    }

    /// <summary>
    /// Called when the enemy is hit by a bullet.
    /// Refreshes target memory so the enemy can properly re-engage the attacker.
    /// </summary>
    public virtual void GetHit()
    {
        if (isDead)
            return;

        if (perception != null && player != null)
        {
            Vector3 knownThreatPosition = player.position;

            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null && playerComponent.playerBody != null)
                knownThreatPosition = playerComponent.playerBody.position;

            perception.RegisterTargetKnowledge(knownThreatPosition);
        }

        EnterBattleMode();
        currentHealth--;
    }

    public void NotifyDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (inBattleMode)
        {
            inBattleMode = false;
            BattleModeExited?.Invoke(this);
        }

        Died?.Invoke(this);
    }

    /// <summary>
    /// Applies a delayed ragdoll impact to the specified rigidbody at hit point.
    /// This delay lets the ragdoll enable/settle for a frame before receiving impulse.
    /// </summary>
    public virtual void DeathImpact( Vector3 force,Vector3 hitPoint,Rigidbody rb)
    {
        StartCoroutine(DeathImpactCourutine(force,hitPoint,rb));
    }
    private IEnumerator DeathImpactCourutine(Vector3 force, Vector3 hitPoint, Rigidbody rb)
    {
        yield return new WaitForSeconds(.1f);

        rb.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
    }

    public bool IsPlayerInCombatRange() => Vector3.Distance(transform.position, player.position) < combatRange;
    public bool CanSeePlayer() => perception != null ? perception.IsTargetVisible : IsPlayerInCombatRange();
    public bool HasRecentTargetKnowledge() => perception == null || perception.HasTargetKnowledge;

    public Vector3 GetKnownPlayerPosition()
    {
        if (perception != null && perception.HadVisualContact)
            return perception.KnownTargetPosition;

        return player.position;
    }

    #endregion

    #region NavMeshAgent Helpers

    public bool HasReachedDestination(float extraTolerance = 0.05f)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"[{name}] Cannot check HasReachedDestination: NavMeshAgent reference is missing, disabled, or not on NavMesh.", this);
            return false;
        }

        if (agent.pathPending)
            return false;

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            return false;

        if (agent.remainingDistance > agent.stoppingDistance + extraTolerance)
            return false;

        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    public void StopAgentImmediately()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (agent.isOnNavMesh)
            agent.ResetPath();
    }

    #endregion

    #region Facing Helpers

    /// <summary>
    /// Rotates the object to face the specified target position, optionally using a smooth turning speed.
    /// </summary>
    /// <remarks>This method adjusts the object's rotation so that it faces the target position on the
    /// horizontal plane. If the target is very close to the current position, no rotation occurs. Use a higher turn
    /// speed for faster rotation, or 0 to use the default speed.</remarks>
    /// <param name="target">The world position to face. Only the horizontal (XZ) components are considered; the Y component is ignored.</param>
    /// <param name="turnSpeed">The speed at which the object turns toward the target, in degrees per second. If set to 0, the object's default
    /// turn speed is used.</param>
    public void FaceTarget(Vector3 target, float turnSpeed = 0)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f; // keep only horizontal rotation

        if (direction.sqrMagnitude < 0.0001f)
            return;

        if (turnSpeed == 0)
            turnSpeed = this.turnSpeed;

        Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);
        Vector3 currentEulerAngles = transform.rotation.eulerAngles;
        float yRotation = Mathf.LerpAngle(currentEulerAngles.y, targetRotation.eulerAngles.y, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentEulerAngles.x, yRotation, currentEulerAngles.z);
    }

    /// <summary>
    /// Smoothly rotates the enemy toward a fixed world-space target position on the horizontal plane
    /// until the turn is complete.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="FaceTarget(Vector3, float)"/>, this method is intended for a locked target
    /// position captured once by the calling state, such as the player's position when entering a turn
    /// or attack-preparation state.
    ///
    /// Pass the same <paramref name="target"/> each frame until this method returns true.
    /// Once the enemy is facing the target within <paramref name="angleTolerance"/>, the method snaps
    /// exactly to the final rotation and reports completion.
    /// </remarks>
    /// <param name="target">
    /// The fixed world position to face. Only horizontal rotation is applied; vertical difference is ignored.
    /// </param>
    /// <param name="turnSpeed">
    /// Rotation speed in degrees per second. If set to 0 or less, the enemy's default <see cref="turnSpeed"/>
    /// value is used.
    /// </param>
    /// <param name="angleTolerance">
    /// The angle, in degrees, at which the turn is considered complete.
    /// </param>
    /// <returns>
    /// True if the enemy has finished turning toward the target; otherwise false.
    /// </returns>
    public bool TurnToTargetSmooth(Vector3 target, float turnSpeed = 0f, float angleTolerance = 2f)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return true;

        if (turnSpeed <= 0f)
            turnSpeed = this.turnSpeed;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) <= angleTolerance)
        {
            transform.rotation = targetRotation;
            return true;
        }

        return false;
    }

    public void FaceSteeringTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (agent.pathPending || !agent.hasPath)
            return;

        FaceTarget(agent.steeringTarget);
    }

    public void FacePlayer(float turnSpeed = 0) => FaceTarget(player.position, turnSpeed);
    public void FaceKnownPlayerPosition(float turnSpeed = 0) => FaceTarget(GetKnownPlayerPosition(), turnSpeed);

    public bool TurnToPlayerSmooth(float turnSpeed = 0f, float angleTolerance = 2f)
    {
        return TurnToTargetSmooth(player.position, turnSpeed, angleTolerance);
    }

    public bool TurnToKnownPlayerPositionSmooth(float turnSpeed = 0f, float angleTolerance = 2f)
    {
        return TurnToTargetSmooth(GetKnownPlayerPosition(), turnSpeed, angleTolerance);
    }

    /// <summary>
    /// Returns the unsigned horizontal angle, in degrees, between the enemy's forward direction
    /// and the direction toward the specified target position.
    /// </summary>
    /// <param name="target">
    /// The world position to compare against. Vertical difference is ignored.
    /// </param>
    /// <returns>
    /// The horizontal angle from the enemy's current forward direction to the target.
    /// Returns 0 if the target is extremely close to the enemy.
    /// </returns>
    public float GetAngleToTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return 0f;

        return Vector3.Angle(transform.forward, direction.normalized);
    }

    /// <summary>
    /// Returns whether the enemy is facing the specified target position within a maximum horizontal angle.
    /// </summary>
    /// <param name="target">
    /// The world position to test against. Vertical difference is ignored.
    /// </param>
    /// <param name="maxAngle">
    /// The maximum allowed horizontal angle, in degrees, for the target to be considered in front of the enemy.
    /// </param>
    /// <returns>
    /// True if the enemy is facing the target within <paramref name="maxAngle"/>; otherwise false.
    /// </returns>
    public bool IsFacingTarget(Vector3 target, float maxAngle)
    {
        return GetAngleToTarget(target) <= maxAngle;
    }

    #endregion

    #region IPoolable implementation

    /// <summary>
    /// Called by the pooling system when this enemy is fetched from the pool.
    /// </summary>
    public virtual void OnSpawnedFromPool()
    {
        ResetEnemyForReuse();
    }

    /// <summary>
    /// Called by the pooling system when the enemy is returned to the pool.
    /// </summary>
    public virtual void OnReturnedToPool()
    {
        // Optional: cleanup if needed (stop sounds, particles, etc.)
        if (inBattleMode)
            ExitBattleMode();
    }


    /// <summary>
    /// Restores common runtime state so the enemy can be reused after death.
    /// Enemy subclasses should override <see cref="OnResetEnemyStateMachineForReuse"/> to restore FSM state.
    /// </summary>
    protected virtual void ResetEnemyForReuse()
    {
        // Core gameplay state
        currentHealth = maxHealth;
        isDead = false;
        inBattleMode = false;

        // Animation flags
        manualMovement = false;
        manualRotation = false;

        // Restore components disabled on death
        if (anim != null)
            anim.enabled = true;

        if (agent != null)
        {
            if (!agent.enabled)
                agent.enabled = true;

            agent.isStopped = false;

            // Safe reset (avoid exceptions if not on NavMesh)
            if (agent.isOnNavMesh)
                agent.ResetPath();
        }

        // Restore ragdoll / colliders
        if (ragdoll != null)
        {
            ragdoll.RagdollActive(false);
            ragdoll.CollidersActive(true);
        }

        // Restore dissolve visuals and original materials
        if (deathDissolve != null)
            deathDissolve.ResetForReuse();

        // Patrol state reset
        currentPatrolIndex = 0;

        // IMPORTANT:
        // Your specific enemy type (EnemyMelee / EnemyRanged) should reset its state machine
        // back to Idle here by overriding this method OR calling a dedicated reset method.
        OnResetEnemyStateMachineForReuse();
    }

    /// <summary>
    /// Override in subclasses (EnemyMelee, etc.) to put the state machine back into a valid alive state.
    /// </summary>
    protected virtual void OnResetEnemyStateMachineForReuse()
    {
        Debug.LogWarning($"[{name}] OnResetEnemyStateMachineForReuse not overridden in subclass. Make sure to reset the FSM to a valid alive state (e.g., Idle).", this);
    }

    #endregion

    #region Animation Event Relays
    public void ActivateManualMovement(bool manualMovement) => this.manualMovement = manualMovement;
    public bool ManualMovementActive() => manualMovement;

    public void ActivateManualRotation(bool manualRotation) => this.manualRotation = manualRotation;
    public bool ManualRotationActive() => manualRotation;
    public void AnimationTrigger() => stateMachine.currentState.AnimationTrigger();

    public virtual void AbilityTrigger() => stateMachine.currentState.AbilityTrigger();

    protected float GetAnimationClipDuration(string clipName)
    {
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning($"Animation clip with name {clipName} not found on {name}", this);
        return 0f;
    }

    #endregion

    #region Patrol logic
    public Vector3 GetPatrolDestination()
    {
        if (patrolPointsPosition == null || patrolPointsPosition.Length == 0)
        {
            Debug.LogWarning($"[{name}] No patrol points assigned. Ensure patrol points are set up in the inspector.", this);
            return transform.position;
        }
        Vector3 destination = patrolPointsPosition[currentPatrolIndex];

        currentPatrolIndex++;

        if (currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;

        return destination;
    }
    private void InitializePatrolPoints()
    {
        patrolPointsPosition = new Vector3[patrolPoints.Length];

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            patrolPointsPosition[i] = patrolPoints[i].position;
            patrolPoints[i].gameObject.SetActive(false);
        }
    }

    #endregion

    #region Debug Gizmos
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, combatRange);
    }

    #endregion
}
