using UnityEngine;

/// <summary>
/// Death state for ranged enemies.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Ensure grenade throw is resolved if the enemy dies mid-throw (avoids "lost grenade").
/// - Disable Animator and NavMeshAgent so ragdoll owns the body.
/// - Enable ragdoll and trigger dissolve effect.
/// - After a short delay, disable ragdoll colliders to prevent further interactions.
/// </remarks>
public class DeadState_Range : EnemyState
{
    #region Runtime

    private EnemyRange enemy;
    private bool interactionDisabled;

    #endregion

    #region Constructor

    public DeadState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyRange;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        if (enemy.throwGrenadeState.finishedThrowingGrenade == false)
            enemy.ThrowGrenade();

        // Disable animation/agent so ragdoll fully owns the body.
        enemy.anim.enabled = false;
        enemy.agent.enabled = false;

        enemy.ragdoll.RagdollActive(true);

        interactionDisabled = false;
        stateTimer = 1.5f; // Time before we disable colliders and interactions, allowing the initial death impact to play out

        if (enemy.deathDissolve != null)
            enemy.deathDissolve.PlayDeathDissolve();
        else
            Debug.LogWarning($"EnemyDeathDissolve missing on {enemy.name}", enemy);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        DisableInteractionIfShould();
    }

    #endregion

    #region Helpers

    private void DisableInteractionIfShould()
    {
        if (interactionDisabled == false && stateTimer < 0)
        {
            // Turn off ragdoll so it no longer interacts with bullets/physics.
            enemy.ragdoll.RagdollActive(false);
            enemy.ragdoll.CollidersActive(false);
            interactionDisabled = true;
        }
    }

    #endregion
}
