using UnityEngine;

/// <summary>
/// Short transition / decision-making state used after entering battle mode or after actions.
/// 
/// Behavior:
/// - Stops NavMeshAgent movement and faces the player.
/// - Waits for an Animation Event trigger (see <see cref="EnemyAnimationEvents.AnimationTrigger"/>).
/// - Once triggered, selects the next state:
///   - Ability (axe throw) if available
///   - Attack if player in range
///   - Otherwise chase
/// </summary>
public class RecoveryState_Melee : EnemyState
{
    private EnemyMelee enemy;
    public RecoveryState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.agent.isStopped = true;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.FacePlayer();

        if (triggerCalled)
        {
            if (enemy.CanThrowAxe())
                stateMachine.ChangeState(enemy.abilityState);
            else if (enemy.PlayerInAttackRange())
                stateMachine.ChangeState(enemy.attackState);
            else
                stateMachine.ChangeState(enemy.chaseState);
        }
    }
}
