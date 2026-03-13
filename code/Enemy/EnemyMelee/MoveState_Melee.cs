using UnityEngine;

/// <summary>
/// Patrol movement state.
/// 
/// Behavior:
/// - Sets NavMeshAgent speed to <see cref="Enemy.walkSpeed"/>.
/// - Moves to the next patrol point (see <see cref="Enemy.GetPatrolDestination"/>).
/// - When destination is reached, transitions back to <see cref="IdleState_Melee"/>.
/// </summary>
public class MoveState_Melee : EnemyState
{
    private EnemyMelee enemy;
    private Vector3 destination;
    public MoveState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();

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
}
