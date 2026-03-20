using UnityEngine;

/// <summary>
/// Boss jump attack state.
/// </summary>
/// <remarks>
/// Flow:
/// - Captures the player's position at the moment of takeoff (lastPlayerPosition).
/// - Plays a landing zone telegraph via <see cref="EnemyBossVisuals.PlayLandingZoneFX"/>.
/// - During the airborne portion, movement can be driven manually (see <see cref="Enemy.ManualMovementActive"/>).
/// - On the landing animation event, <see cref="EnemyBoss.JumpImpact"/> is typically called.
/// - When the animation trigger completes (<see cref="triggerCalled"/>), returns to <see cref="MoveState_Boss"/>.
///
/// Hammer-specific note:
/// - For the hammer variant, the NavMeshAgent may be re-enabled early to steer toward the target.
/// </remarks>
public class JumpAttackState_Boss : EnemyState
{
    #region Runtime

    private EnemyBoss enemy;
    private Vector3 lastPlayerPosition;

    private float jumpAttackMovementSpeed;

    #endregion

    #region Construction

    public JumpAttackState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        enemy.agent.isStopped = true;
        enemy.agent.velocity = Vector3.zero;

        // Snapshot target position so the jump is dodgeable (telegraph is consistent).
        lastPlayerPosition = enemy.player.position;

        enemy.bossVisuals.PlayLandingZoneFX(lastPlayerPosition);
        enemy.bossVisuals.enableWeaponTrail(true);

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, lastPlayerPosition);
        jumpAttackMovementSpeed = distanceToPlayer / enemy.travelTimeToTarget;

        // Face immediately at takeoff so the jump arc starts aligned.
        enemy.FaceTarget(lastPlayerPosition, 1000);

        if (enemy.bossWeaponType == BossWeaponType.Hammer)
        {
            // For the hammer, we want at the start of the movement for the agent to take control and move towards the player.
            enemy.agent.speed = enemy.runSpeed;
            enemy.agent.SetDestination(lastPlayerPosition);
        }
    }

    public override void Exit()
    {
        base.Exit();
        enemy.SetJumpAttackOnCooldown();
        enemy.bossVisuals.enableWeaponTrail(false);
    }

    public override void Update()
    {
        base.Update();

        Vector3 myPosition = enemy.transform.position;

        // Manual movement toggles are typically driven by animation events.
        enemy.agent.enabled = !enemy.ManualMovementActive();

        if (enemy.ManualMovementActive())
        {
            enemy.transform.position = Vector3.MoveTowards(myPosition, lastPlayerPosition, jumpAttackMovementSpeed * Time.deltaTime);
        }

        if (triggerCalled)
            stateMachine.ChangeState(enemy.moveState);
    }

    #endregion
}