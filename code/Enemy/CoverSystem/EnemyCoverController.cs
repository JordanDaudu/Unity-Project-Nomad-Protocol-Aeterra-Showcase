using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reusable component that gives an enemy the ability to search for,
/// evaluate, reserve, and release cover points.
///
/// This logic is separated from specific enemy classes so future enemy types
/// can reuse the same cover system.
/// </summary>
[DisallowMultipleComponent]
public class EnemyCoverController : MonoBehaviour
{
    [Header("Search")]

    [Tooltip("Maximum radius used to search for nearby cover colliders.")]
    [SerializeField] private float coverSearchRadius = 30f;

    [Tooltip("How often nearby cover candidates should be refreshed.")]
    [SerializeField] private float coverRefreshInterval = 1f;

    [Tooltip("Layers that contain colliders belonging to valid cover objects.")]
    [SerializeField] private LayerMask coverLayerMask;

    [Header("Line Of Sight")]

    [Tooltip("Layers that are allowed to block line of sight between the threat and the cover point.")]
    [SerializeField] private LayerMask coverBlockingMask;

    [Tooltip("Height offset added to this enemy when checking line of sight.")]
    [SerializeField] private float selfEyeHeight = 1f;

    [Tooltip("Height offset added to the threat when checking line of sight.")]
    [SerializeField] private float threatEyeHeight = 1f;

    [Tooltip("If enabled, only cover points that are actually protected by blocking geometry are allowed.")]
    [SerializeField] private bool requireLineOfSightBlock = false;

    [Header("Scoring")]

    [Tooltip("How much distance affects the final score. Higher values prefer closer cover.")]
    [SerializeField] private float distanceWeight = 1f;

    [Tooltip("Controls how strongly the system prefers closer cover. Values below 1 make farther cover more competitive. Values above 1 strongly favor nearby cover.")]
    [SerializeField] private float distanceScoreExponent = 1f;

    [Tooltip("How much the point's safe/exposed side affects the final score.")]
    [SerializeField] private float sideSafetyWeight = 1.5f;

    [Tooltip("How much a successfully blocked line-of-sight check improves the final score.")]
    [SerializeField] private float blockedLineOfSightWeight = 2f;

    [Tooltip("Small bonus for keeping the currently reserved cover point instead of switching constantly.")]
    [SerializeField] private float keepCurrentCoverBonus = 0.25f;

    [Header("Point Filtering")]

    [Tooltip("If enabled, points that are extremely close to higher-scoring points are treated as duplicates during selection.")]
    [SerializeField] private bool filterNearDuplicatePoints = true;

    [Tooltip("Minimum world-space distance between two cover points before they are treated as separate tactical spots.")]
    [SerializeField] private float minimumCoverPointSeparation = 1f;

    [Header("Repositioning")]

    [Tooltip("How often the controller is allowed to re-check for a better cover point while already in battle. Higher values reduce jitter and constant repositioning.")]
    [SerializeField] private float repositionCheckInterval = 1.25f;

    [Tooltip("Minimum score improvement required before switching from the current cover point to a different one. Higher values make the enemy less eager to reposition.")]
    [SerializeField] private float minimumScoreImprovementToSwitch = 0.4f;

    [Tooltip("Maximum distance from the enemy to a new cover point when repositioning during battle. Prevents the enemy from abandoning a nearby fight just to run to a far cover point.")]
    [SerializeField] private float maxRepositionDistance = 10f;

    /// <summary>
    /// Runtime timestamp used to throttle how often reposition checks may happen.
    /// </summary>
    private float nextRepositionCheckTime;

    [Header("Runtime")]

    [Tooltip("Cached nearby covers found during the latest refresh.")]
    [SerializeField] private List<Cover> nearbyCovers = new List<Cover>();

    /// <summary>The currently reserved cover point, if any.</summary>
    private CoverPoint currentCoverPoint;

    /// <summary>Timestamp controlling how often nearby covers may be refreshed.</summary>
    private float nextCoverRefreshTime;

    /// <summary>The point currently reserved by this controller.</summary>
    public CoverPoint CurrentCoverPoint => currentCoverPoint;

    /// <summary>The transform of the current reserved point, or null if none exists.</summary>
    public Transform CurrentCoverTransform => currentCoverPoint != null ? currentCoverPoint.transform : null;

    /// <summary>Read-only access to the current nearby cover list.</summary>
    public IReadOnlyList<Cover> NearbyCovers => nearbyCovers;

    private void OnDisable()
    {
        ReleaseCurrentCover();
    }

    private void OnDestroy()
    {
        ReleaseCurrentCover();
    }

    /// <summary>
    /// Convenience wrapper that attempts to find and reserve the best cover,
    /// returning only the transform.
    /// </summary>
    public Transform AttemptToFindCover(Transform threat)
    {
        return TryReserveBestCover(threat, out CoverPoint reservedPoint) ? reservedPoint.transform : null;
    }

    /// <summary>
    /// Finds and reserves the best cover point against the given threat.
    /// </summary>
    public bool TryReserveBestCover(Transform threat, out CoverPoint reservedPoint)
    {
        reservedPoint = null;

        if (threat == null)
            return false;

        RefreshNearbyCovers(true);

        if (ShouldKeepCurrentCover(threat))
        {
            reservedPoint = currentCoverPoint;
            return true;
        }

        CoverPoint bestPoint = FindBestCoverPoint(threat);

        if (bestPoint == null)
            return false;

        ClaimCoverPoint(bestPoint);
        reservedPoint = currentCoverPoint;

        return reservedPoint != null;
    }

    /// <summary>
    /// Refreshes the nearby cover list by scanning colliders inside the search radius.
    /// </summary>
    public void RefreshNearbyCovers(bool forceRefresh = false)
    {
        if (!forceRefresh && Time.time < nextCoverRefreshTime)
            return;

        nextCoverRefreshTime = Time.time + coverRefreshInterval;
        nearbyCovers.Clear();

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            coverSearchRadius,
            coverLayerMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<Cover> uniqueCovers = new HashSet<Cover>();

        foreach (Collider collider in hitColliders)
        {
            Cover cover = collider.GetComponentInParent<Cover>();

            if (cover != null && uniqueCovers.Add(cover))
                nearbyCovers.Add(cover);
        }
    }

    /// <summary>
    /// Releases the currently reserved cover point, if any.
    /// </summary>
    public void ReleaseCurrentCover()
    {
        if (currentCoverPoint == null)
            return;

        currentCoverPoint.Release(this);
        currentCoverPoint = null;
    }

    /// <summary>
    /// Searches through all nearby covers and returns the best-scoring point.
    /// Near-duplicate points are filtered so the system does not treat nearly identical
    /// positions as separate tactical choices.
    /// </summary>
    protected virtual CoverPoint FindBestCoverPoint(Transform threat)
    {
        List<CoverPointCandidate> candidates = BuildScoredCandidates(threat);

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        if (filterNearDuplicatePoints)
            RemoveNearDuplicateCandidates(candidates);

        return candidates.Count > 0 ? candidates[0].point : null;
    }

    /// <summary>
    /// Periodically checks whether a meaningfully better cover point exists.
    /// </summary>
    public bool TryReserveBetterCover(Transform threat, out CoverPoint reservedPoint)
    {
        reservedPoint = null;

        if (threat == null)
            return false;

        if (Time.time < nextRepositionCheckTime)
            return false;

        nextRepositionCheckTime = Time.time + repositionCheckInterval;

        RefreshNearbyCovers();

        CoverPoint bestPoint = FindBestCoverPoint(threat);

        if (bestPoint == null)
            return false;

        // Do not reposition to cover that is too far away from the enemy's current position.
        if (!IsWithinRepositionDistance(bestPoint))
            return false;

        if (currentCoverPoint == null)
        {
            ClaimCoverPoint(bestPoint);
            reservedPoint = currentCoverPoint;
            return reservedPoint != null;
        }

        if (bestPoint == currentCoverPoint)
            return false;

        float currentScore = EvaluateCoverPoint(currentCoverPoint, threat);
        float bestScore = EvaluateCoverPoint(bestPoint, threat);

        bool currentCoverInvalid = currentScore == float.MinValue;

        if (currentCoverInvalid)
        {
            ClaimCoverPoint(bestPoint);
            reservedPoint = currentCoverPoint;
            return reservedPoint != null;
        }

        if (bestScore >= currentScore + minimumScoreImprovementToSwitch)
        {
            ClaimCoverPoint(bestPoint);
            reservedPoint = currentCoverPoint;
            return reservedPoint != null;
        }

        return false;
    }

    /// <summary>
    /// Calculates the tactical value of a single cover point.
    /// </summary>
    protected virtual float EvaluateCoverPoint(CoverPoint point, Transform threat)
    {
        if (point == null || threat == null)
            return float.MinValue;

        if (!point.CanBeUsedBy(this))
            return float.MinValue;

        // Prevent different cover objects with almost identical points from being treated
        // as separate usable tactical spots when one is already occupied by another enemy.
        if (IsTooCloseToAnotherOccupiedPoint(point))
            return float.MinValue;

        bool isProtected = IsPointProtectedFromThreat(point, threat);

        if (requireLineOfSightBlock && !isProtected)
            return float.MinValue;

        float distanceScore = EvaluateDistanceScore(point);
        float sideSafetyScore = EvaluateSideSafetyScore(point, threat);
        float protectionScore = isProtected ? 1f : 0f;
        float currentBonus = point == currentCoverPoint ? keepCurrentCoverBonus : 0f;

        return
            (distanceScore * distanceWeight) +
            (sideSafetyScore * sideSafetyWeight) +
            (protectionScore * blockedLineOfSightWeight) +
            currentBonus;
    }

    /// <summary>
    /// Returns a normalized score where closer cover points receive a higher value.
    /// The exponent allows tuning how strongly close range is preferred.
    /// </summary>
    protected virtual float EvaluateDistanceScore(CoverPoint point)
    {
        float distance = Vector3.Distance(transform.position, point.Position);
        float normalizedScore = 1f - Mathf.Clamp01(distance / Mathf.Max(coverSearchRadius, 0.01f));

        return Mathf.Pow(normalizedScore, Mathf.Max(0.01f, distanceScoreExponent));
    }

    /// <summary>
    /// Evaluates whether the point is on the protected side or exposed side of the cover
    /// relative to the threat.
    /// </summary>
    protected virtual float EvaluateSideSafetyScore(CoverPoint point, Transform threat)
    {
        if (point.Owner == null)
            return 0.5f;

        Vector3 coverToThreat = threat.position - point.Owner.transform.position;
        coverToThreat.y = 0f;

        if (coverToThreat.sqrMagnitude < 0.0001f)
            return 0.5f;

        coverToThreat.Normalize();

        float dot = Vector3.Dot(point.WorldDirectionFromCover, coverToThreat);

        // dot =  1  -> exposed side
        // dot =  0  -> neutral side
        // dot = -1  -> protected side
        return (-dot + 1f) * 0.5f;
    }

    /// <summary>
    /// Checks whether valid blocking geometry exists between the threat and the point.
    /// </summary>
    protected virtual bool IsPointProtectedFromThreat(CoverPoint point, Transform threat)
    {
        Vector3 origin = threat.position + Vector3.up * threatEyeHeight;
        Vector3 target = point.Position + Vector3.up * selfEyeHeight;

        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return false;

        return Physics.Raycast(
            origin,
            direction.normalized,
            distance,
            coverBlockingMask,
            QueryTriggerInteraction.Ignore
        );
    }

    /// <summary>
    /// Returns true if the current reserved point is still valid enough to keep.
    /// </summary>
    private bool ShouldKeepCurrentCover(Transform threat)
    {
        if (currentCoverPoint == null)
            return false;

        if (!currentCoverPoint.CanBeUsedBy(this))
            return false;

        if (requireLineOfSightBlock && !IsPointProtectedFromThreat(currentCoverPoint, threat))
            return false;

        return true;
    }

    /// <summary>
    /// Returns true if this point is too close to another point already occupied by a different enemy.
    /// This prevents multiple enemies from taking near-identical positions from overlapping covers.
    /// </summary>
    private bool IsTooCloseToAnotherOccupiedPoint(CoverPoint candidatePoint)
    {
        if (!filterNearDuplicatePoints || minimumCoverPointSeparation <= 0f)
            return false;

        float sqrMinimumDistance = minimumCoverPointSeparation * minimumCoverPointSeparation;

        foreach (Cover cover in nearbyCovers)
        {
            if (cover == null)
                continue;

            IReadOnlyList<CoverPoint> points = cover.CoverPoints;

            for (int i = 0; i < points.Count; i++)
            {
                CoverPoint otherPoint = points[i];

                if (otherPoint == null || otherPoint == candidatePoint)
                    continue;

                if (!otherPoint.IsOccupied)
                    continue;

                // Ignore points already occupied by this same controller.
                if (otherPoint.CanBeUsedBy(this))
                    continue;

                if ((candidatePoint.Position - otherPoint.Position).sqrMagnitude < sqrMinimumDistance)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a scored candidate list before duplicate filtering is applied.
    /// </summary>
    private List<CoverPointCandidate> BuildScoredCandidates(Transform threat)
    {
        List<CoverPointCandidate> candidates = new List<CoverPointCandidate>();

        foreach (Cover cover in nearbyCovers)
        {
            if (cover == null)
                continue;

            IReadOnlyList<CoverPoint> points = cover.CoverPoints;

            for (int i = 0; i < points.Count; i++)
            {
                CoverPoint point = points[i];
                float score = EvaluateCoverPoint(point, threat);

                if (score == float.MinValue)
                    continue;

                candidates.Add(new CoverPointCandidate(point, score));
            }
        }

        return candidates;
    }

    /// <summary>
    /// Removes candidates that are too close to higher-scoring candidates.
    /// This keeps the tactical option list cleaner in dense cover layouts.
    /// </summary>
    private void RemoveNearDuplicateCandidates(List<CoverPointCandidate> candidates)
    {
        if (minimumCoverPointSeparation <= 0f || candidates.Count <= 1)
            return;

        float sqrMinimumDistance = minimumCoverPointSeparation * minimumCoverPointSeparation;
        List<CoverPointCandidate> filteredCandidates = new List<CoverPointCandidate>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            CoverPointCandidate candidate = candidates[i];
            bool isNearDuplicate = false;

            for (int j = 0; j < filteredCandidates.Count; j++)
            {
                if ((candidate.point.Position - filteredCandidates[j].point.Position).sqrMagnitude < sqrMinimumDistance)
                {
                    isNearDuplicate = true;
                    break;
                }
            }

            if (!isNearDuplicate)
                filteredCandidates.Add(candidate);
        }

        candidates.Clear();
        candidates.AddRange(filteredCandidates);
    }

    /// <summary>
    /// Releases the current point and tries to reserve the given one.
    /// </summary>
    private void ClaimCoverPoint(CoverPoint point)
    {
        if (point == null)
            return;

        if (currentCoverPoint == point)
            return;

        ReleaseCurrentCover();

        if (point.Reserve(this))
            currentCoverPoint = point;
    }

    private bool IsWithinRepositionDistance(CoverPoint point)
    {
        if (point == null)
            return false;

        return Vector3.Distance(transform.position, point.Position) <= maxRepositionDistance;
    }

    /// <summary>
    /// Internal helper used when scoring and filtering candidate points.
    /// </summary>
    private struct CoverPointCandidate
    {
        public CoverPoint point;
        public float score;

        public CoverPointCandidate(CoverPoint point, float score)
        {
            this.point = point;
            this.score = score;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, coverSearchRadius);

        if (currentCoverPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentCoverPoint.Position);
            Gizmos.DrawSphere(currentCoverPoint.Position, 0.12f);
        }
    }
#endif
}