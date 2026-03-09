using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Global camera utility responsible for small runtime camera adjustments.
/// 
/// Current scope:
/// - Holds a single <see cref="CinemachineCamera"/> instance
/// - Smoothly lerps the Cinemachine Position Composer's CameraDistance toward a requested target
/// 
/// Notes:
/// - Implemented as a simple scene-persistent singleton because multiple systems may request
///   camera distance changes (e.g., weapon equip changes desired distance).
/// - This does not own camera input/rotation logic; it only changes distance.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    #region Inspector

    [Header("Camera Distance Settings")]
    [SerializeField] private bool canChangeCameraDistance;
    [SerializeField] private float distanceChangeRate = 0.5f;

    #endregion

    #region Runtime

    private CinemachineCamera cinemachineCamera;
    private CinemachinePositionComposer positionComposer;

    // Target value requested by gameplay systems (e.g., weapon changes).
    private float targetCameraDistance;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        // Basic singleton pattern to guarantee a single global camera manager.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the camera across scenes
        }
        else
        {
            Debug.LogWarning("Multiple instances of CameraManager detected! Destroying duplicate.");
            Destroy(gameObject); // Ensure only one instance exists
        }

        // Cinemachine is expected as a child (camera rig under this manager).
        cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
        positionComposer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();
    }

    private void Update()
    {
        UpdateCameraDistance();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Requests a camera distance change. The distance will smoothly interpolate over time.
    /// </summary>
    public void ChangeCameraDistance(float distance) => targetCameraDistance = distance;

    #endregion

    #region Internal Logic
    private void UpdateCameraDistance()
    {
        if (canChangeCameraDistance == false)
            return;

        float currentCameraDistance = positionComposer.CameraDistance;
        float cameraTreshold = 0.1f;

        // Small threshold avoids micro-jitter when we are effectively "at" target.
        if (Mathf.Abs(targetCameraDistance - currentCameraDistance) < cameraTreshold) return;

        positionComposer.CameraDistance =
            Mathf.Lerp(currentCameraDistance, targetCameraDistance, Time.deltaTime * distanceChangeRate);

    }

    #endregion
}
