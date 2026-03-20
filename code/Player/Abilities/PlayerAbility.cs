using UnityEngine;

/// <summary>
/// Base class for player abilities (roll, dash, grenade, shield, etc.).
/// </summary>
/// <remarks>
/// Design goals:
/// - Abilities are modular components attached to the Player.
/// - Abilities share a consistent cooldown + activation contract.
/// - Ability input routing is handled externally by <see cref="PlayerAbilityController"/>.
///
/// Typical lifecycle:
/// 1) Controller calls <see cref="CanActivate"/> to validate activation.
/// 2) If valid, controller calls <see cref="Activate"/>.
/// 3) Ability sets <see cref="isActive"/> while running and updates <see cref="lastUseTime"/>.
/// </remarks>
public abstract class PlayerAbility : MonoBehaviour
{
    #region Inspector

    [Tooltip("Minimum time between activations (seconds).")]
    [SerializeField] protected float cooldown;

    #endregion

    #region Runtime

    /// <summary>
    /// Last time (Time.time) when the ability was activated.
    /// Negative default allows immediate usage when the scene starts.
    /// </summary>
    protected float lastUseTime = -999f;

    /// <summary>
    /// Cached reference to the <see cref="Player"/> component.
    /// </summary>
    protected Player player;

    /// <summary>
    /// True while the ability is currently running.
    /// </summary>
    protected bool isActive;

    /// <summary>
    /// Public read-only state flag for other systems (UI, movement gating, etc.).
    /// </summary>
    public bool IsActive => isActive;

    #endregion

    #region Unity Callbacks

    protected virtual void Awake()
    {
        // Abilities live on the player, so Player is expected to exist on the same GameObject.
        player = GetComponent<Player>();
    }

    #endregion

    #region Activation Contract

    /// <summary>
    /// Returns true if the ability is allowed to activate right now.
    /// </summary>
    /// <remarks>
    /// Base gate:
    /// - Ability is not already active
    /// - Cooldown has elapsed since <see cref="lastUseTime"/>
    ///
    /// Derived abilities should call base.CanActivate() first and then apply extra rules
    /// (e.g., "not in mid-air", "not movement locked", "has stamina", etc.).
    /// </remarks>
    public virtual bool CanActivate()
    {
        return !isActive && Time.time >= lastUseTime + cooldown;
    }

    /// <summary>
    /// Starts the ability effect.
    /// </summary>
    /// <remarks>
    /// Implementations should:
    /// - Set isActive = true
    /// - Set lastUseTime = Time.time
    /// - Start coroutines/timers as needed
    /// - Cleanly exit by setting isActive = false
    /// </remarks>
    public abstract void Activate();

    #endregion
}