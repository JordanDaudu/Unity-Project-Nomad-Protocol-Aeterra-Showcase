using UnityEngine;

/// <summary>
/// Patrol movement state for ranged enemies.
/// </summary>
/// <remarks>
/// Behavior:
/// - Moves the enemy to the next patrol destination (see <see cref="Enemy.GetPatrolDestination"/>).
/// - Once destination is reached, transitions back to <see cref="IdleState_Range"/>.
/// </remarks>
public class MoveState_Range : EnemyState
{
    #region Runtime

    private EnemyRange enemy;
    private Vector3 destination;

    #endregion

    #region Constructor

    public MoveState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        enemy.agent.isStopped = false;
        enemy.agent.speed = enemy.walkSpeed;

        destination = enemy.GetPatrolDestination();
        enemy.agent.SetDestination(destination);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.FaceSteeringTarget();

        if (enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + .05f)
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }

    #endregion
}
