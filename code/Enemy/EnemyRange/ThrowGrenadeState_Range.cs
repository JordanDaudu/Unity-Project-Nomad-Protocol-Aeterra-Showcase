using UnityEngine;

/// <summary>
/// Ability state for ranged enemies that performs a grenade throw.
/// </summary>
/// <remarks>
/// Visual flow:
/// - Hides the primary weapon model.
/// - Enables the secondary weapon model (animation swap) and grenade model.
/// - Disables IK so the animation can fully control arms during the throw.
/// 
/// Timing:
/// - The actual grenade spawn/throw occurs inside <see cref="AbilityTrigger"/>,
///   called by an Animation Event at the moment of release.
/// - When the animation trigger is called (<see cref="EnemyState.AnimationTrigger"/>),
///   the state returns to <see cref="BattleState_Range"/>.
/// 
/// Death edge case:
/// - <see cref="DeadState_Range"/> checks <see cref="finishedThrowingGrenade"/> to decide whether to force a throw on death.
/// </remarks>
public class ThrowGrenadeState_Range : EnemyState
{
    #region Runtime

    private EnemyRange enemy;

    /// <summary>
    /// True once the grenade release event has occurred.
    /// Used to handle "dies mid-throw" edge cases.
    /// </summary>
    public bool finishedThrowingGrenade { get; private set; }

    #endregion

    #region Constructor

    public ThrowGrenadeState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    #endregion

    #region State Lifecycle


    public override void Enter()
    {
        base.Enter();

        finishedThrowingGrenade = false;

        enemy.visuals.EnableWeaponModel(false);
        enemy.visuals.EnableSecondaryWeaponModel(true);
        enemy.visuals.EnableGrenadeModel(true);
        enemy.visuals.EnableIK(false, false);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.FacePlayer();
        enemy.Aim.position = enemy.player.transform.position; // Taking precise point of the player and not the predicted position, because the grenade is slow and we want to give the player a chance to dodge it.

        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }

    #endregion

    #region Animation Events

    public override void AbilityTrigger()
    {
        base.AbilityTrigger();

        finishedThrowingGrenade = true;
        enemy.ThrowGrenade();
    }

    #endregion
}
