using UnityEngine;

/// <summary>
/// Boss movement state.
/// </summary>
/// <remarks>
/// Dual purpose state:
/// - Out of battle: patrols between destinations (see <see cref="Enemy.GetPatrolDestination"/>).
/// - In battle: chases the player and periodically chooses a high-level action (ability or jump attack).
///
/// Combat logic details:
/// - The boss can "speed up" after a period without attacking to maintain pressure.
/// - Action selection is gated by <see cref="EnemyBoss.ActionCooldown"/>.
/// - If in attack range, transitions to <see cref="AttackState_Boss"/> (or <see cref="TurnToPlayerState_Boss"/>).
/// </remarks>
public class MoveState_Boss : EnemyState
{
    #region Runtime

    private EnemyBoss enemy;
    private Vector3 destination;

    private float actionTimer;

    private float timeBeforeSpeedUp = 5;
    private bool speedUpActivated;

    #endregion

    #region Construction

    public MoveState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        SpeedReset();

        enemy.agent.isStopped = false;

        destination = enemy.GetPatrolDestination();
        enemy.agent.SetDestination(destination);

        actionTimer = enemy.ActionCooldown;
    }

    public override void Update()
    {
        base.Update();

        actionTimer -= Time.deltaTime;
        enemy.FaceSteeringTarget();

        if (enemy.inBattleMode)
        {
            if (!enemy.HasRecentTargetKnowledge())
            {
                // For now I prefer the Boss type to keep chasing

                //enemy.ExitBattleMode();
                //stateMachine.ChangeState(enemy.idleState);
                //return;
            }

            if (ShouldSpeedUp())
            {
                SpeedUp();
            }

            Vector3 playerPosition = enemy.player.position;
            enemy.agent.SetDestination(playerPosition);

            if (actionTimer <= 0f)
            {
                PerformRandomAction();
            }
            else if (enemy.PlayerInAttackRange())
            {
                if (enemy.IsFacingPlayerForAttack())
                    stateMachine.ChangeState(enemy.attackState);
                else
                    stateMachine.ChangeState(enemy.turnToPlayerState);
            }
        }
        else
        {
            if (enemy.HasReachedDestination(0.25f))
                stateMachine.ChangeState(enemy.idleState);
        }
    }

    #endregion

    #region Speed Up Logic

    private void SpeedUp()
    {
        enemy.agent.speed = enemy.runSpeed;
        enemy.anim.SetFloat("MoveAnimIndex", 1);
        speedUpActivated = true;
    }

    private void SpeedReset()
    {
        speedUpActivated = false;
        enemy.anim.SetFloat("MoveAnimIndex", 0);
        enemy.agent.speed = enemy.walkSpeed;
    }

    private bool ShouldSpeedUp()
    {
        if (speedUpActivated)
            return false;

        // Give the boss a short window after attacking before speeding up again.
        if (Time.time < enemy.attackState.lastTimeAttacked + timeBeforeSpeedUp)
            return false;

        return true;
    }

    #endregion

    #region Action Selection

    private void PerformRandomAction()
    {
        actionTimer = enemy.ActionCooldown;

        int randomActionIndex = Random.Range(0, 2);

        if (randomActionIndex == 0)
        {
            TryToUseAbility();
        }
        else
        {
            if (enemy.CanUseJumpAttack())
                stateMachine.ChangeState(enemy.jumpAttackState);
            else if (enemy.bossWeaponType == BossWeaponType.Hammer)
                TryToUseAbility();
        }
    }

    private void TryToUseAbility()
    {
        if (enemy.CanUseAbility())
            stateMachine.ChangeState(enemy.abilityState);
    }

    #endregion
}