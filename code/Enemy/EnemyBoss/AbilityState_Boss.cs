using UnityEngine;

/// <summary>
/// Boss ability execution state.
/// </summary>
/// <remarks>
/// This state is entered when the boss decides to perform its special ability.
/// The actual effect is triggered by an animation event (see <see cref="EnemyAnimationEvents.AbilityEvent"/>)
/// which calls <see cref="EnemyState.AbilityTrigger"/>.
////
//// Ability behavior depends on <see cref="BossWeaponType"/>:
/// - FlameThrower: starts/stops a particle system for a fixed duration.
/// - Hammer: spawns an activation FX at the impact point.
///
/// Key connections:
/// - <see cref="EnemyBoss.SetAbilityOnCooldown"/> is called on exit.
/// - <see cref="EnemyBossVisuals"/> manages weapon trail + battery UI visuals.
/// </remarks>
public class AbilityState_Boss : EnemyState
{
    #region Runtime

    private EnemyBoss enemy;

    #endregion

    #region Construction

    public AbilityState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        // Used by ShouldDisableFlamethrower().
        stateTimer = enemy.flameThrowerDuration;

        // Ability locks the boss in place while the animation + effect plays.
        enemy.agent.isStopped = true;
        enemy.agent.velocity = Vector3.zero;

        // Trail starts enabled for anticipation and is disabled once flamethrower begins.
        enemy.bossVisuals.enableWeaponTrail(true);
    }

    public override void Exit()
    {
        base.Exit();

        enemy.SetAbilityOnCooldown();
        enemy.bossVisuals.ResetBatteries();
    }

    public override void Update()
    {
        base.Update();

        enemy.FacePlayer();

        if (ShouldDisableFlamethrower())
            DisableFlameThrower();

        if (triggerCalled)
            stateMachine.ChangeState(enemy.moveState);
    }

    #endregion

    #region Animation Event Hook

    public override void AbilityTrigger()
    {
        base.AbilityTrigger();

        if (enemy.bossWeaponType == BossWeaponType.FlameThrower)
        {
            enemy.ActivateFlamethrower(true);
            enemy.bossVisuals.DischargeBatteries();
            enemy.bossVisuals.enableWeaponTrail(false);
        }

        if (enemy.bossWeaponType == BossWeaponType.Hammer)
        {
            enemy.ActivateHammer();
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Stops the flamethrower effect if it is currently active.
    /// </summary>
    public void DisableFlameThrower()
    {
        if (enemy.bossWeaponType != BossWeaponType.FlameThrower)
            return;

        if (enemy.flameThrowerActive == false)
            return;

        enemy.ActivateFlamethrower(false);
    }

    private bool ShouldDisableFlamethrower()
    {
        // stateTimer is set to flameThrowerDuration in Enter().
        return stateTimer <= 0f;
    }

    #endregion
}