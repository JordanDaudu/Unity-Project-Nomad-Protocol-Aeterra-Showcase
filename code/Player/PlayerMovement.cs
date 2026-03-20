using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player locomotion using <see cref="CharacterController"/>.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Read movement + sprint input from the new Input System.</description></item>
/// <item><description>Apply XZ movement through <see cref="CharacterController.Move"/>.</description></item>
/// <item><description>Apply manual gravity (CharacterController does not use Rigidbody gravity).</description></item>
/// <item><description>Rotate the player toward the mouse aim point (top-down shooter style).</description></item>
/// <item><description>Drive locomotion animator parameters ("xVelocity", "zVelocity", "isRunning").</description></item>
/// <item><description>Expose helper APIs for movement-locking abilities such as roll.</description></item>
/// </list>
///
/// Key connections:
/// - Reads aim point from <see cref="PlayerAim.GetMouseHitInfo"/>.
/// - Roll/abilities interact with this class via <see cref="SetMovementLocked"/> and direction helpers.
/// - Animation parameters are expected by the player's Animator Controller.
/// </remarks>
[RequireComponent(typeof(CharacterController))]

public class PlayerMovement : MonoBehaviour
{
    #region Components

    private Player player;
    private CharacterController characterController;
    private Animator animator;

    #endregion

    #region Input

    private InputSystem_Actions controls;

    public Vector2 moveInput { get; private set; }

    #endregion

    #region Inspector

    [Header("Movement Info")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;

    [Tooltip("Gravity acceleration applied to CharacterController when not grounded.")]
    [SerializeField] private float gravity = 9.81f;

    [Tooltip("Slerp speed for turning toward mouse aim point.")]
    [SerializeField] private float turnSpeed = 12f;

    #endregion

    #region Runtime

    private float currentSpeed;
    private float verticalVelocity;

    /// <summary>
    /// The current movement direction being applied on the XZ plane.
    /// </summary>
    private Vector3 movementDirection;

    private bool isRunning;
    private bool isMovementLocked;

    /// <summary>
    /// True when movement is locked by an ability (e.g., roll) and standard locomotion should not run.
    /// </summary>
    public bool IsMovementLocked => isMovementLocked;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        player = GetComponent<Player>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        currentSpeed = walkSpeed;

        AssignInputEvents();
    }

    private void OnDestroy()
    {
        // Important for safety when domain reload is disabled or objects are destroyed mid-session.
        UnassignInputEvents();
    }

    private void Update()
    {
        if (!isMovementLocked)
        {
            ApplyMovement();
            ApplyRotation();
        }
        else
        {
            // When locked, ensure we don't keep "ghost" direction for animation or drift.
            movementDirection = Vector3.zero;
        }

        ApplyGravity();
        UpdateAnimator();
    }

    #endregion

    #region Movement

    /// <summary>
    /// Rotates the player to face the mouse aim point on the XZ plane.
    /// </summary>
    /// <remarks>
    /// We ignore Y (height) to prevent tilting.
    /// Uses a small magnitude threshold to avoid NaNs/jitter when aim point == player position.
    /// </remarks>
    private void ApplyRotation()
    {
        Vector3 lookDirection = player.aim.GetMouseHitInfo().point - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.0001f)
            return;

        lookDirection.Normalize();

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Applies horizontal movement based on <see cref="moveInput"/> and <see cref="currentSpeed"/>.
    /// </summary>
    /// <remarks>
    /// Movement direction is clamped to magnitude 1 so diagonals are not faster.
    /// </remarks>
    private void ApplyMovement()
    {
        movementDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        movementDirection = Vector3.ClampMagnitude(movementDirection, 1f);

        if (movementDirection.sqrMagnitude > 0.0001f)
            characterController.Move(movementDirection * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Applies gravity through the CharacterController.
    /// </summary>
    /// <remarks>
    /// CharacterController doesn't apply physics gravity automatically.
    /// We maintain our own vertical velocity and feed it through Move().
    /// </remarks>
    private void ApplyGravity()
    {
        if (!characterController.isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0f)
        {
            // Small negative value helps keep the controller grounded (prevents floaty slope behavior).
            verticalVelocity = -0.5f;
        }

        Vector3 gravityVector = new Vector3(0f, verticalVelocity * Time.deltaTime, 0f);
        characterController.Move(gravityVector);
    }

    #endregion

    #region Ability Support

    /// <summary>
    /// Locks/unlocks standard locomotion so abilities (roll, stun, cutscenes) can take over movement.
    /// </summary>
    /// <remarks>
    /// When locked, we zero movementDirection so animations and other systems don't read stale data.
    /// </remarks>
    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;

        if (locked)
            movementDirection = Vector3.zero;
    }

    /// <summary>
    /// Returns the current facing direction on the XZ plane.
    /// </summary>
    /// <remarks>
    /// This preserves your current forward-only roll behavior (roll always follows facing).
    /// If forward is invalid (should not happen), defaults to world forward.
    /// </remarks>
    public Vector3 GetFacingDirection()
    {
        Vector3 facingDirection = transform.forward;
        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return facingDirection.normalized;
    }

    /// <summary>
    /// Returns current move-input direction if there is one,
    /// otherwise falls back to facing direction.
    /// </summary>
    /// <remarks>
    /// Useful if you later want movement-based rolling (WASD direction) instead of facing-based rolling.
    /// </remarks>
    public Vector3 GetMoveDirectionOrFacing()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (inputDirection.sqrMagnitude > 0.0001f)
            return inputDirection.normalized;

        return GetFacingDirection();
    }

    #endregion

    #region Animation

    /// <summary>
    /// Updates locomotion animator parameters.
    /// </summary>
    /// <remarks>
    /// Expected Animator params:
    /// - "xVelocity": strafe blend value
    /// - "zVelocity": forward blend value
    /// - "isRunning": run state gate
    ///
    /// The dot products convert world movement direction into local-space "intent".
    /// </remarks>
    private void UpdateAnimator()
    {
        if (isMovementLocked)
        {
            animator.SetFloat("xVelocity", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("zVelocity", 0f, 0.1f, Time.deltaTime);
            animator.SetBool("isRunning", false);
            return;
        }

        Vector3 normalizedMove = movementDirection.sqrMagnitude > 0.0001f
            ? movementDirection.normalized
            : Vector3.zero;

        float xVelocity = Vector3.Dot(normalizedMove, transform.right);
        float zVelocity = Vector3.Dot(normalizedMove, transform.forward);

        animator.SetFloat("xVelocity", xVelocity, 0.1f, Time.deltaTime);
        animator.SetFloat("zVelocity", zVelocity, 0.1f, Time.deltaTime);

        bool shouldPlayRun = isRunning && movementDirection.sqrMagnitude > 0.0001f;
        animator.SetBool("isRunning", shouldPlayRun);
    }

    #endregion

    #region Input Wiring

    /// <summary>
    /// Subscribes to movement-related input events.
    /// </summary>
    /// <remarks>
    /// We subscribe once and unsubscribe in <see cref="OnDestroy"/> to prevent duplicate subscriptions
    /// if this component is recreated or if Domain Reload is disabled.
    /// </remarks>
    private void AssignInputEvents()
    {
        controls = player.controls;

        controls.Player.Move.performed += OnMovePerformed;
        controls.Player.Move.canceled += OnMoveCanceled;

        controls.Player.Sprint.performed += OnSprintPerformed;
        controls.Player.Sprint.canceled += OnSprintCanceled;
    }

    private void UnassignInputEvents()
    {
        if (controls == null)
            return;

        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;

        controls.Player.Sprint.performed -= OnSprintPerformed;
        controls.Player.Sprint.canceled -= OnSprintCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext context) => moveInput = Vector2.zero;

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        currentSpeed = runSpeed;
        isRunning = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        currentSpeed = walkSpeed;
        isRunning = false;
    }

    #endregion
}