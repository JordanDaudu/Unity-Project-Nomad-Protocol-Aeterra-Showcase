using System;
using UnityEngine;

/// <summary>
/// Handles player locomotion using <see cref="CharacterController"/>.
/// 
/// Responsibilities:
/// - Read move input via <see cref="InputSystem_Actions"/>
/// - Apply movement and gravity through CharacterController.Move
/// - Rotate the player toward the aim point supplied by <see cref="PlayerAim"/>
/// - Drive animator parameters for blend tree / locomotion state
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Components

    private Player player;
    private CharacterController characterController;
    private Animator animator;

    #endregion

    #region Input

    private InputSystem_Actions controls;

    // Public read-only access for other systems (e.g., camera aim logic).
    public Vector2 moveInput { get; private set; }

    #endregion

    #region Inspector

    [Header("Movement info")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float turnSpeed;

    #endregion

    #region Runtime

    private float speed;
    private float verticalVelocity;
    private Vector3 movementDirection;
    private bool isRunning;

    #endregion

    #region Unity Callbacks

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GetComponent<Player>();

        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        speed = walkSpeed;

        AssignInputEvents();
    }

    // Update is called once per frame
    void Update()
    {
        ApplyMovement();
        ApplyGravity();
        ApplyRotation();
        AnimatorControllers();
    }

    #endregion

    #region Movement

    private void ApplyRotation()
    {
        // Rotate toward the current aim point (mouse hit point).
        Vector3 lookingDirection = player.aim.GetMouseHitInfo().point - transform.position;
        lookingDirection.y = 0f;
        lookingDirection.Normalize();

        Quaternion desiredRotation = Quaternion.LookRotation(lookingDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
    }

    private void ApplyMovement()
    {
        // Convert 2D input into a world-space direction on the XZ plane.
        movementDirection = new Vector3(moveInput.x, 0, moveInput.y);

        if (movementDirection.magnitude > 0)
        {
            characterController.Move(movementDirection * Time.deltaTime * speed);
        }
    }

    private void ApplyGravity()
    {
        // CharacterController is not driven by Rigidbody gravity, so we apply it manually.
        if (characterController.isGrounded == false)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            movementDirection.y = verticalVelocity;
        }
        else
        {
            verticalVelocity = -.5f;
        }

        Vector3 gravityVector = new Vector3(0, verticalVelocity * Time.deltaTime, 0);
        characterController.Move(gravityVector);
    }

    #endregion

    #region Animation

    private void AnimatorControllers()
    {
        // Project movement direction onto local axes to drive strafing/forward blend parameters.
        float xVelocity = Vector3.Dot(movementDirection.normalized, transform.right);
        float zVelocity = Vector3.Dot(movementDirection.normalized, transform.forward);

        animator.SetFloat("xVelocity", xVelocity, .1f, Time.deltaTime);
        animator.SetFloat("zVelocity", zVelocity, .1f, Time.deltaTime);

        bool playRunAnimation = isRunning && movementDirection.magnitude > 0;
        animator.SetBool("isRunning", playRunAnimation);
    }

    #endregion

    #region Input Wiring

    private void AssignInputEvents()
    {
        controls = player.controls;

        controls.Player.Move.performed += context => moveInput = context.ReadValue<Vector2>();
        controls.Player.Move.canceled += context => moveInput = Vector2.zero;

        controls.Player.Sprint.performed += context =>
        {
            speed = runSpeed;
            isRunning = true;
        };
        controls.Player.Sprint.canceled += context =>
        {
            speed = walkSpeed;
            isRunning = false;
        };
    }

    #endregion
}
