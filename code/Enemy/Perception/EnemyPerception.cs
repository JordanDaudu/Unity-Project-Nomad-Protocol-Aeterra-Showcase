using UnityEngine;

/// <summary>
/// Shared perception and target-memory component used by all enemy types.
/// 
/// Responsibilities:
/// - Detect whether the target is currently visible
/// - Apply different field-of-view rules for initial detection vs. active combat
/// - Remember the last seen target position for a limited time
/// - Expose a fair "known target position" that does not cheat through walls forever
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [Header("Vision Setup")]

    [Tooltip("Point from which the enemy checks vision. Usually the head or eye transform. If left empty, the enemy uses its transform position plus a small upward offset.")]
    [SerializeField] private Transform eyePoint;

    [Tooltip("Maximum distance at which this enemy can visually detect the target.")]
    [SerializeField] private float sightRange = 18f;

    [Tooltip("Field of view angle used before combat starts. This controls how wide the enemy can see when initially spotting the player.")]
    [SerializeField, Range(1f, 360f)] private float detectionViewAngle = 120f;

    [Tooltip("Field of view angle used after combat has already started. Usually wider than detection view so the enemy does not forget the player too easily during battle.")]
    [SerializeField, Range(1f, 360f)] private float combatViewAngle = 200f;

    [Tooltip("Layers that can block or validate line of sight. This should usually include the world and the player, but exclude things like enemy bullets, helper objects, and irrelevant trigger layers.")]
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Header("Memory")]

    [Tooltip("How long the enemy remembers the player's last seen position after losing direct sight.")]
    [SerializeField] private float memoryDuration = 3f;

    [Tooltip("Small grace period after sight is lost before visibility is considered fully broken. Helps reduce flickering at corners or near LOS edges.")]
    [SerializeField] private float lostSightGraceTime = 0.15f;

    private Transform target;
    private Transform targetAimPoint;

    private bool hadVisualContact;
    private float lastSeenTime = float.NegativeInfinity;
    private float lastVisibleTime = float.NegativeInfinity;

    public bool IsTargetVisible { get; private set; }
    public bool HadVisualContact => hadVisualContact;
    public float LastSeenTime => lastSeenTime;
    public float TimeSinceLastSeen => hadVisualContact ? Time.time - lastSeenTime : float.PositiveInfinity;
    public Vector3 LastSeenPosition { get; private set; }

    /// <summary>
    /// Returns true while the enemy still has fresh target knowledge from recent sight.
    /// </summary>
    public bool HasTargetKnowledge => hadVisualContact && Time.time <= lastSeenTime + memoryDuration;

    /// <summary>
    /// Position the enemy should currently reason about.
    /// Visible target -> live aim point.
    /// Hidden target -> last seen position.
    /// </summary>
    public Vector3 KnownTargetPosition
    {
        get
        {
            if (IsTargetVisible)
                return GetTargetAimPosition();

            return LastSeenPosition;
        }
    }

    public void SetTarget(Transform target, Transform targetAimPoint = null)
    {
        this.target = target;
        this.targetAimPoint = targetAimPoint != null ? targetAimPoint : target;

        if (this.targetAimPoint != null)
            LastSeenPosition = this.targetAimPoint.position;
    }

    public void SetTargetAimPoint(Transform targetAimPoint)
    {
        this.targetAimPoint = targetAimPoint != null ? targetAimPoint : target;

        if (!hadVisualContact && this.targetAimPoint != null)
            LastSeenPosition = this.targetAimPoint.position;
    }

    /// <summary>
    /// Refreshes target memory from an external event such as taking damage,
    /// without forcing the target to count as currently visible.
    /// </summary>
    public void RegisterTargetKnowledge(Vector3 knownTargetPosition)
    {
        hadVisualContact = true;
        lastSeenTime = Time.time;
        LastSeenPosition = knownTargetPosition;

        // Being hit should refresh memory, but not fake direct line of sight.
        IsTargetVisible = false;
    }

    /// <summary>
    /// Clears runtime sight/memory state while keeping the target references.
    /// Useful when reusing pooled enemies.
    /// </summary>
    public void ResetPerception()
    {
        IsTargetVisible = false;
        hadVisualContact = false;
        lastSeenTime = float.NegativeInfinity;
        lastVisibleTime = float.NegativeInfinity;

        if (targetAimPoint != null)
            LastSeenPosition = targetAimPoint.position;
        else if (target != null)
            LastSeenPosition = target.position;
        else
            LastSeenPosition = Vector3.zero;
    }

    /// <summary>
    /// Refreshes visibility and memory.
    /// Call once per frame from the owning enemy.
    /// </summary>
    public void TickPerception(bool inCombat)
    {
        if (target == null)
        {
            IsTargetVisible = false;
            return;
        }

        bool visibleNow = ComputeVisibility(inCombat);

        if (visibleNow)
        {
            IsTargetVisible = true;
            hadVisualContact = true;
            lastSeenTime = Time.time;
            lastVisibleTime = Time.time;
            LastSeenPosition = GetTargetAimPosition();
            return;
        }

        // Small grace window reduces noisy LOS flicker near edges.
        IsTargetVisible = Time.time <= lastVisibleTime + lostSightGraceTime;
    }

    private bool ComputeVisibility(bool inCombat)
    {
        Vector3 origin = GetEyePosition();
        Vector3 targetPosition = GetTargetAimPosition();
        Vector3 toTarget = targetPosition - origin;
        float distance = toTarget.magnitude;

        if (distance > sightRange)
            return false;

        Vector3 direction = toTarget.normalized;
        float allowedAngle = (inCombat ? combatViewAngle : detectionViewAngle) * 0.5f;

        if (Vector3.Angle(transform.forward, direction) > allowedAngle)
            return false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
            return IsTargetHit(hit.transform);

        return false;
    }

    private bool IsTargetHit(Transform hitTransform)
    {
        if (hitTransform == null || target == null)
            return false;

        return hitTransform == target || hitTransform.IsChildOf(target);
    }

    private Vector3 GetEyePosition()
    {
        if (eyePoint != null)
            return eyePoint.position;

        return transform.position + Vector3.up;
    }

    private Vector3 GetTargetAimPosition()
    {
        if (targetAimPoint != null)
            return targetAimPoint.position;

        if (target != null)
            return target.position;

        return Vector3.zero;
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up;

        // Eye point
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(origin, 0.12f);

        // Sight range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, sightRange);

        // Detection cone
        DrawViewCone(origin, detectionViewAngle, Color.blue);

        // Combat cone
        DrawViewCone(origin, combatViewAngle, new Color(1f, 0.5f, 0f, 1f)); // orange

        // Last known / visible target
        if (Application.isPlaying && hadVisualContact)
        {
            Gizmos.color = IsTargetVisible ? Color.green : Color.red;
            Gizmos.DrawSphere(LastSeenPosition, 0.15f);
            Gizmos.DrawLine(origin, KnownTargetPosition);
        }
    }

    private void DrawViewCone(Vector3 origin, float angle, Color color)
    {
        Gizmos.color = color;

        Vector3 left = Quaternion.Euler(0f, -angle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, angle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawLine(origin, origin + left * sightRange);
        Gizmos.DrawLine(origin, origin + right * sightRange);
    }
}