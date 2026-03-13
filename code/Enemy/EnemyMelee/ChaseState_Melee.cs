using UnityEngine;

/// <summary>
/// Combat chase state.
/// 
/// Behavior:
/// - Enables NavMeshAgent movement at <see cref="Enemy.runSpeed"/>.
/// - Periodically refreshes destination toward the player (throttled).
/// - If player enters attack range, transitions to <see cref="AttackState_Melee"/>.
/// </summary>
public class ChaseState_Melee : EnemyState
{
    private EnemyMelee enemy;
    private float lastTimeUpdatedDestination;

    public ChaseState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.agent.speed = enemy.runSpeed;
        enemy.agent.isStopped = false;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (!enemy.HasRecentTargetKnowledge())
        {
            // For now I prefer the melee type to keep chasing
        }

        if (enemy.CanSeePlayer() && enemy.PlayerInAttackRange())
        {
            stateMachine.ChangeState(enemy.attackState);
            return;
        }

        enemy.FaceSteeringTarget();

        if (CanUpdateDestination())
            enemy.agent.SetDestination(enemy.player.position);
    }

    private bool CanUpdateDestination()
    {
        // Destination refresh is throttled to reduce path recalculation spam.
        if (Time.time > lastTimeUpdatedDestination + .25f)
        {
            lastTimeUpdatedDestination = Time.time;
            return true;
        }

        return false;
    }

    private bool ReachedDestination()
    {
        if (enemy.agent.pathPending)
            return false;

        return enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + 0.15f &&
               (!enemy.agent.hasPath || enemy.agent.velocity.sqrMagnitude < 0.05f);
    }
}
