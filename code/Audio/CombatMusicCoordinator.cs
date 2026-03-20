using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks enemy combat participation and translates it into music requests.
///
/// Responsibilities:
/// - Listen to enemy combat lifecycle events such as battle enter, battle exit, and death.
/// - Acquire and release combat music requests through <see cref="AudioManager"/>.
/// - Keep combat music alive for a short grace period after the last enemy leaves combat.
/// - Optionally auto-register active enemies in the scene, including pooled enemies that become active later.
///
/// Design notes:
/// - Enemies do not choose music directly. They only report combat state.
/// - This coordinator decides which combat track should be requested based on enemy type.
/// - If both BattleModeExited and Died fire for the same enemy, only the first handled removal counts.
/// - The cooldown request prevents combat music from ending too abruptly when combat briefly drops.
///
/// Pooling note:
/// - This script works with pooled enemies, but explicit RegisterEnemy / UnregisterEnemy calls are the most robust option.
/// - The auto-refresh scan is a convenient fallback for automatic registration.
/// </summary>
public class CombatMusicCoordinator : MonoBehaviour
{
    [Header("Tracks")]
    [Tooltip("Fallback combat track used when no more specific per-enemy-type combat track is configured.")]
    [SerializeField] private MusicTrackId defaultCombatTrack = MusicTrackId.Combat_Default;

    [Tooltip("Combat track used for melee enemies. Falls back to Default Combat Track if set to None.")]
    [SerializeField] private MusicTrackId meleeCombatTrack = MusicTrackId.Combat_Melee;

    [Tooltip("Combat track used for ranged enemies. Falls back to Default Combat Track if set to None.")]
    [SerializeField] private MusicTrackId rangedCombatTrack = MusicTrackId.Combat_Ranged;

    [Tooltip("Combat track used for boss enemies. Falls back to Default Combat Track if set to None.")]
    [SerializeField] private MusicTrackId bossCombatTrack = MusicTrackId.Combat_Boss;

    [Header("Timing")]
    [Tooltip("How long combat music should be held after the last tracked enemy leaves combat or dies.")]
    [SerializeField] private float exitCombatDelay = 4f;

    [Header("Registration")]
    [Tooltip("If enabled, the coordinator periodically scans the scene for Enemy instances and auto-registers them.")]
    [SerializeField] private bool autoRegisterExistingEnemies = true;

    [Tooltip("How often, in seconds, to rescan for enemies when auto registration is enabled.")]
    [SerializeField] private float refreshInterval = 1f;

    private readonly HashSet<Enemy> registeredEnemies = new();
    private readonly Dictionary<Enemy, AudioManager.MusicHandle> activeEnemyHandles = new();

    private AudioManager.MusicHandle cooldownHandle;
    private float cooldownEndTime = -1f;
    private float nextRefreshTime = -1f;

    private MusicTrackId cooldownTrackId = MusicTrackId.None;

    /// <summary>
    /// Performs an initial registration scan if automatic registration is enabled.
    /// </summary>
    private void Start()
    {
        if (autoRegisterExistingEnemies)
            RefreshEnemyRegistration();
    }

    /// <summary>
    /// Updates automatic enemy registration and manages the combat music cooldown timer.
    /// </summary>
    private void Update()
    {
        if (autoRegisterExistingEnemies && Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + refreshInterval;
            RefreshEnemyRegistration();
        }

        if (cooldownHandle != null &&
            activeEnemyHandles.Count == 0 &&
            Time.unscaledTime >= cooldownEndTime)
        {
            cooldownHandle.Release();
            cooldownHandle = null;
            cooldownTrackId = MusicTrackId.None;
            cooldownEndTime = -1f;
        }
    }

    /// <summary>
    /// Registers an enemy with the coordinator and subscribes to its combat lifecycle events.
    /// If the enemy is already in battle mode when registered, a combat music request is acquired immediately.
    /// </summary>
    /// <param name="enemy">Enemy to register.</param>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || registeredEnemies.Contains(enemy))
            return;

        registeredEnemies.Add(enemy);

        enemy.BattleModeEntered += OnEnemyEnteredBattle;
        enemy.BattleModeExited += OnEnemyExitedBattle;
        enemy.Died += OnEnemyDied;

        if (enemy.inBattleMode)
            OnEnemyEnteredBattle(enemy);
    }

    /// <summary>
    /// Unregisters an enemy from the coordinator, unsubscribes from its events,
    /// and releases any active combat music handle associated with it.
    /// </summary>
    /// <param name="enemy">Enemy to unregister.</param>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null || !registeredEnemies.Contains(enemy))
            return;

        enemy.BattleModeEntered -= OnEnemyEnteredBattle;
        enemy.BattleModeExited -= OnEnemyExitedBattle;
        enemy.Died -= OnEnemyDied;

        registeredEnemies.Remove(enemy);

        bool removedHandle = ReleaseEnemyHandle(enemy);

        if (removedHandle && activeEnemyHandles.Count == 0)
            StartCooldownIfNeeded(ResolveTrackForEnemy(enemy));
    }

    /// <summary>
    /// Handles an enemy entering battle mode by cancelling any pending cooldown
    /// and acquiring the appropriate combat music request for that enemy type.
    /// </summary>
    /// <param name="enemy">Enemy that entered battle.</param>
    private void OnEnemyEnteredBattle(Enemy enemy)
    {
        CancelCooldown();

        if (activeEnemyHandles.ContainsKey(enemy))
            return;

        if (AudioManager.Instance == null)
            return;

        MusicTrackId track = ResolveTrackForEnemy(enemy);
        AudioManager.MusicHandle handle = AudioManager.Instance.AcquireMusic(track, enemy);

        if (handle != null)
            activeEnemyHandles.Add(enemy, handle);
    }

    /// <summary>
    /// Handles an enemy exiting battle mode.
    /// </summary>
    /// <param name="enemy">Enemy that exited battle.</param>
    private void OnEnemyExitedBattle(Enemy enemy)
    {
        HandleEnemyCombatEnded(enemy);
    }

    /// <summary>
    /// Handles an enemy death.
    /// </summary>
    /// <param name="enemy">Enemy that died.</param>
    private void OnEnemyDied(Enemy enemy)
    {
        HandleEnemyCombatEnded(enemy);
    }

    /// <summary>
    /// Processes the end of combat participation for a specific enemy.
    /// This safely ignores duplicate end events, such as Died firing after BattleModeExited.
    /// </summary>
    /// <param name="enemy">Enemy whose combat participation ended.</param>
    private void HandleEnemyCombatEnded(Enemy enemy)
    {
        bool removedHandle = ReleaseEnemyHandle(enemy);

        if (!removedHandle)
            return;

        if (activeEnemyHandles.Count == 0)
            StartCooldownIfNeeded(ResolveTrackForEnemy(enemy));
    }

    /// <summary>
    /// Resolves which combat track should be used for the given enemy type.
    /// Per-type tracks fall back to the default combat track when set to None.
    /// </summary>
    /// <param name="enemy">Enemy to inspect.</param>
    /// <returns>The music track that should represent this enemy in combat.</returns>
    private MusicTrackId ResolveTrackForEnemy(Enemy enemy)
    {
        if (enemy is EnemyBoss)
            return bossCombatTrack != MusicTrackId.None ? bossCombatTrack : defaultCombatTrack;

        if (enemy is EnemyRange)
            return rangedCombatTrack != MusicTrackId.None ? rangedCombatTrack : defaultCombatTrack;

        if (enemy is EnemyMelee)
            return meleeCombatTrack != MusicTrackId.None ? meleeCombatTrack : defaultCombatTrack;

        return defaultCombatTrack;
    }

    /// <summary>
    /// Releases the music handle currently associated with the given enemy.
    /// </summary>
    /// <param name="enemy">Enemy whose handle should be released.</param>
    /// <returns>True if a handle existed and was released; otherwise false.</returns>
    private bool ReleaseEnemyHandle(Enemy enemy)
    {
        if (enemy == null)
            return false;

        if (!activeEnemyHandles.TryGetValue(enemy, out AudioManager.MusicHandle handle))
            return false;

        handle?.Release();
        activeEnemyHandles.Remove(enemy);
        return true;
    }

    /// <summary>
    /// Starts or extends the post-combat cooldown hold.
    /// During this window, combat music remains active instead of dropping immediately to exploration.
    /// </summary>
    /// <param name="trackToHold">Combat track to keep alive during cooldown.</param>
    private void StartCooldownIfNeeded(MusicTrackId trackToHold)
    {
        if (exitCombatDelay <= 0f || AudioManager.Instance == null)
            return;

        if (trackToHold == MusicTrackId.None)
            trackToHold = defaultCombatTrack;

        if (cooldownHandle != null)
        {
            cooldownEndTime = Time.unscaledTime + exitCombatDelay;
            return;
        }

        cooldownTrackId = trackToHold;
        cooldownHandle = AudioManager.Instance.AcquireMusic(cooldownTrackId, this);
        cooldownEndTime = Time.unscaledTime + exitCombatDelay;
    }

    /// <summary>
    /// Cancels the active cooldown hold, if any.
    /// </summary>
    private void CancelCooldown()
    {
        cooldownHandle?.Release();
        cooldownHandle = null;
        cooldownTrackId = MusicTrackId.None;
        cooldownEndTime = -1f;
    }

    /// <summary>
    /// Refreshes scene enemy registration when automatic registration is enabled.
    /// New active enemies are registered, and missing or inactive enemies are unregistered.
    /// </summary>
    private void RefreshEnemyRegistration()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
            RegisterEnemy(enemy);

        List<Enemy> toRemove = null;

        foreach (Enemy enemy in registeredEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                toRemove ??= new List<Enemy>();
                toRemove.Add(enemy);
            }
        }

        if (toRemove != null)
        {
            foreach (Enemy enemy in toRemove)
            {
                if (enemy != null)
                    UnregisterEnemy(enemy);
            }
        }
    }

    /// <summary>
    /// Releases active music handles when possible and unsubscribes from enemy events during destruction.
    /// If the AudioManager is already inactive or shutting down, handle references are cleared without forcing music resolution.
    /// </summary>
    private void OnDestroy()
    {
        bool canReleaseThroughAudioManager =
            AudioManager.Instance != null &&
            AudioManager.Instance.isActiveAndEnabled;

        if (canReleaseThroughAudioManager)
            CancelCooldown();
        else
            cooldownHandle = null;

        if (canReleaseThroughAudioManager)
        {
            foreach (KeyValuePair<Enemy, AudioManager.MusicHandle> pair in activeEnemyHandles)
                pair.Value?.Release();
        }

        activeEnemyHandles.Clear();
        cooldownTrackId = MusicTrackId.None;
        cooldownEndTime = -1f;

        foreach (Enemy enemy in registeredEnemies)
        {
            if (enemy == null)
                continue;

            enemy.BattleModeEntered -= OnEnemyEnteredBattle;
            enemy.BattleModeExited -= OnEnemyExitedBattle;
            enemy.Died -= OnEnemyDied;
        }

        registeredEnemies.Clear();
    }
}