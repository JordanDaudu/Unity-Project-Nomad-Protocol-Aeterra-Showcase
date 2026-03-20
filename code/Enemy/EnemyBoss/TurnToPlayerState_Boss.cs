using UnityEngine;

/// <summary>
/// Temporary combat state used when the boss is close enough to attack,
/// but is not yet facing the player within the required attack angle.
/// </summary>
/// <remarks>
/// This state rotates the boss in place and transitions into <see cref="AttackState_Boss"/> once:
/// - the smooth turning helper reports finished, OR
/// - the boss is already facing within <see cref="EnemyBoss.AttackFacingAngle"/>
///
/// Key connection:
/// - Uses <see cref="Enemy.TurnToPlayerSmooth"/> for rotation smoothing.
/// </remarks>
public class TurnToPlayerState_Boss : EnemyState
{
    #region Runtime

    private EnemyBoss enemy;

    #endregion

    #region Construction

    public TurnToPlayerState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        // Turn-in-place: stop agent movement so rotation is stable.
        enemy.StopAgentImmediately();
    }

    public override void Update()
    {
        base.Update();

        if (!enemy.PlayerInAttackRange())
        {
            stateMachine.ChangeState(enemy.moveState);
            return;
        }

        bool finishedTurning = enemy.TurnToPlayerSmooth(enemy.AttackTurnSpeed, 3f);

        if (finishedTurning || enemy.IsFacingPlayerForAttack())
        {
            stateMachine.ChangeState(enemy.attackState);
        }
    }

    #endregion
}