/*
 * EnemyStateMachine.cs
 *
 * Minimal finite state machine (FSM) used by enemies.
 *
 * Design notes:
 * - The owning Enemy calls Update() on currentState each frame.
 * - States are pure logic objects (not MonoBehaviours) to keep transitions cheap and explicit.
 * - State transitions must go through ChangeState() to guarantee Exit/Enter ordering.
 */
public class EnemyStateMachine
{
    /// <summary>
    /// Currently active state. The owner is responsible for calling Update() each frame.
    /// </summary>
    public EnemyState currentState { get; private set; }

    /// <summary>
    /// Initializes the FSM with the provided start state (calls Enter()).
    /// </summary>
    public void Initialize(EnemyState startState)
    {
        currentState = startState;
        currentState.Enter();
    }

    /// <summary>
    /// Transitions to a new state (Exit -> Enter).
    /// </summary>
    public void ChangeState(EnemyState newState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
