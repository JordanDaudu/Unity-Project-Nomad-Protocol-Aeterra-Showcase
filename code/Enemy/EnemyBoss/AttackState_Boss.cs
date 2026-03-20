using UnityEngine;

/// <summary>
/// Boss primary melee attack state.
/// </summary>
/// <remarks>
/// Animation-driven:
/// - The attack finishes when an animation event calls <see cref="EnemyState.AnimationTrigger"/>, setting <see cref="triggerCalled"/>.
/// - On completion, the boss transitions either back to idle (if still in attack range) or back to move.
///
/// Key connections:
/// - <see cref="MoveState_Boss"/> uses <see cref="lastTimeAttacked"/> to decide when to speed up pursuit.
/// - <see cref="EnemyBossVisuals.enableWeaponTrail"/> is toggled for attack readability.
/// </remarks>
public class AttackState_Boss : EnemyState
{
    #region Constants

    private const int attackAnimCount = 2;

    #endregion

    #region Runtime

    private EnemyBoss enemy;

    /// <summary>
    /// Timestamp (Time.time) of the last completed attack.
    /// Used by other states to throttle/shape behavior.
    /// </summary>
    public float lastTimeAttacked { get; private set; }

    #endregion

    #region Construction

    public AttackState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        enemy.bossVisuals.enableWeaponTrail(true);

        // Randomize between a small set of attack animations to reduce repetition.
        enemy.anim.SetFloat("AttackAnimIndex", Random.Range(0, attackAnimCount));

        // Attacks are performed in place; movement resumes after attack completes.
        enemy.agent.isStopped = true;
    }

    public override void Exit()
    {
        base.Exit();

        lastTimeAttacked = Time.time;
        enemy.bossVisuals.enableWeaponTrail(false);
    }

    public override void Update()
    {
        base.Update();

        if (triggerCalled)
        {
            if (enemy.PlayerInAttackRange())
                stateMachine.ChangeState(enemy.idleState);
            else
                stateMachine.ChangeState(enemy.moveState);
        }
    }

    #endregion
}