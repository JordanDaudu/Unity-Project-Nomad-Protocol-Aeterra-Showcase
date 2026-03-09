using UnityEngine;

/// <summary>
/// Patrol idle state.
/// 
/// Behavior:
/// - Waits for <see cref="Enemy.idleTime"/> seconds, then transitions to <see cref="MoveState_Melee"/>.
/// - Battle mode transitions are handled by the owning <see cref="EnemyMelee"/>.
/// </summary>
public class IdleState_Melee : EnemyState
{
    private EnemyMelee enemy;
    public IdleState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = enemyBase.idleTime;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();


        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.moveState);
    }
}
