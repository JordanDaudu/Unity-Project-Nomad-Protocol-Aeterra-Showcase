using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Melee attack execution state.
/// 
/// Animation-driven movement:
/// - ManualRotationActive / ManualMovementActive are toggled by <see cref="EnemyAnimationEvents"/>.
/// - When active, this state rotates toward player and moves toward an attackDirection target.
/// 
/// Exit behavior:
/// - On Exit(), configures the next attack data and a recovery animation index.
/// </summary>
public class AttackState_Melee : EnemyState
{
    const float playerCloseDistance = 1.4f;
    const int slashAttackAnimCount = 6;

    private const float MAX_ATTACK_DISTANCE = 50f;

    private EnemyMelee enemy;

    private Vector3 attackDirection;
    private float attackMoveSpeed;

    public AttackState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();
        enemy.UpdateAttackData();
        enemy.visuals.EnableWeaponModel(true);
        enemy.visuals.EnableWeaponTrail(true);

        attackMoveSpeed = enemy.attackData.moveSpeed;

        // Animator parameters used to select attack animation and tune its playback speed.
        enemy.anim.SetFloat("AttackAnimationSpeed", enemy.attackData.animationSpeed);
        enemy.anim.SetFloat("AttackIndex", enemy.attackData.attackIndex);

        // Randomizes a slash variant (Animator should use this to pick one of multiple slashes).
        enemy.anim.SetFloat("SlashAttackIndex", Random.Range(0, slashAttackAnimCount));

        // Stop NavMesh movement during manual attack movement.
        enemy.agent.isStopped = true;
        enemy.agent.velocity = Vector3.zero;

        // Initial attack direction is straight forward.
        attackDirection = enemy.transform.position + (enemy.transform.forward * MAX_ATTACK_DISTANCE);
    }

    public override void Exit()
    {
        base.Exit();
        SetupNextAttack();

        enemy.visuals.EnableWeaponTrail(false);
    }

    public override void Update()
    {
        base.Update();

        if (enemy.ManualRotationActive())
        {
            enemy.FacePlayer();
            attackDirection = enemy.transform.position + (enemy.transform.forward * MAX_ATTACK_DISTANCE);
        }

        if (enemy.ManualMovementActive())
        {
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, attackDirection, attackMoveSpeed * Time.deltaTime);
        }

        if (triggerCalled)
        {
            if (enemy.PlayerInAttackRange())
            {
                stateMachine.ChangeState(enemy.recoveryState);
                return;
            }

            // Weighted transition choice
            float roll = Random.value; // 0..1

            if (roll < 0.90f)
            {

                stateMachine.ChangeState(enemy.recoveryState);
            }
            else
                stateMachine.ChangeState(enemy.chaseState);
        }
    }

    private void SetupNextAttack()
    {
        int recoveryIndex = PlayerClose() ? 1 : 0;

        // Animator uses this to select which recovery animation to play.
        enemy.anim.SetFloat("RecoveryIndex", recoveryIndex);

        // Choose next attack parameters so next AttackState uses updated data.
        enemy.attackData = UpdatedAttackData();
    }

    private bool PlayerClose() => Vector3.Distance(enemy.transform.position, enemy.player.position) <= playerCloseDistance;

    private AttackData_EnemyMelee UpdatedAttackData()
    {
        // Copy list so filtering does not mutate the original attack list.
        List<AttackData_EnemyMelee> validAttack = new List<AttackData_EnemyMelee>(enemy.attackList);

        // If too close, avoid charge attacks.
        if (PlayerClose())
            validAttack.RemoveAll(parameter => parameter.attackType == AttackType_Melee.Charge);

        int random = Random.Range(0, validAttack.Count);

        return validAttack[random];
    }
}
