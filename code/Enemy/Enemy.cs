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
        if (inBattleMode)
            return false;

        if (perception != null)
            return perception.IsTargetVisible;

        return IsPlayerInCombatRange();
    }

    public virtual void EnterBattleMode()
    {
        inBattleMode = true;
    }

    public virtual void ExitBattleMode()
    {
        inBattleMode = false;
    }

    /// <summary>
    /// Called when the enemy is hit by a bullet.
    /// Refreshes target memory so the enemy can properly re-engage the attacker.
    /// </summary>
    public virtual void GetHit()
    {
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

    public void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f; // keep only horizontal rotation

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);

        Vector3 currentEulerAngles = transform.rotation.eulerAngles;

        float yRotation = Mathf.LerpAngle(currentEulerAngles.y, targetRotation.eulerAngles.y, turnSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(currentEulerAngles.x, yRotation, currentEulerAngles.z);
    }

    public void FaceSteeringTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (agent.pathPending || !agent.hasPath)
            return;

        FaceTarget(agent.steeringTarget);
    }

    public void FacePlayer() => FaceTarget(player.position);
    public void FaceKnownPlayerPosition() => FaceTarget(GetKnownPlayerPosition());

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
    }


    /// <summary>
    /// Restores common runtime state so the enemy can be reused after death.
    /// Enemy subclasses should override <see cref="OnResetEnemyStateMachineForReuse"/> to restore FSM state.
    /// </summary>
    protected virtual void ResetEnemyForReuse()
    {
        // Core gameplay state
        currentHealth = maxHealth;
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
