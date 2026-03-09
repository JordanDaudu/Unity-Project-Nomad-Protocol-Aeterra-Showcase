using UnityEngine;

/// <summary>
/// Death state for melee enemy.
/// 
/// Responsibilities:
/// - Disable Animator and NavMeshAgent (AI stops driving movement).
/// - Enable ragdoll physics for an impact-driven death.
/// - Optionally play dissolve effect (see <see cref="EnemyDeathDissolve"/>).
/// - After a short delay, disable ragdoll colliders to prevent further interactions.
/// </summary>
public class DeadState_Melee : EnemyState
{
    private EnemyMelee enemy;

    private bool interactionDisabled;
    public DeadState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyMelee;
    }

    public override void Enter()
    {
        base.Enter();

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
}
