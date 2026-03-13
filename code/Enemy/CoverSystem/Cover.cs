using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a world object that can provide tactical cover.
/// 
/// A cover object owns one or more <see cref="CoverPoint"/> children.
/// Enemies do not reserve the cover object itself; they reserve one of its points.
/// </summary>
[DisallowMultipleComponent]
public class Cover : MonoBehaviour
{
    [Header("Generation")]

    [Tooltip("Prefab used when generating cover points automatically.")]
    [SerializeField] private GameObject coverPointPrefab;

    [Tooltip("If enabled, cover points will be generated automatically in Awake when none are already present.")]
    [SerializeField] private bool autoGeneratePoints = true;

    [Tooltip("Horizontal offset used for left/right generated cover points.")]
    [SerializeField] private float xOffset = 1f;

    [Tooltip("Vertical offset used for all generated cover points.")]
    [SerializeField] private float yOffset = 0.2f;

    [Tooltip("Depth offset used for front/back generated cover points.")]
    [SerializeField] private float zOffset = 1f;

    [Header("Runtime")]

    [Tooltip("Cached list of cover points belonging to this cover.")]
    [SerializeField] private List<CoverPoint> coverPoints = new List<CoverPoint>();

    /// <summary>
    /// Read-only access to the points that belong to this cover.
    /// </summary>
    public IReadOnlyList<CoverPoint> CoverPoints => coverPoints;

    private void Awake()
    {
        InitializeCoverPoints();
    }

    /// <summary>
    /// Initializes this cover's point list.
    /// 
    /// First it tries to reuse any existing child <see cref="CoverPoint"/> components.
    /// If none are found and auto-generation is enabled, it generates default points.
    /// Finally, it ensures all points know this object is their owner.
    /// </summary>
    private void InitializeCoverPoints()
    {
        coverPoints.Clear();
        coverPoints.AddRange(GetComponentsInChildren<CoverPoint>(true));

        if (coverPoints.Count == 0 && autoGeneratePoints)
            GenerateCoverPoints();

        AssignOwnerToPoints();
    }

    /// <summary>
    /// Ensures every point knows that this cover is its owner.
    /// </summary>
    private void AssignOwnerToPoints()
    {
        foreach (CoverPoint point in coverPoints)
        {
            if (point == null)
                continue;

            point.SetOwner(this);
        }
    }

    /// <summary>
    /// Generates four default cover points around this cover:
    /// front, back, right, and left.
    /// 
    /// These are local-space offsets, so they rotate correctly with the cover object.
    /// </summary>
    private void GenerateCoverPoints()
    {
        if (coverPointPrefab == null)
        {
            Debug.LogWarning($"[{name}] Missing Cover Point Prefab.", this);
            return;
        }

        CoverPointDefinition[] definitions =
        {
            new CoverPointDefinition(CoverPointSide.Front, new Vector3(0f, yOffset,  zOffset), Vector3.forward),
            new CoverPointDefinition(CoverPointSide.Back,  new Vector3(0f, yOffset, -zOffset), Vector3.back),
            new CoverPointDefinition(CoverPointSide.Right, new Vector3( xOffset, yOffset, 0f), Vector3.right),
            new CoverPointDefinition(CoverPointSide.Left,  new Vector3(-xOffset, yOffset, 0f), Vector3.left)
        };

        foreach (CoverPointDefinition definition in definitions)
        {
            GameObject pointObject = Instantiate(coverPointPrefab, transform);
            pointObject.transform.localPosition = definition.localPosition;
            pointObject.transform.localRotation = Quaternion.identity;

            CoverPoint coverPoint = pointObject.GetComponent<CoverPoint>();

            if (coverPoint == null)
            {
                Debug.LogWarning($"[{name}] Cover point prefab does not contain a CoverPoint component.", this);
                Destroy(pointObject);
                continue;
            }

            coverPoint.SetOwner(this);
            coverPoint.ConfigureGeneratedData(definition.side, definition.localDirection);
            coverPoints.Add(coverPoint);
        }
    }

    /// <summary>
    /// Simple internal struct used to define generated point data cleanly.
    /// </summary>
    private struct CoverPointDefinition
    {
        public CoverPointSide side;
        public Vector3 localPosition;
        public Vector3 localDirection;

        public CoverPointDefinition(CoverPointSide side, Vector3 localPosition, Vector3 localDirection)
        {
            this.side = side;
            this.localPosition = localPosition;
            this.localDirection = localDirection;
        }
    }
}