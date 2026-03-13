using UnityEngine;

/// <summary>
/// Combat movement state that runs the ranged enemy to a reserved cover point.
/// </summary>
/// <remarks>
/// Flow:
/// - Requests/reserves a cover point via <see cref="EnemyRange.AttemptToFindCover"/> (delegates to <see cref="EnemyCoverController"/>).
/// - Runs to that point at <see cref="Enemy.runSpeed"/>.
/// - Once arrived, faces the known player position and transitions to <see cref="BattleState_Range"/>.
/// 
/// Edge cases:
/// - If no cover is found (or cover disappears), the enemy falls back to battle state instead of stalling.
/// - If target knowledge expires, the enemy exits battle and returns to idle.
/// </remarks>
public class RunToCoverState_Range : EnemyState
{
    #region Runtime

    private EnemyRange enemy;
    private Vector3 destination;
    private Transform reservedCoverTransform;

    #endregion

    #region Constructor

    public RunToCoverState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        enemy.visuals.EnableIK(true, false);
        enemy.agent.isStopped = false;
        enemy.agent.speed = enemy.runSpeed;

        reservedCoverTransform = enemy.AttemptToFindCover();

        // If no valid cover was found, do not stay in this state.
        if (reservedCoverTransform == null)
        {
            stateMachine.ChangeState(enemy.battleState);
            return;
        }

        destination = reservedCoverTransform.position;
        enemy.agent.SetDestination(destination);
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
            enemy.ExitBattleMode();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        // If for some reason the cover transform disappeared, fall back to battle.
        if (reservedCoverTransform == null)
        {
            stateMachine.ChangeState(enemy.battleState);
            return;
        }

        enemy.UpdateAimPosition();
        enemy.FaceSteeringTarget();

        // Wait until the agent has actually finished calculating its path.
        if (enemy.agent.pathPending)
            return;

        // If the agent reached the cover point, switch to battle state.
        if (enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + 0.1f)
        {
            if (!enemy.agent.hasPath || enemy.agent.velocity.sqrMagnitude < 0.01f)
            {
                enemy.FaceKnownPlayerPosition();
                stateMachine.ChangeState(enemy.battleState);
            }
        }
    }

    #endregion
}
