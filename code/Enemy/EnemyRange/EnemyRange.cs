using UnityEngine;

public class EnemyRange : Enemy
{
    public Transform weaponHolder;
    public EnemyRangeWeaponType weaponType;

    public float fireRate = 1f; // Bullets per second
    public float bulletSpeed = 20f;
    public GameObject bulletPrefab;
    public Transform gunPoint;
    public int bulletsToShoot = 5; // Bullets tp shoot before weapon cooldown starts
    public float weaponCooldownTime = 1.5f; // Weapon cooldown duration after shooting all bullets

    public IdleState_Range idleState { get; private set; }
    public MoveState_Range moveState { get; private set; }
    public BattleState_Range battleState { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        idleState = new IdleState_Range(this, stateMachine, "Idle");
        moveState = new MoveState_Range(this, stateMachine, "Move");
        battleState = new BattleState_Range(this, stateMachine, "Battle");
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);

        visuals.SetupLook();
    }

    protected override void Update()
    {
        base.Update();

        stateMachine.currentState.Update();
    }

    public override void EnterBattleMode()
    {
        if (inBattleMode)
            return;

        base.EnterBattleMode();
        stateMachine.ChangeState(battleState);
    }

    public void FireSingleBullet()
    {
        anim.SetTrigger("Shoot");

        Vector3 bulletDirection = ((player.position + Vector3.up) - gunPoint.position).normalized;

        GameObject newBullet = ObjectPool.Instance.GetObject(bulletPrefab);

        newBullet.transform.position = gunPoint.position;
        newBullet.transform.rotation = Quaternion.LookRotation(gunPoint.forward);

        newBullet.GetComponent<EnemyBullet>().BulletSetup();

        Rigidbody rbNewBullet = newBullet.GetComponent<Rigidbody>();

        rbNewBullet.mass = 20 / bulletSpeed;
        rbNewBullet.linearVelocity = bulletDirection * bulletSpeed;
    }
}
