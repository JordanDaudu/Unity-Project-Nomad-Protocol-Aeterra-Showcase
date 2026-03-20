using System.Collections;
using UnityEngine;

/// <summary>
/// Dive-roll ability implementation.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Lock standard locomotion via <see cref="PlayerMovement.SetMovementLocked"/>.</description></item>
/// <item><description>Compute roll direction (facing or movement-based).</description></item>
/// <item><description>Trigger the roll animation (Animator trigger).</description></item>
/// <item><description>Move the CharacterController manually for a fixed duration using an <see cref="AnimationCurve"/> speed profile.</description></item>
/// <item><description>Ensure rollback-safe cleanup on disable.</description></item>
/// </list>
///
/// Design notes:
/// - Uses CharacterController.Move rather than Rigidbody for consistency with the main locomotion system.
/// - Uses a speed curve so the roll feels snappy at start and eases out naturally.
/// - Locks movement during roll to prevent input fighting (WASD vs forced roll motion).
///
/// Key connections:
/// - Reads direction helpers from <see cref="PlayerMovement"/>.
/// - Respects movement lock state to avoid double-lock issues.
/// </remarks>
public class PlayerRollAbility : PlayerAbility
{
    #region Inspector

    [Header("Roll Settings")]
    [SerializeField] private float rollSpeed = 8f;

    [Tooltip("Total roll time (seconds). Should match the effective animation length you want.")]
    [SerializeField] private float rollDuration = 0.88f;

    [Tooltip("If true, roll uses move input direction (WASD). If false, uses facing direction.")]
    [SerializeField] private bool useMoveInputDirection = false;

    [Tooltip("Animator trigger name used to play the roll animation.")]
    [SerializeField] private string animatorTriggerName = "DiveRoll";

    [SerializeField]
    private AnimationCurve rollSpeedCurve = new AnimationCurve(
        new Keyframe(0.00f, 0.60f),
        new Keyframe(0.08f, 0.88f),
        new Keyframe(0.16f, 1.00f),
        new Keyframe(0.32f, 0.92f),
        new Keyframe(0.52f, 0.68f),
        new Keyframe(0.72f, 0.28f),
        new Keyframe(0.88f, 0.06f),
        new Keyframe(1.00f, 0.00f)
    );

    #endregion

    #region Components

    private CharacterController controller;
    private PlayerMovement movement;
    private Animator animator;

    #endregion

    #region Runtime

    private Coroutine rollRoutine;

    #endregion

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();

        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        // Pool/disable safety: if the object is disabled mid-roll, stop cleanly and unlock movement.
        ForceStopRoll();
    }

    #endregion

    #region Activation

    public override bool CanActivate()
    {
        if (!base.CanActivate())
            return false;

        // Hard dependency checks so we don't null-ref during development.
        if (controller == null || movement == null || animator == null)
            return false;

        // Avoid activating roll if something else already locked movement.
        return !movement.IsMovementLocked;
    }

    public override void Activate()
    {
        if (!CanActivate())
            return;

        if (rollRoutine != null)
            StopCoroutine(rollRoutine);

        rollRoutine = StartCoroutine(RollRoutine());
    }

    #endregion

    #region Roll Routine

    private IEnumerator RollRoutine()
    {
        isActive = true;
        lastUseTime = Time.time;

        // Lock locomotion so ApplyMovement/ApplyRotation don't fight the roll motion.
        movement.SetMovementLocked(true);

        // Direction policy:
        // - Facing direction preserves "aim-based" rolling.
        // - Move input direction enables WASD-based rolling.
        Vector3 rollDirection = useMoveInputDirection
            ? movement.GetMoveDirectionOrFacing()
            : movement.GetFacingDirection();

        animator.SetTrigger(animatorTriggerName);

        float elapsed = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / rollDuration);

            // Curve output is a multiplier for rollSpeed (0..1-ish).
            float speedMultiplier = rollSpeedCurve.Evaluate(normalizedTime);

            controller.Move(rollDirection * rollSpeed * speedMultiplier * Time.deltaTime);

            yield return null;
        }

        EndRoll();
    }

    #endregion

    #region Cleanup

    private void ForceStopRoll()
    {
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        EndRoll();
    }

    private void EndRoll()
    {
        if (movement != null)
            movement.SetMovementLocked(false);

        isActive = false;
        rollRoutine = null;
    }

    #endregion
}