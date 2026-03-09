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
public class Enemy : MonoBehaviour, IPoolable
{
    #region Health

    [SerializeField] protected int maxHealth = 20;
    [SerializeField] protected int currentHealth;

    #endregion

    #region AI Tuning

    [Header("Idle data")]
    public float idleTime;
    public float aggressionRange;

    [Header("Move data")]
    public float moveSpeed;
    public float chaseSpeed;
    public float turnSpeed;

    #endregion

    #region Patrol

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
    }

    protected virtual void Start()
    {
        InitializePatrolPoints();
    }

    protected virtual void Update()
    {
        // Base enemy does not tick the state machine directly.
        // Subclasses (EnemyMelee, etc.) call: stateMachine.currentState.Update();
        if (ShouldEnterBattleMode())
            EnterBattleMode();
    }

    #endregion

    #region Combat State

    /// <summary>
    /// Checks if the player is within aggresionRange and enters battle mode once.
    /// </summary>
    protected bool ShouldEnterBattleMode()
    {
        bool inAggresionRange = Vector3.Distance(transform.position, player.position) < aggressionRange;

        if (inAggresionRange && !inBattleMode)
        {
            EnterBattleMode();
            return true;
        }

        return false;
    }

    public virtual void EnterBattleMode()
    {
        inBattleMode = true;
    }

    /// <summary>
    /// Called when the enemy is hit by a bullet.
    /// </summary>
    public virtual void GetHit()
    {
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

    #endregion

    #region Facing Helpers

    private void FaceTarget(Vector3 target)
    {
        Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);

        Vector3 currentEulerAngles = transform.rotation.eulerAngles;

        float yRotation = Mathf.LerpAngle(currentEulerAngles.y, targetRotation.eulerAngles.y, turnSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(currentEulerAngles.x, yRotation, currentEulerAngles.z);
    }

    public void FaceSteeringTarget() => FaceTarget(agent.steeringTarget);

    public void FacePlayer() => FaceTarget(player.position);

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
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, aggressionRange);
    }

    #endregion
}
