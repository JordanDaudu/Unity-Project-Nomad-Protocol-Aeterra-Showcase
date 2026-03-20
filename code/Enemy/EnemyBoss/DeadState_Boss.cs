using UnityEngine;

/// <summary>
/// Death state for the boss.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Ensure active abilities are cleaned up (e.g., stop flamethrower) so pooled VFX aren't left running.
/// - Notify global systems of the boss death (music, encounter systems, progression, etc.).
/// - Disable Animator and NavMeshAgent so ragdoll fully owns the body.
/// - Trigger dissolve visual and then disable ragdoll colliders after a short interaction window.
/// </remarks>
public class DeadState_Boss : EnemyState
{
    #region Runtime

    private EnemyBoss enemy;
    private bool interactionDisabled;

    #endregion

    #region Construction

    public DeadState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as EnemyBoss;
    }

    #endregion

    #region State Lifecycle

    public override void Enter()
    {
        base.Enter();

        // Safety: ensure flamethrower doesn't remain active after death.
        enemy.abilityState.DisableFlameThrower();

        // Used for systems like CombatMusicCoordinator / encounter logic.
        enemy.NotifyDeath();

        // Disable animation/agent so ragdoll fully owns the body.
        enemy.anim.enabled = false;
        enemy.agent.enabled = false;

        enemy.ragdoll.RagdollActive(true);

        interactionDisabled = false;

        // Time before we disable colliders and interactions, allowing the initial death impact to play out.
        stateTimer = 1.5f;

        if (enemy.deathDissolve != null)
            enemy.deathDissolve.PlayDeathDissolve();
        else
            Debug.LogWarning($"EnemyDeathDissolve missing on {enemy.name}", enemy);
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