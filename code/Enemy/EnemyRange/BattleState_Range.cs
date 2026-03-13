using Unity.Burst.Intrinsics;
using UnityEngine;

/// <summary>
/// Main combat state for ranged enemies.
/// 
/// Responsibilities:
/// - Face the player while fighting
/// - Fire weapon bursts according to weapon stats
/// - Periodically ask the cover system whether a meaningfully better cover point exists
/// - Transition back to RunToCover when repositioning is needed
/// </summary>
public class BattleState_Range : EnemyState
{
    private EnemyRange enemy;

    private float lastTimeShot = -10f;
    private int bulletsShot = 0;

    private int bulletsPerAttack;
    private float weaponCooldown;

    private bool firstTimeAttack = true;

    public BattleState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    public override void Enter()
    {
        base.Enter();
        SetupValuesForFirstAttack();

        enemy.DetachAimFromHierarchy();
        enemy.agent.isStopped = true;
        enemy.agent.velocity = Vector3.zero;

        enemy.visuals.EnableIK(true, true);

        stateTimer = enemy.attackDelay;

        if (enemy.CanUseCovers)
            enemy.CoverController.RefreshNearbyCovers(true);
    }

    public override void Exit()
    {
        base.Exit();

        enemy.visuals.EnableIK(false, false);
    }

    public override void Update()
    {
        base.Update();

        enemy.UpdateAimPosition();

        if (!enemy.HasRecentTargetKnowledge())
        {
            enemy.ExitBattleMode();
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        enemy.FaceTarget(enemy.GetCurrentAimTargetPosition());

        if (enemy.CanThrowGrenade())
            stateMachine.ChangeState(enemy.throwGrenadeState);

        if (!enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(enemy.advanceToPlayerState);
            return;
        }

        if (enemy.IsPlayerInCombatRange() == false && enemy.IsUnstoppable == false)
        {
            stateMachine.ChangeState(enemy.advanceToPlayerState);
            return;
        }

        // After returning from Advance, wait a short delay before looking for a new cover spot.
        if (enemy.CanRepositionCover && enemy.CanSearchForCoverAfterAdvance)
        {
            // Occasionally check if a clearly better cover position exists.
            // If so, reserve it and transition back to the movement state.
            if (enemy.TryReserveBetterCover(out Transform betterCover))
            {
                Debug.Log($"[{enemy.name}] Found better cover at {betterCover.position}. Transitioning to run to cover state.", enemy);
                stateMachine.ChangeState(enemy.runToCoverState);
                return;
            }
        }

        // Only when entering battle a small delay before firing for animation purposes. After that
        if (stateTimer > 0f)
            return;

        if (WeaponOutOfBullets())
        {
            if(enemy.IsUnstoppable && UnstoppableWalkReady())
            {
                enemy.advanceDuration = weaponCooldown;
                stateMachine.ChangeState(enemy.advanceToPlayerState);
            }

            if (CooldownFinished())
                ResetWeaponAttackCycle();

            return;
        }

        if (CanShoot() && enemy.CanFireAtPlayer())
            Shoot();
    }

    private bool UnstoppableWalkReady()
    {
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.player.position);
        bool outOfStoppingDistance = distanceToPlayer > enemy.advanceStoppingDistance;
        bool unstoppableWalkOnCooldown = Time.time < enemy.weaponData.MinWeaponCooldown + enemy.advanceToPlayerState.LastTimeAdvanced;

        return outOfStoppingDistance && unstoppableWalkOnCooldown == false;
    }

    #region Weapon Region

    /// <summary>
    /// Resets the current firing cycle after the cooldown has finished.
    /// </summary>
    private void ResetWeaponAttackCycle()
    {
        bulletsShot = 0;
        bulletsPerAttack = enemy.weaponData.RollBulletsPerAttack();
        weaponCooldown = enemy.weaponData.RollWeaponCooldown();
    }

    /// <summary>
    /// Returns true when the post-burst cooldown has finished.
    /// </summary>
    private bool CooldownFinished() => Time.time > lastTimeShot + weaponCooldown;

    /// <summary>
    /// Returns true when the current burst has spent all bullets.
    /// </summary>
    private bool WeaponOutOfBullets() => bulletsShot >= bulletsPerAttack;

    /// <summary>
    /// Returns true when enough time has passed to fire the next bullet.
    /// </summary>
    private bool CanShoot() => Time.time > lastTimeShot + 1f / enemy.weaponData.FireRate;

    /// <summary>
    /// Fires one bullet and advances the burst counter.
    /// </summary>
    private void Shoot()
    {
        enemy.FireSingleBullet();
        lastTimeShot = Time.time;
        bulletsShot++;
    }

    private void SetupValuesForFirstAttack()
    {
        if (firstTimeAttack)
        {
            firstTimeAttack = false;
            bulletsPerAttack = enemy.weaponData.RollBulletsPerAttack();
            weaponCooldown = enemy.weaponData.RollWeaponCooldown();
        }
    }
    #endregion
}