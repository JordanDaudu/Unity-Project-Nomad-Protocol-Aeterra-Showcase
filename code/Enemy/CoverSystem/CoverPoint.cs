using UnityEngine;

/// <summary>
/// Identifies which side of a cover object a cover point belongs to.
/// This is used when evaluating whether a point is on the safe or exposed side
/// relative to a threat.
/// </summary>
public enum CoverPointSide
{
    Front,
    Back,
    Left,
    Right
}

/// <summary>
/// Represents a single tactical slot around a <see cref="Cover"/> object.
/// 
/// A cover point is what enemies actually reserve and move to.
/// This is more precise than selecting the cover object itself, because
/// different sides of the same cover may be safe or exposed depending on
/// where the threat is.
/// </summary>
[DisallowMultipleComponent]
public class CoverPoint : MonoBehaviour
{
    [Header("Generated Data")]

    [Tooltip("Which side of the owning cover this point belongs to.")]
    [SerializeField] private CoverPointSide side;

    [Tooltip("Local outward direction from the cover toward this point. Used for scoring safe/exposed sides.")]
    [SerializeField] private Vector3 localDirectionFromCover = Vector3.forward;

    /// <summary>
    /// The cover object that owns this point.
    /// </summary>
    private Cover owner;

    /// <summary>
    /// The cover controller currently occupying this point.
    /// Only one enemy cover controller may reserve a point at a time.
    /// </summary>
    private EnemyCoverController occupant;

    /// <summary>The owning <see cref="Cover"/>.</summary>
    public Cover Owner => owner;

    /// <summary>The side of the cover this point belongs to.</summary>
    public CoverPointSide Side => side;

    /// <summary>Whether this point is currently reserved by an enemy.</summary>
    public bool IsOccupied => occupant != null;

    /// <summary>Convenience access to the world position of this point.</summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// Returns the outward direction from the cover toward this point in world space.
    /// 
    /// This is useful for evaluating whether the point is on the protected side
    /// or the exposed side relative to a threat.
    /// </summary>
    public Vector3 WorldDirectionFromCover
    {
        get
        {
            Transform reference = owner != null ? owner.transform : transform.parent;

            if (reference == null)
                return localDirectionFromCover.normalized;

            return reference.TransformDirection(localDirectionFromCover).normalized;
        }
    }

    private void Awake()
    {
        // Try to automatically find the owning cover if it was not assigned explicitly.
        if (owner == null)
            owner = GetComponentInParent<Cover>();
    }

    /// <summary>
    /// Assigns the owning cover object.
    /// Called by <see cref="Cover"/> during initialization.
    /// </summary>
    public void SetOwner(Cover coverOwner)
    {
        owner = coverOwner;
    }

    /// <summary>
    /// Sets data for points generated automatically by <see cref="Cover"/>.
    /// </summary>
    /// <param name="pointSide">Which side of the cover this point belongs to.</param>
    /// <param name="localDirection">Local outward direction from the cover to this point.</param>
    public void ConfigureGeneratedData(CoverPointSide pointSide, Vector3 localDirection)
    {
        side = pointSide;
        localDirectionFromCover = localDirection.normalized;
    }

    /// <summary>
    /// Returns true if this point is either free or already reserved by the same user.
    /// </summary>
    public bool CanBeUsedBy(EnemyCoverController user)
    {
        return occupant == null || occupant == user;
    }

    /// <summary>
    /// Attempts to reserve this point for the given controller.
    /// </summary>
    /// <returns>True if the reservation succeeded.</returns>
    public bool Reserve(EnemyCoverController user)
    {
        if (!CanBeUsedBy(user))
            return false;

        occupant = user;
        return true;
    }

    /// <summary>
    /// Releases this point only if the given controller is the current occupant.
    /// </summary>
    public void Release(EnemyCoverController user)
    {
        if (occupant == user)
            occupant = null;
    }
}