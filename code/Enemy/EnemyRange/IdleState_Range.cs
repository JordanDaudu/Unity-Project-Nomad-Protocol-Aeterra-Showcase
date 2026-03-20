using UnityEngine;

/// <summary>
/// Patrol idle state for ranged enemies.
/// </summary>
/// <remarks>
/// Behavior:
/// - Stops the NavMeshAgent and plays one of several idle animation variations.
/// - After <see cref="Enemy.idleTime"/>, transitions to <see cref="MoveState_Range"/>.
/// </remarks>
public class IdleState_Range : EnemyState
{
    #region Constants

    private const int idleAnimVariationCount = 3; // Number of different idle animations to randomly choose from

    #endregion

    #region Runtime

    private EnemyRange enemy;

    #endregion

    #region Constructor
    public IdleState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        enemy.anim.SetFloat("IdleAnimIndex", Random.Range(0, idleAnimVariationCount) ); // Randomize idle animation to add variety

        if (enemy.weaponType == EnemyRangeWeaponType.Pistol || enemy.weaponType == EnemyRangeWeaponType.Revolver)
            enemy.visuals.EnableIK(false, false);
        else
            enemy.visuals.EnableIK(true, false);
        
        enemy.StopAgentImmediately();
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

    #endregion
}
