using UnityEngine;

/// <summary>
/// Ability execution state for melee enemies (axe throw variant).
/// 
/// Animation-driven movement:
/// - ManualRotationActive / ManualMovementActive are toggled by <see cref="EnemyAnimationEvents"/>.
/// - While active, enemy rotates toward player and moves forward.
/// 
/// Ability timing:
/// - The actual axe spawn/release happens in <see cref="AbilityTrigger"/>, called via an Animation Event.
/// </summary>
public class AbilityState_Melee : EnemyState
{
    private EnemyMelee enemy;
    private Vector3 movementDirection;

    private const float MAX_MOVEMENT_DISTANCE = 20f;

    private float moveSpeed; // Local copy so we can restore Enemy.moveSpeed in Exit().
    public AbilityState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.visuals.EnableWeaponModel(true);

        moveSpeed = enemy.walkSpeed;
        movementDirection = enemy.transform.position + (enemy.transform.forward * MAX_MOVEMENT_DISTANCE);
    }

    public override void Exit()
    {
        base.Exit();

        // Restore movement speed and default recovery animation selection after ability.
        enemy.walkSpeed = moveSpeed;
        enemy.anim.SetFloat("RecoveryIndex", 0);
    }

    public override void Update()
    {
        base.Update();

        if (enemy.ManualRotationActive())
        {
            enemy.FacePlayer();
            movementDirection = enemy.transform.position + (enemy.transform.forward * MAX_MOVEMENT_DISTANCE);
        }

        if (enemy.ManualMovementActive())
        {
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, movementDirection, enemy.walkSpeed * Time.deltaTime);
        }

        if (triggerCalled)
            stateMachine.ChangeState(enemy.recoveryState);
    }

    public override void AbilityTrigger()
    {
        base.AbilityTrigger();

        enemy.ThrowAxe();
    }
}
