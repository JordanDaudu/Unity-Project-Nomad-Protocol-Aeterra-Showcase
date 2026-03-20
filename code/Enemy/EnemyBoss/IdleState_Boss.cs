using UnityEngine;

/// <summary>
/// Boss idle/combat decision state.
/// </summary>
/// <remarks>
/// In patrol mode, this state functions like a regular idle pause.
/// In battle mode, it becomes the "decision" state that chooses between:
/// - attack (if in range and facing)
/// - turn to player (if in range but not facing)
/// - move (if out of range)
/// </remarks>
public class IdleState_Boss : EnemyState
{
    #region Runtime

    private EnemyBoss enemy;

    #endregion

    #region Construction

    public IdleState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        // Ensure we fully stop any residual agent movement before deciding next action.
        enemy.agent.velocity = Vector3.zero;
        stateTimer = enemyBase.idleTime;
    }

    public override void Update()
    {
        base.Update();

        if (enemy.inBattleMode && enemy.PlayerInAttackRange())
        {
            // If we can attack, do so. Otherwise, rotate in place toward player first.
            if (enemy.CanUseAbility())
                stateMachine.ChangeState(enemy.abilityState);
            else if (enemy.CanUseJumpAttack())
                stateMachine.ChangeState(enemy.jumpAttackState);
            else if (enemy.IsFacingPlayerForAttack())
                stateMachine.ChangeState(enemy.attackState);
            else
                stateMachine.ChangeState(enemy.turnToPlayerState);

            return;
        }

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.moveState);
    }

    #endregion
}