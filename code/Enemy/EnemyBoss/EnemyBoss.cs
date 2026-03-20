using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Boss weapon/ability archetype.
/// </summary>
/// <remarks>
/// This influences:
/// - which ability behavior is executed in <see cref="AbilityState_Boss"/>
/// - which visuals are enabled in <see cref="EnemyBossVisuals"/>
/// </remarks>
public enum BossWeaponType
{
    FlameThrower,
    Hammer
}

/// <summary>
/// Boss enemy implementation.
/// </summary>
/// <remarks>
/// This is a specialized <see cref="Enemy"/> variant with its own state machine and abilities.
///
/// High-level behavior:
/// - Patrols/positions via <see cref="MoveState_Boss"/>
/// - Performs melee attacks in <see cref="AttackState_Boss"/> when in range and facing the player
/// - Performs an ability (<see cref="AbilityState_Boss"/>) or jump attack (<see cref="JumpAttackState_Boss"/>) on cooldown
/// - Dies into ragdoll + dissolve via <see cref="DeadState_Boss"/>
///
/// Key connections:
/// - <see cref="EnemyAnimationEvents"/> forwards AnimationTrigger / AbilityTrigger from the Animator timeline into the FSM.
/// - <see cref="EnemyBossVisuals"/> owns VFX for flamethrower batteries and jump attack landing zones.
/// - <see cref="CombatMusicCoordinator"/> treats <see cref="EnemyBoss"/> as a boss combat participant for music.
/// </remarks>
public class EnemyBoss : Enemy
{
    #region States

    public IdleState_Boss idleState { get; private set; }
    public MoveState_Boss moveState { get; private set; }
    public AttackState_Boss attackState { get; private set; }
    public JumpAttackState_Boss jumpAttackState { get; private set; }
    public AbilityState_Boss abilityState { get; private set; }
    public TurnToPlayerState_Boss turnToPlayerState { get; private set; }
    public DeadState_Boss deadState { get; private set; }

    #endregion

    #region Components

    public EnemyBossVisuals bossVisuals { get; private set; }

    #endregion

    #region Inspector

    [Header("Boss Details")]
    public BossWeaponType bossWeaponType;

    [SerializeField] private float actionCooldown = 10f;
    [SerializeField] private float attackRange;

    [Header("Boss Attack Facing")]
    [SerializeField] private float attackFacingAngle = 25f;
    [SerializeField] private float attackTurnSpeed = 540f;

    [Header("Ability Settings")]
    public float minAbilityDistance = 0f;
    public float maxAbilityDistance = 2f;
    public float abilityCooldown;

    [Header("Flamethrower Settings")]
    public ParticleSystem flameThrowerEffect;
    public float flameThrowerDuration;

    [Header("Hammer Settings")]
    public GameObject activationPrefab;

    [Header("Jump Attack Settings")]
    public Transform impactPoint;
    public float jumpAttackCooldown = 10f;
    public float travelTimeToTarget = 1f;
    public float minJumpDistance = 5f;

    [Space]
    [SerializeField] private float impactRadius = 2.5f; // Important if changed, then change landing zone FX radius as well.
    [SerializeField] private float upForceMultiplier = 10f;
    [SerializeField] private float impactPower = 10f;

    #endregion

    #region Runtime

    private float lastTimeUsedAbility;
    private float lastTimeJumped;

    public bool flameThrowerActive { get; private set; }

    #endregion

    #region Public Properties

    public float AttackFacingAngle => attackFacingAngle;
    public float AttackTurnSpeed => attackTurnSpeed;

    public float ActionCooldown => actionCooldown;
    public float AttackRange => attackRange;

    #endregion

    #region Cooldowns

    public void SetAbilityOnCooldown() => lastTimeUsedAbility = Time.time;
    public void SetJumpAttackOnCooldown() => lastTimeJumped = Time.time;

    #endregion

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();

        bossVisuals = GetComponent<EnemyBossVisuals>();

        // State instances are constructed once and reused.
        idleState = new IdleState_Boss(this, stateMachine, "Idle");
        moveState = new MoveState_Boss(this, stateMachine, "Move");
        attackState = new AttackState_Boss(this, stateMachine, "Attack");
        jumpAttackState = new JumpAttackState_Boss(this, stateMachine, "JumpAttack");
        abilityState = new AbilityState_Boss(this, stateMachine, "Ability");

        // Reuses Idle animation bool while turning; the state logic decides when to switch.
        turnToPlayerState = new TurnToPlayerState_Boss(this, stateMachine, "Idle");

        // Boss dies into ragdoll+dissolve; Idle bool is used as a "no-op" animator state flag.
        deadState = new DeadState_Boss(this, stateMachine, "Idle");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();

        stateMachine.currentState.Update();

        if (ShouldEnterBattleMode())
            EnterBattleMode();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.blanchedAlmond;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.lawnGreen;
        Gizmos.DrawWireSphere(transform.position, minJumpDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, impactRadius);

        Gizmos.color = Color.mediumPurple;
        Gizmos.DrawWireSphere(transform.position, maxAbilityDistance);
        Gizmos.DrawWireSphere(transform.position, minAbilityDistance);
    }

    #endregion

    #region Combat

    public override void EnterBattleMode()
    {
        if (inBattleMode)
            return;

        base.EnterBattleMode();
        stateMachine.ChangeState(moveState);
    }

    public override void GetHit()
    {
        base.GetHit();

        if (currentHealth <= 0 && stateMachine.currentState != deadState)
            stateMachine.ChangeState(deadState);
    }

    public bool IsFacingPlayerForAttack() => IsFacingTarget(player.position, attackFacingAngle);

    public bool PlayerInAttackRange() => Vector3.Distance(transform.position, player.position) < attackRange;

    #endregion

    #region Ability

    public void ActivateFlamethrower(bool activate)
    {
        flameThrowerActive = activate;

        if (!activate)
        {
            flameThrowerEffect.Stop();
            anim.SetTrigger("StopFlamethrower");
            return;
        }

        var mainModule = flameThrowerEffect.main;
        var emissionModule = flameThrowerEffect.transform.GetChild(0).GetComponent<ParticleSystem>().main;

        mainModule.duration = flameThrowerDuration;
        emissionModule.duration = flameThrowerDuration;

        flameThrowerEffect.Clear();
        flameThrowerEffect.Play();
    }

    public void ActivateHammer()
    {
        GameObject newActivation = ObjectPool.Instance.GetObject(activationPrefab, impactPoint);

        ObjectPool.Instance.ReturnObjectToPool(newActivation, 1);
    }

    public bool CanUseAbility()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool isPlayerInAbilityRange = distanceToPlayer >= minAbilityDistance &&
                                      distanceToPlayer <= maxAbilityDistance;

        if (!isPlayerInAbilityRange)
            return false;

        if (Time.time < lastTimeUsedAbility + abilityCooldown)
            return false;

        if (!CanSeePlayer())
            return false;

        return true;
    }

    #endregion

    #region Jump Attack

    public bool CanUseJumpAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < minJumpDistance)
            return false;

        if (Time.time < lastTimeJumped + jumpAttackCooldown)
            return false;

        if (!CanSeePlayer())
            return false;

        return true;
    }

    /// <summary>
    /// Called by the jump attack animation at the landing frame.
    /// Applies an explosion force and triggers landing FX.
    /// </summary>
    public void JumpImpact()
    {
        Transform impactPoint = this.impactPoint;

        if (impactPoint == null)
            impactPoint = transform;

        Collider[] colliders = Physics.OverlapSphere(impactPoint.position, impactRadius);

        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddExplosionForce(impactPower, impactPoint.position, impactRadius, upForceMultiplier, ForceMode.Impulse);
            }
        }

        bossVisuals.PlayOnLandingFX(impactPoint.position);
    }

    #endregion
}