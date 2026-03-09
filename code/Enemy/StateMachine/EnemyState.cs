using UnityEngine;

/// <summary>
/// Base class for all enemy FSM states.
///
/// Key concept:
/// - Each state controls animation flags and time-based logic.
/// - Animation Events call back into <see cref="Enemy.AnimationTrigger"/> / <see cref="Enemy.AbilityTrigger"/>,
///   which forwards to the active state's <see cref="AnimationTrigger"/> / <see cref="AbilityTrigger"/>.
/// </summary>
public class EnemyState
{
    #region Protected References

    protected Enemy enemyBase;
    protected EnemyStateMachine stateMachine;
    protected Animator anim;

    #endregion

    #region State Data

    protected string animBoolName;
    protected float stateTimer;

    /// <summary>
    /// Set to true by animation events (see <see cref="AnimationTrigger"/>).
    /// Used as a simple "animation finished / window reached" gate.
    /// </summary>
    protected bool triggerCalled;

    #endregion

    protected EnemyState(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
    {
        this.enemyBase = enemyBase;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    #region Lifecycle

    public virtual void Enter()
    {
        // Animation booleans are used as state "tags" in the Animator Controller.
        enemyBase.anim.SetBool(animBoolName, true);

        triggerCalled = false;
    }
    public virtual void Update()
    {
        // Many states use stateTimer as a simple countdown.
        stateTimer -= Time.deltaTime;
    }

    public virtual void Exit()
    {
        enemyBase.anim.SetBool(animBoolName, false);
    }

    #endregion

    #region Animation Event Hooks

    /// <summary>
    /// Called via <see cref="EnemyAnimationEvents.AnimationTrigger"/> to inform the current state
    /// that an animation event marker has been reached.
    /// </summary>
    public void AnimationTrigger() => triggerCalled = true;

    /// <summary>
    /// Optional hook called via <see cref="EnemyAnimationEvents.AbilityEvent"/> for ability-specific timing.
    /// Default is no-op.
    /// </summary>
    public virtual void AbilityTrigger()
    {

    }

    #endregion
}
