using System;
using UnityEngine;

/// <summary>
/// Manages aiming logic and aim-related visuals (laser) for the player.
/// 
/// Responsibilities:
/// - Track mouse/pointer input via the new Input System (Look action)
/// - Raycast into the world to determine aim point (<see cref="GetMouseHitInfo"/>)
/// - Update a world-space aim Transform (used by weapons, animation, etc.)
/// - Update laser visuals based on weapon state and obstruction
/// - Drive camera target position for a "look ahead" effect
/// 
/// Notes:
/// - This script assumes Look input provides screen position (not delta). Ensure your Input Action is configured accordingly.
/// - There are temporary debug toggles (P/L keys) using the old input system.
/// </summary>
public class PlayerAim : MonoBehaviour
{
    #region Components

    private Player player;
    private InputSystem_Actions controls;

    #endregion

    #region Inspector

    [Header("Aim visuals - laser")]
    [SerializeField] private LineRenderer aimLaser;

    [Header("Aim control")]
    [SerializeField] private Transform aim;

    [SerializeField] private bool isAimingPrecisely;
    [SerializeField] private bool isLockingToTarget;

    [Header("Camera control")]
    [SerializeField] private Transform cameraTarget;

    [Range(0.5f, 1f)]
    [SerializeField] private float minCameraDistance = 1.5f;

    [Range(1f, 3f)]
    [SerializeField] private float maxCameraDistance = 4.0f;

    [Range(3f, 5f)]
    [SerializeField] private float cameraSensitivity = 5f;

    [Space]
    [SerializeField] private LayerMask aimLayerMask;

    #endregion

    #region Runtime

    private Vector2 mouseInput;

    // If the raycast fails this frame, we keep the last valid hit so systems don't get "null aim".
    private RaycastHit lastKnownMouseHit;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        player = GetComponent<Player>();
        AssignInputEvents();
    }

    private void Update()
    {
        // Temporary debug toggles (old input system). Remove once bound in Input Actions.
        if (Input.GetKeyDown(KeyCode.P))
            isAimingPrecisely = !isAimingPrecisely;

        if (Input.GetKeyDown(KeyCode.L))
            isLockingToTarget = !isLockingToTarget;

        UpdateAimVisuals();
        UpdateAimPosition();
        UpdateCameraPosition();
    }

    #endregion

    #region Aim Visuals

    private void UpdateAimVisuals()
    {
        // Laser is only shown when the weapon system reports it is ready.
        aimLaser.enabled = player.weapon.WeaponReady();

        if (aimLaser.enabled == false)
            return;

        WeaponModel weaponModel = player.weaponVisuals.CurrentWeaponModel();

        // Visually orient the weapon mesh and gun point toward the aim.
        weaponModel.transform.LookAt(aim);
        weaponModel.gunPoint.LookAt(aim);

        Transform gunPoint = player.weapon.GunPoint();
        Vector3 laserDirection = player.weapon.BulletDirection();

        // Laser "tip" extends a bit past the end point to look nicer in open space.
        float laserTipLength = .5f;
        float gunDistance = player.weapon.CurrentWeapon().gunDistance;

        Vector3 endPoint = gunPoint.position + laserDirection * gunDistance;

        // If we hit something before gunDistance, clamp laser to the hit and remove the tip.
        if (Physics.Raycast(gunPoint.position, laserDirection, out RaycastHit hit, gunDistance))
        {
            endPoint = hit.point;
            laserTipLength = 0;
        }

        aimLaser.SetPosition(0, gunPoint.position);
        aimLaser.SetPosition(1, endPoint);
        aimLaser.SetPosition(2, endPoint + laserDirection * laserTipLength);
    }

    #endregion

    #region Aim Point

    private void UpdateAimPosition()
    {
        Transform target = Target();

        // Optional lock-on behavior.
        if (target != null && isLockingToTarget)
        {
            if (target.GetComponent<Renderer>() != null)
                aim.position = target.GetComponent<Renderer>().bounds.center;
            else
                aim.position = target.position;


            return;
        }

        aim.position = GetMouseHitInfo().point;

        // Non-precise mode keeps aim at a fixed height to avoid aiming "into the floor".
        if (!isAimingPrecisely)
            aim.position = new Vector3(aim.position.x, transform.position.y + 1, aim.position.z);
    }

    /// <summary>
    /// Returns the current lock-on candidate under the mouse, if any.
    /// </summary>
    public Transform Target()
    {
        // Target is defined by the presence of the Target component on the current mouse hit.
        Transform target = null;
        if (GetMouseHitInfo().transform.GetComponent<Target>() != null)
        {
            target = GetMouseHitInfo().transform;
        }

        return target;
    }

    /// <summary>
    /// Exposes the world-space aim transform for other systems (weapon direction, animation, etc.).
    /// </summary>
    public Transform Aim() => aim;

    /// <summary>
    /// Whether Y-axis aiming is allowed (true) or clamped (false).
    /// </summary>
    public bool canAimPrecisely() => isAimingPrecisely;

    /// <summary>
    /// Raycasts from the camera through the current pointer position into the world.
    /// Returns the last valid hit if the raycast fails this frame.
    /// </summary>
    public RaycastHit GetMouseHitInfo()
    {
        // IMPORTANT: mouseInput must represent screen position (pixels). If using delta, this will not work correctly.
        Ray ray = Camera.main.ScreenPointToRay(mouseInput);

        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, aimLayerMask))
        {
            lastKnownMouseHit = hitInfo;
            return hitInfo;
        }
        
        return lastKnownMouseHit;
    }

    #endregion

    #region Camera
    private Vector3 DesiredCameraPosition()
    {
        // When moving backward, reduce max distance for tighter control.
        float actualMaxCameraDistance = player.movement.moveInput.y < 0.5f ? minCameraDistance : maxCameraDistance;

        Vector3 desiredCameraPosition = GetMouseHitInfo().point;
        Vector3 aimDirection = (desiredCameraPosition - transform.position).normalized;

        float distanceToDesiredPosition = Vector3.Distance(transform.position, desiredCameraPosition);
        float clampedDistance = Mathf.Clamp(distanceToDesiredPosition, minCameraDistance, actualMaxCameraDistance);


        desiredCameraPosition = transform.position + aimDirection * clampedDistance;
        desiredCameraPosition.y = transform.position.y + 1;

        return desiredCameraPosition;
    }
    private void UpdateCameraPosition()
    {
        // cameraTarget is typically followed by a Cinemachine virtual camera.
        cameraTarget.position = Vector3.Lerp(cameraTarget.position, DesiredCameraPosition(), cameraSensitivity * Time.deltaTime);
    }

    #endregion

    #region Input Wiring

    private void AssignInputEvents()
    {
        controls = player.controls;

        controls.Player.Look.performed += context => mouseInput = context.ReadValue<Vector2>();
        controls.Player.Look.canceled += context => mouseInput = Vector2.zero;
    }

    #endregion
}
