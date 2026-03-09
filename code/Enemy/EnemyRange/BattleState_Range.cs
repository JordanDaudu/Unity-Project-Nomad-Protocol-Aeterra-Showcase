using UnityEngine;

public class BattleState_Range : EnemyState
{
    private EnemyRange enemy;

    private float lastTimeShot = -10f;
    private int bulletsShot = 0;
    public BattleState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        enemy.FacePlayer();

        if (WeaponOutOfBullets())
        {
            if (WeaponOnCooldown())
                AttemptToResetWeapon();

            return;
        }

        if (CanShoot())
        {
            Shoot();
        }
    }

    private void AttemptToResetWeapon() => bulletsShot = 0;

    private bool WeaponOnCooldown() => Time.time > lastTimeShot + enemy.weaponCooldownTime;

    private bool WeaponOutOfBullets() => bulletsShot >= enemy.bulletsToShoot;

    private bool CanShoot() => Time.time > lastTimeShot + 1 / enemy.fireRate;

    private void Shoot()
    {
        enemy.FireSingleBullet();
        lastTimeShot = Time.time;
        bulletsShot++;
    }
}
