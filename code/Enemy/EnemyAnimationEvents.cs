using UnityEngine;

/// <summary>
/// Animation Event receiver for enemy animations.
///
/// Why this exists:
/// - The Animator timeline is the "source of truth" for timing windows (attack impact, ability release, etc.).
/// - Animation Events call methods on this component.
/// - This component forwards those calls to the owning <see cref="Enemy"/>, which then forwards to the active FSM state.
/// 
/// Key connections:
/// - Enemy -> EnemyStateMachine -> EnemyState
/// - Enemy.AnimationTrigger() -> currentState.AnimationTrigger()
/// - Enemy.AbilityTrigger()   -> currentState.AbilityTrigger()
/// </summary>
public class EnemyAnimationEvents : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    public void AnimationTrigger() => enemy.AnimationTrigger();

    public void StartManualMovement() => enemy.ActivateManualMovement(true);
    public void StopManualMovement() => enemy.ActivateManualMovement(false);
    public void StartManualRotation() => enemy.ActivateManualRotation(true);
    public void StopManualRotation() => enemy.ActivateManualRotation(false);
    public void AbilityEvent() => enemy.AbilityTrigger();
    public void EnableIK() => enemy.visuals.EnableIK(true, true, 1.5f);
    public void EnableWeaponModel()
    {
        enemy.visuals.EnableWeaponModel(true);
        enemy.visuals.EnableSecondaryWeaponModel(false);
    }
}
