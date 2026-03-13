using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data describing a single melee attack variant.
/// 
/// Used by:
/// - <see cref="AttackState_Melee"/> to configure animation index/speed and movement.
/// - EnemyMelee to randomize and select next attacks from <see cref="attackList"/>.
/// </summary>
[System.Serializable]
public struct AttackData_EnemyMelee
{
    public string attackName;

    [Tooltip("Distance from enemy to player required for this attack to be considered 'in range'.")]
    public float attackRange;

    [Tooltip("Movement speed used during the attack animation window and regular usage.")]
    public float moveSpeed;

    [Tooltip("Animator parameter used to select which attack animation to play.")]
    public float attackIndex;

    [Range(1, 2)]
    public float animationSpeed;
    public AttackType_Melee attackType;
}

public enum AttackType_Melee { Close, Charge }

public enum EnemyType_Melee { Regular, Shield, Dodge, AxeThrow }

/// <summary>
/// Concrete enemy type implementing a modular melee AI via an FSM.
/// 
/// Owns:
/// - A set of states (Idle/Move/Recovery/Chase/Attack/Ability/Dead)
/// - Variant-specific features: shield, dodge reaction, axe throw ability
/// - Attack selection data and animator parameter setup
/// 
/// Key connections:
/// - Driven by <see cref="EnemyStateMachine"/> created in <see cref="Enemy.Awake"/>.
/// - Ticked in Update via stateMachine.currentState.Update().
/// - Reacts to player bullets via <see cref="Enemy.GetHit"/> called from Bullet.cs.
/// - Manual movement/rotation windows are toggled by <see cref="EnemyAnimationEvents"/>.
/// </summary>
public class EnemyMelee : Enemy
{

    #region States
    public IdleState_Melee idleState { get; private set; }
    public MoveState_Melee moveState { get; private set; }
    public RecoveryState_Melee recoveryState { get; private set; }
    public ChaseState_Melee chaseState { get; private set; }
    public AttackState_Melee attackState { get; private set; }
    public DeadState_Melee deadState { get; private set; }
    public AbilityState_Melee abilityState { get; private set; }

    #endregion

    [Header("Enemy Settings")]
    [SerializeField] private EnemyType_Melee enemyType;
    [SerializeField] public EnemyMeleeWeaponType weaponType;
    [SerializeField] private EnemyShield enemyShield;

    [Header("Dodge Settings")]
    [Tooltip("Cooldown after a dodge animation completes. Negative allows immediate dodge at start.")]
    [SerializeField] private float dodgeCooldown;
    private float lastTimeDodged = -10f;

    [Header("Axe Throw Ability")]
    public GameObject axePrefab;
    public float axeFlySpeed;
    public float axeAimTimer;
    public float axeThrowCooldown;
    public Transform axeStartPoint;
    private float lastTimeAxeThrown;

    [Header("Attack Data")]
    public AttackData_EnemyMelee attackData;
    public List<AttackData_EnemyMelee> attackList;

    protected override void Awake()
    {
        base.Awake();

        // State instances are constructed once and reused.
        idleState = new IdleState_Melee(this, stateMachine, "Idle");
        moveState = new MoveState_Melee(this, stateMachine, "Move");
        recoveryState = new RecoveryState_Melee(this, stateMachine, "Recovery");
        chaseState = new ChaseState_Melee(this, stateMachine, "Chase");
        attackState = new AttackState_Melee(this, stateMachine, "Attack");
        deadState = new DeadState_Melee(this, stateMachine, "Idle"); // Idle anim is just a placeholder, we use ragdoll for death so we won't be playing any death anims
        abilityState = new AbilityState_Melee(this, stateMachine, "AxeThrow");

    }



    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
        ResetCooldown();

        InitializeSpeciality();
        visuals.SetupLook();
        UpdateAttackData();
    }

    protected override void Update()
    {
        base.Update();
        stateMachine.currentState.Update();
    }

    public override void EnterBattleMode()
    {
        if (inBattleMode)
            return;

        base.EnterBattleMode();

        stateMachine.ChangeState(recoveryState);
    }

    protected override void OnResetEnemyStateMachineForReuse()
    {
        // Ensure the FSM is in an alive state when reused from the pool.
        if (stateMachine.currentState != idleState)
            stateMachine.Initialize(idleState);
    }

    protected override void ResetEnemyForReuse()
    {
        base.ResetEnemyForReuse();

        InitializeSpeciality();
    }

    public override void GetHit()
    {
        base.GetHit();

        if (currentHealth <= 0 && stateMachine.currentState != deadState)
            stateMachine.ChangeState(deadState);
    }

    public bool PlayerInAttackRange() => Vector3.Distance(transform.position, player.position) < attackData.attackRange;

    public void ActivateDodgeRoll()
    {
        // Only dodge if:
        // - Variant is Dodge
        // - Currently chasing
        // - Player isn't too close (avoid dodging "in place")
        // - Off cooldown (includes full animation duration)
        if (enemyType != EnemyType_Melee.Dodge)
            return;

        if (stateMachine.currentState != chaseState)
            return;

        if (Vector3.Distance(transform.position, player.position) < 2f)
            return;

        float dodgeAnimDuration = GetAnimationClipDuration("Dodge roll");

        if (Time.time > lastTimeDodged + dodgeAnimDuration + dodgeCooldown)
        {
            lastTimeDodged = Time.time;
            anim.SetTrigger("Dodge");
        }
    }

    public bool CanThrowAxe()
    {
        if (enemyType != EnemyType_Melee.AxeThrow)
            return false;

        if (Time.time < axeThrowCooldown + lastTimeAxeThrown)
            return false;

        lastTimeAxeThrown = Time.time;
        return true;
    }

    public void ThrowAxe()
    {
        // Axe is pooled and configured to track the player for a short duration.
        GameObject newAxe = ObjectPool.Instance.GetObject(axePrefab, axeStartPoint);

        newAxe.GetComponent<EnemyAxe>().AxeSetup(axeFlySpeed, player, axeAimTimer);
    }

    public override void AbilityTrigger()
    {
        base.AbilityTrigger();

        // Slow the enemy slightly while performing ability follow-up / recovery window.
        walkSpeed = walkSpeed * 0.6f;

        // Slow the enemy slightly while performing ability follow-up / recovery window.
        visuals.EnableWeaponModel(false);
    }

    public void UpdateAttackData()
    {
        EnemyMeleeWeaponModel currentWeapon = visuals.currentWeaponModel.GetComponent<EnemyMeleeWeaponModel>();

        if (currentWeapon.WeaponData != null)
        {
            attackList = new List<AttackData_EnemyMelee>(currentWeapon.WeaponData.attackData);
            turnSpeed = currentWeapon.WeaponData.turnSpeed;
        }
    }

    protected override void InitializeSpeciality()
    {
        AxeThrowInitialization();
        shieldInitialization();
        DodgeInitialization();
    }

    private void DodgeInitialization()
    {
        if (enemyType == EnemyType_Melee.Dodge)
        {
            weaponType = EnemyMeleeWeaponType.Unarmed;
        }
    }

    private void shieldInitialization()
    {
        if (enemyType == EnemyType_Melee.Shield)
        {
            // Animator uses this to select shielded chase blend/animation set.
            anim.SetFloat("ChaseIndex", 1);
            enemyShield.gameObject.SetActive(true);
            enemyShield.RestoreDurability();
            weaponType = EnemyMeleeWeaponType.OneHand;
        }
    }

    private void AxeThrowInitialization()
    {
        if (enemyType == EnemyType_Melee.AxeThrow)
        {
            weaponType = EnemyMeleeWeaponType.Throw;
        }
    }
    private void ResetCooldown()
    {
        lastTimeDodged -= dodgeCooldown;
        lastTimeAxeThrown -= axeThrowCooldown;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackData.attackRange);
    }
}
