using UnityEngine;

/// <summary>
/// Base class for enemy weapon visual models.
/// </summary>
/// <remarks>
/// A weapon model represents the visual setup of a weapon attached to an enemy,
/// such as a melee weapon mesh or a ranged weapon mesh.
///
/// This class is used by <see cref="EnemyVisuals"/> to:
/// <list type="bullet">
/// <item><description>Determine whether a weapon model matches a given enemy.</description></item>
/// <item><description>Activate the selected weapon model.</description></item>
/// <item><description>Apply an optional animator override controller.</description></item>
/// <item><description>Trigger optional visual effects such as weapon trails.</description></item>
/// </list>
///
/// Subclasses are responsible for implementing their own compatibility logic
/// through <see cref="IsCompatibleWith(Enemy)"/>.
/// </remarks>
public abstract class EnemyWeaponModel : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Optional animator override controller applied when this weapon model is selected.")]
    [SerializeField] private AnimatorOverrideController overrideController;

    /// <summary>
    /// Gets the animator override controller associated with this weapon model.
    /// </summary>
    public AnimatorOverrideController OverrideController => overrideController;

    /// <summary>
    /// Layer index to activate when this weapon model is selected.
    /// 0 means base layer only.
    /// </summary>
    public virtual int AnimationLayerIndex => 0;

    // Whether this weapon setup needs left-hand IK positioning.
    // Default is false because not every weapon uses an off-hand pose.
    public virtual bool RequiresLeftHandIK => false;

    // Optional IK targets provided by specific weapon models.
    // Base class returns null because many weapons do not need them.
    public virtual Transform LeftHandTarget => null;
    public virtual Transform LeftElbowTarget => null;

    /// <summary>
    /// Determines whether this weapon model is compatible with the specified enemy.
    /// </summary>
    /// <param name="enemy">The enemy whose visuals are being configured.</param>
    /// <returns>
    /// <c>true</c> if this weapon model can be used by the specified enemy;
    /// otherwise, <c>false</c>.
    /// </returns>
    public abstract bool IsCompatibleWith(Enemy enemy);

    /// <summary>
    /// Enables or disables trail effects associated with this weapon model.
    /// </summary>
    /// <param name="enable">
    /// <c>true</c> to enable trail effects; <c>false</c> to disable them.
    /// </param>
    /// <remarks>
    /// The base implementation does nothing because not every weapon model has trail effects.
    /// Subclasses can override this method when they provide trail visuals.
    /// </remarks>
    public virtual void EnableTrailEffect(bool enable)
    {
        // Intentionally empty. Not all weapon models have trail effects.
    }
}