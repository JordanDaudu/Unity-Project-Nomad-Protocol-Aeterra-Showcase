using UnityEngine;

/// <summary>
/// Ranged enemy state that advances toward the player's known position.
/// </summary>
/// <remarks>
/// Used when:
/// - The enemy is out of effective combat range
/// - The enemy temporarily cannot see the player and needs to close distance or reach last known position
/// - The enemy has the Unstoppable perk and must keep advancing for a fixed duration
///
/// Key connections:
/// - Uses <see cref="EnemyRange.GetKnownPlayerPosition"/> from the perception system.
/// - Releases any reserved cover when entering this state (see <see cref="EnemyRange.ReleaseReservedCover"/>).
/// - Transitions back to <see cref="BattleState_Range"/> once close enough and/or vision returns.
/// </remarks>
public class AdvanceToPlayer_Range : EnemyState
{
    #region Runtime

    private EnemyRange enemy;
    private Vector3 targetPosition;

    private float lastTimeAdvanced = float.NegativeInfinity;

    #endregion

    #region Constructor
    public AdvanceToPlayer_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    #endregion

    #region Public API

    public float LastTimeAdvanced => lastTimeAdvanced;

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        enemy.visuals.EnableIK(true, false);

        // The enemy is no longer holding its old cover position once it starts advancing.
        enemy.ReleaseReservedCover();

        enemy.agent.isStopped = false;
        enemy.agent.speed = enemy.advanceSpeed;

        if (enemy.IsUnstoppable)
        {
            enemy.visuals.EnableIK(true, false);
            stateTimer = enemy.advanceDuration;
        }
    }

    public override void Exit()
    {
        base.Exit();

        lastTimeAdvanced = Time.time;
    }

    public override void Update()
    {
        base.Update();

        enemy.UpdateAimPosition();

        if (!enemy.HasRecentTargetKnowledge())
        {
            enemy.StopAgentImmediately();
            enemy.ExitBattleMode();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        targetPosition = enemy.GetKnownPlayerPosition();
        enemy.agent.SetDestination(targetPosition);
        enemy.FaceSteeringTarget();

        if (CanEnterBattleState())
        {
            stateMachine.ChangeState(enemy.battleState);
            return;
        }

        if (!enemy.CanSeePlayer() && ReachedLastKnownPosition())
        {
            enemy.StopAgentImmediately();

            enemy.ExitBattleMode();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }
    }

    #endregion

    #region Helpers

    private bool CanEnterBattleState()
    {
        if (enemy.agent.pathPending)
            return false;

        bool closeEnoughAndSeePlayer = enemy.CanSeePlayer() &&
               enemy.agent.remainingDistance <= enemy.advanceStoppingDistance;

        if (enemy.IsUnstoppable)
        {
            return stateTimer <= 0f || closeEnoughAndSeePlayer;
        }

        return closeEnoughAndSeePlayer;
    }

    private bool ReachedLastKnownPosition()
    {
        if (enemy.agent.pathPending)
            return false;

        return enemy.agent.remainingDistance <= enemy.lastKnownPositionArrivalDistance;
    }

    #endregion
}
