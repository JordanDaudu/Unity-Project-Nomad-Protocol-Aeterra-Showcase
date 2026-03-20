using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio service for the project.
///
/// Responsibilities:
/// - Own the music system and resolve the highest-priority active music request.
/// - Play 2D/3D/UI/ambient one-shot sounds through pooled emitters.
/// - Control mixer volumes and persist user audio settings.
/// - Manage an optional ambient loop source.
/// - Stay safe during shutdown so no new transition coroutines are started while the manager is being destroyed.
///
/// Design notes:
/// - Music is request-driven via <see cref="AcquireMusic"/>, with optional forced override via <see cref="ForcePlayMusic"/>.
/// - Music transitions use a fade-out-to-silence, then fade-in-new-track flow. Tracks do not overlap.
/// - One-shot SFX are played through pooled <see cref="PooledAudioEmitter"/> instances.
/// - This class is intended to be persistent via <see cref="DontDestroyOnLoad(UnityEngine.Object)"/>.
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>Global singleton instance.</summary>
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [Tooltip("Main audio mixer used by the game.")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Mixer Groups")]
    [Tooltip("Mixer group used for all music playback.")]
    [SerializeField] private AudioMixerGroup musicGroup;

    [Tooltip("Mixer group used for non-spatial 2D sound effects.")]
    [SerializeField] private AudioMixerGroup sfx2DGroup;

    [Tooltip("Mixer group used for spatial 3D sound effects.")]
    [SerializeField] private AudioMixerGroup sfx3DGroup;

    [Tooltip("Mixer group used for UI sounds.")]
    [SerializeField] private AudioMixerGroup uiGroup;

    [Tooltip("Mixer group used for ambient loops and ambient one-shots.")]
    [SerializeField] private AudioMixerGroup ambientGroup;

    [Header("Exposed Mixer Parameters")]
    [Tooltip("Exposed mixer parameter name for the master volume.")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Tooltip("Exposed mixer parameter name for the music volume.")]
    [SerializeField] private string musicVolumeParameter = "MusicVolume";

    [Tooltip("Exposed mixer parameter name for the 2D SFX volume.")]
    [SerializeField] private string sfx2DVolumeParameter = "Sfx2DVolume";

    [Tooltip("Exposed mixer parameter name for the 3D SFX volume.")]
    [SerializeField] private string sfx3DVolumeParameter = "Sfx3DVolume";

    [Tooltip("Exposed mixer parameter name for the UI volume.")]
    [SerializeField] private string uiVolumeParameter = "UIVolume";

    [Tooltip("Exposed mixer parameter name for the ambient volume.")]
    [SerializeField] private string ambientVolumeParameter = "AmbientVolume";

    [Header("Music Sources")]
    [Tooltip("First dedicated music AudioSource.")]
    [SerializeField] private AudioSource musicSourceA;

    [Tooltip("Second dedicated music AudioSource.")]
    [SerializeField] private AudioSource musicSourceB;

    [Header("Ambient Loop Source")]
    [Tooltip("Dedicated AudioSource used for a looping ambient bed.")]
    [SerializeField] private AudioSource ambientLoopSource;

    [Header("Music Library")]
    [Tooltip("All music tracks that can be requested by MusicTrackId.")]
    [SerializeField] private List<MusicTrackData> musicTracks = new();

    [Header("Music Defaults")]
    [Tooltip("Track used when there are no active music requests.")]
    [SerializeField] private MusicTrackId defaultExplorationTrack = MusicTrackId.Exploration_Default;

    [Tooltip("If enabled, all configured music clips will request audio data loading on Awake.")]
    [SerializeField] private bool preloadMusicOnAwake = true;

    [Header("Emitter Pool")]
    [Tooltip("Optional prefab used for pooled one-shot emitters. If left empty, emitters are created at runtime.")]
    [SerializeField] private PooledAudioEmitter emitterPrefab;

    [Tooltip("Optional hierarchy parent used to keep pooled emitters organized in the scene.")]
    [SerializeField] private Transform emitterPoolParent;

    [Tooltip("Number of pooled emitters created on startup.")]
    [SerializeField] private int initialEmitterPoolSize = 12;

    [Header("Default Volumes")]
    [Tooltip("Default normalized master volume.")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    [Tooltip("Default normalized music volume.")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

    [Tooltip("Default normalized 2D SFX volume.")]
    [SerializeField, Range(0f, 1f)] private float sfx2DVolume = 1f;

    [Tooltip("Default normalized 3D SFX volume.")]
    [SerializeField, Range(0f, 1f)] private float sfx3DVolume = 1f;

    [Tooltip("Default normalized UI volume.")]
    [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

    [Tooltip("Default normalized ambient volume.")]
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 1f;

    [Header("Persistence")]
    [Tooltip("If enabled, audio settings are loaded from and saved to PlayerPrefs.")]
    [SerializeField] private bool loadAndSaveSettings = true;

    [Header("Debug")]
    [Tooltip("If enabled, the audio system starts muted for quick testing.")]
    [SerializeField] private bool startMutedForTesting = false;

    [Tooltip("Debug key used to toggle mute at runtime.")]
    [SerializeField] private KeyCode toggleMuteKey = KeyCode.M;

    private const string PrefMaster = "Audio.Master";
    private const string PrefMusic = "Audio.Music";
    private const string PrefSfx2D = "Audio.Sfx2D";
    private const string PrefSfx3D = "Audio.Sfx3D";
    private const string PrefUI = "Audio.UI";
    private const string PrefAmbient = "Audio.Ambient";
    private const string PrefMuted = "Audio.Muted";

    private bool isMuted;
    private bool isShuttingDown;

    private readonly Dictionary<MusicTrackId, MusicTrackData> musicLookup = new();
    private readonly Dictionary<int, MusicRequest> activeMusicRequests = new();
    private readonly Queue<PooledAudioEmitter> availableEmitters = new();

    private int nextRequestId = 1;
    private int requestSequence = 0;

    private MusicTrackId currentResolvedTrackId = MusicTrackId.None;
    private MusicTrackId forcedTrackId = MusicTrackId.None;

    private Coroutine musicTransitionRoutine;
    private Coroutine ambientRoutine;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;

    /// <summary>
    /// Internal active music request record.
    /// </summary>
    private struct MusicRequest
    {
        public int id;
        public int sequence;
        public MusicTrackId trackId;
        public MusicPriority priority;
        public UnityEngine.Object owner;
    }

    /// <summary>
    /// Disposable handle returned by <see cref="AcquireMusic"/>.
    /// Releasing the handle removes that request from the active music set.
    /// </summary>
    public sealed class MusicHandle : IDisposable
    {
        private AudioManager manager;
        private int requestId;

        internal MusicHandle(AudioManager manager, int requestId)
        {
            this.manager = manager;
            this.requestId = requestId;
        }

        /// <summary>Returns true while this handle still points to a valid active request.</summary>
        public bool IsValid => manager != null && requestId != 0;

        /// <summary>Releases the associated music request.</summary>
        public void Release()
        {
            if (!IsValid)
                return;

            manager.ReleaseMusicInternal(requestId);
            manager = null;
            requestId = 0;
        }

        /// <inheritdoc />
        public void Dispose() => Release();
    }

    /// <summary>
    /// Initializes the singleton, mixer routing, track lookup, emitter pool, and persisted settings.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;

        BuildMusicLookup();
        AssignMixerGroups();
        CreateInitialEmitterPool();

        if (preloadMusicOnAwake)
            PreloadMusicClips();

        if (loadAndSaveSettings)
            LoadSettings();

        if (startMutedForTesting)
            isMuted = true;

        ApplyAllMixerVolumes();
    }

    /// <summary>
    /// Starts default music resolution after initialization is complete.
    /// </summary>
    private void Start()
    {
        ResolveMusic();
    }

    /// <summary>
    /// Handles debug input for mute toggling.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(toggleMuteKey))
            SetMuted(!isMuted);
    }

    /// <summary>
    /// Marks the manager as shutting down so no new transitions are started during application quit.
    /// </summary>
    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    /// <summary>
    /// Marks the manager as shutting down so no new transitions are started during destroy.
    /// </summary>
    private void OnDestroy()
    {
        isShuttingDown = true;
    }

    #region Public Music API

    /// <summary>
    /// Gets the currently resolved music track id.
    /// </summary>
    public MusicTrackId GetCurrentTrackId() => currentResolvedTrackId;

    /// <summary>
    /// Acquires a music request for the given track.
    /// The highest-priority active request wins. If priorities match, the newer request wins.
    /// </summary>
    /// <param name="trackId">Track to request.</param>
    /// <param name="owner">Optional owner used for debugging/reference.</param>
    /// <returns>A disposable handle that must be released when this music request should end.</returns>
    public MusicHandle AcquireMusic(MusicTrackId trackId, UnityEngine.Object owner = null)
    {
        if (!TryGetTrack(trackId, out MusicTrackData track))
        {
            Debug.LogWarning($"AudioManager: Track '{trackId}' was requested but is not configured.");
            return null;
        }

        if (track.clip == null)
        {
            Debug.LogWarning($"AudioManager: Track '{trackId}' has no clip assigned.");
            return null;
        }

        int requestId = nextRequestId++;

        activeMusicRequests[requestId] = new MusicRequest
        {
            id = requestId,
            sequence = ++requestSequence,
            trackId = trackId,
            priority = track.priority,
            owner = owner
        };

        ResolveMusic();
        return new MusicHandle(this, requestId);
    }

    /// <summary>
    /// Forces a specific track to play regardless of normal active request resolution.
    /// </summary>
    public void ForcePlayMusic(MusicTrackId trackId)
    {
        forcedTrackId = trackId;
        ResolveMusic();
    }

    /// <summary>
    /// Clears the forced music override and returns to normal request-based resolution.
    /// </summary>
    public void ClearForcedMusic()
    {
        forcedTrackId = MusicTrackId.None;
        ResolveMusic();
    }

    /// <summary>
    /// Immediately stops both music sources and clears the currently resolved track.
    /// </summary>
    public void StopMusicImmediately()
    {
        if (musicTransitionRoutine != null)
        {
            StopCoroutine(musicTransitionRoutine);
            musicTransitionRoutine = null;
        }

        if (musicSourceA != null)
        {
            musicSourceA.Stop();
            musicSourceA.volume = 0f;
        }

        if (musicSourceB != null)
        {
            musicSourceB.Stop();
            musicSourceB.volume = 0f;
        }

        currentResolvedTrackId = MusicTrackId.None;
    }

    #endregion

    #region Public SFX API

    /// <summary>
    /// Plays a non-spatial cue using the emitter pool.
    /// </summary>
    public void PlayCue2D(AudioCue cue)
    {
        if (cue == null)
            return;

        PlayEmitterCue(cue, Vector3.zero, null);
    }

    /// <summary>
    /// Plays a spatial cue at the given position, optionally following a transform while it plays.
    /// </summary>
    public void PlayCue3D(AudioCue cue, Vector3 position, Transform followTarget = null)
    {
        if (cue == null)
            return;

        PlayEmitterCue(cue, position, followTarget);
    }

    /// <summary>
    /// Plays a UI cue through the emitter pool.
    /// </summary>
    public void PlayUICue(AudioCue cue)
    {
        if (cue == null)
            return;

        PlayEmitterCue(cue, Vector3.zero, null);
    }

    /// <summary>
    /// Plays an ambient one-shot cue.
    /// </summary>
    public void PlayAmbientOneShot(AudioCue cue, Vector3 position)
    {
        if (cue == null)
            return;

        PlayEmitterCue(cue, position, null);
    }

    /// <summary>
    /// Starts or replaces the ambient loop with a fade-out then fade-in transition.
    /// </summary>
    public void PlayAmbientLoop(AudioCue cue, float fadeDuration = 1f)
    {
        if (cue == null)
            return;

        if (ambientLoopSource == null)
            return;

        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
            return;

        if (!CanRunCoroutines())
            return;

        if (ambientRoutine != null)
            StopCoroutine(ambientRoutine);

        ambientRoutine = StartCoroutine(FadeAmbientLoopTo(clip, cue.GetRandomVolume(), cue.GetRandomPitch(), fadeDuration));
    }

    /// <summary>
    /// Stops the ambient loop with a fade-out.
    /// </summary>
    public void StopAmbientLoop(float fadeDuration = 1f)
    {
        if (ambientLoopSource == null)
            return;

        if (!CanRunCoroutines())
            return;

        if (ambientRoutine != null)
            StopCoroutine(ambientRoutine);

        ambientRoutine = StartCoroutine(FadeOutAmbientLoop(fadeDuration));
    }

    #endregion

    #region Volume API

    /// <summary>
    /// Sets the mute state and reapplies mixer volumes.
    /// </summary>
    public void SetMuted(bool muted)
    {
        isMuted = muted;
        ApplyAllMixerVolumes();

        if (loadAndSaveSettings)
            SaveSettings();
    }

    /// <summary>
    /// Returns whether the audio system is currently muted.
    /// </summary>
    public bool IsMuted() => isMuted;

    /// <summary>
    /// Gets the current normalized volume for the specified channel.
    /// </summary>
    public float GetVolume(AudioVolumeChannel channel)
    {
        return channel switch
        {
            AudioVolumeChannel.Master => masterVolume,
            AudioVolumeChannel.Music => musicVolume,
            AudioVolumeChannel.Sfx2D => sfx2DVolume,
            AudioVolumeChannel.Sfx3D => sfx3DVolume,
            AudioVolumeChannel.UI => uiVolume,
            AudioVolumeChannel.Ambient => ambientVolume,
            _ => 1f
        };
    }

    /// <summary>
    /// Sets the normalized volume for the specified channel and persists it if enabled.
    /// </summary>
    public void SetVolume(AudioVolumeChannel channel, float normalizedVolume)
    {
        normalizedVolume = Mathf.Clamp01(normalizedVolume);

        switch (channel)
        {
            case AudioVolumeChannel.Master: masterVolume = normalizedVolume; break;
            case AudioVolumeChannel.Music: musicVolume = normalizedVolume; break;
            case AudioVolumeChannel.Sfx2D: sfx2DVolume = normalizedVolume; break;
            case AudioVolumeChannel.Sfx3D: sfx3DVolume = normalizedVolume; break;
            case AudioVolumeChannel.UI: uiVolume = normalizedVolume; break;
            case AudioVolumeChannel.Ambient: ambientVolume = normalizedVolume; break;
        }

        ApplyAllMixerVolumes();

        if (loadAndSaveSettings)
            SaveSettings();
    }

    #endregion

    #region Internal Music Logic

    /// <summary>
    /// Removes an internal music request and resolves the desired track again.
    /// Safe during shutdown.
    /// </summary>
    private void ReleaseMusicInternal(int requestId)
    {
        if (!activeMusicRequests.Remove(requestId))
            return;

        if (isShuttingDown)
            return;

        ResolveMusic();
    }

    /// <summary>
    /// Resolves the desired music track from forced override or active requests.
    /// </summary>
    private void ResolveMusic()
    {
        if (isShuttingDown)
            return;

        MusicTrackId desiredTrackId = GetDesiredTrackId();

        if (desiredTrackId == currentResolvedTrackId)
            return;

        if (!TryGetTrack(desiredTrackId, out MusicTrackData track))
        {
            Debug.LogWarning($"AudioManager: Desired track '{desiredTrackId}' is missing from the music library.");
            return;
        }

        TransitionToMusic(desiredTrackId, track);
    }

    /// <summary>
    /// Selects the winning track id based on forced override, then active requests, then default exploration.
    /// </summary>
    private MusicTrackId GetDesiredTrackId()
    {
        if (forcedTrackId != MusicTrackId.None)
            return forcedTrackId;

        bool foundAny = false;
        MusicRequest best = default;

        foreach (MusicRequest request in activeMusicRequests.Values)
        {
            if (!foundAny)
            {
                best = request;
                foundAny = true;
                continue;
            }

            bool isHigherPriority = request.priority > best.priority;
            bool samePriorityButNewer = request.priority == best.priority && request.sequence > best.sequence;

            if (isHigherPriority || samePriorityButNewer)
                best = request;
        }

        return foundAny ? best.trackId : defaultExplorationTrack;
    }

    /// <summary>
    /// Starts a music transition to the given track.
    /// This manager uses a fade-out-to-silence, then fade-in-new-track flow.
    /// Tracks do not overlap.
    /// </summary>
    private void TransitionToMusic(MusicTrackId trackId, MusicTrackData track)
    {
        if (track == null || track.clip == null)
            return;

        if (IsAnyMusicSourcePlayingClip(track.clip))
        {
            currentResolvedTrackId = trackId;
            return;
        }

        if (musicTransitionRoutine != null)
        {
            StopCoroutine(musicTransitionRoutine);
            musicTransitionRoutine = null;
        }

        if (!CanRunCoroutines())
            return;

        musicTransitionRoutine = StartCoroutine(FadeOutThenFadeInMusic(trackId, track));
    }

    /// <summary>
    /// Fades out any currently playing music sources to silence, stops them, then starts and fades in the new track.
    /// There is intentionally no overlap between tracks.
    /// </summary>
    private IEnumerator FadeOutThenFadeInMusic(MusicTrackId trackId, MusicTrackData track)
    {
        if (track.clip.loadState != AudioDataLoadState.Loaded)
            track.clip.LoadAudioData();

        float waitTime = 0f;
        while (track.clip.loadState == AudioDataLoadState.Loading && waitTime < 2f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        float duration = Mathf.Max(0.01f, track.fadeDuration);

        float sourceAVolume = musicSourceA != null ? musicSourceA.volume : 0f;
        float sourceBVolume = musicSourceB != null ? musicSourceB.volume : 0f;

        bool sourceAPlaying = musicSourceA != null && musicSourceA.isPlaying;
        bool sourceBPlaying = musicSourceB != null && musicSourceB.isPlaying;

        if (sourceAPlaying || sourceBPlaying)
        {
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                if (musicSourceA != null && sourceAPlaying)
                    musicSourceA.volume = Mathf.Lerp(sourceAVolume, 0f, t);

                if (musicSourceB != null && sourceBPlaying)
                    musicSourceB.volume = Mathf.Lerp(sourceBVolume, 0f, t);

                yield return null;
            }
        }

        if (musicSourceA != null)
        {
            musicSourceA.Stop();
            musicSourceA.volume = 0f;
        }

        if (musicSourceB != null)
        {
            musicSourceB.Stop();
            musicSourceB.volume = 0f;
        }

        AudioSource targetSource = inactiveMusicSource != null ? inactiveMusicSource : activeMusicSource;

        if (targetSource == null)
        {
            musicTransitionRoutine = null;
            yield break;
        }

        targetSource.outputAudioMixerGroup = musicGroup;
        targetSource.clip = track.clip;
        targetSource.loop = track.loop;
        targetSource.volume = 0f;
        targetSource.Play();

        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                targetSource.volume = Mathf.Lerp(0f, track.targetVolume, t);
                yield return null;
            }
        }

        targetSource.volume = track.targetVolume;

        if (targetSource == inactiveMusicSource)
            SwapMusicSources();
        else
            activeMusicSource = targetSource;

        currentResolvedTrackId = trackId;
        musicTransitionRoutine = null;
    }

    /// <summary>
    /// Swaps the active and inactive dedicated music sources.
    /// </summary>
    private void SwapMusicSources()
    {
        AudioSource temp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = temp;
    }

    /// <summary>
    /// Returns true if either dedicated music source is already playing the given clip.
    /// </summary>
    private bool IsAnyMusicSourcePlayingClip(AudioClip clip)
    {
        if (clip == null)
            return false;

        bool sourceAMatches = musicSourceA != null && musicSourceA.isPlaying && musicSourceA.clip == clip;
        bool sourceBMatches = musicSourceB != null && musicSourceB.isPlaying && musicSourceB.clip == clip;

        return sourceAMatches || sourceBMatches;
    }

    #endregion

    #region Internal SFX Logic

    /// <summary>
    /// Plays a cue using a pooled emitter.
    /// </summary>
    private void PlayEmitterCue(AudioCue cue, Vector3 position, Transform followTarget)
    {
        PooledAudioEmitter emitter = GetEmitter();
        emitter.gameObject.SetActive(true);
        emitter.Play(cue, GetMixerGroupForBus(cue.bus), position, followTarget, ReturnEmitterToPool);
    }

    /// <summary>
    /// Gets an available emitter from the pool, creating one if needed.
    /// </summary>
    private PooledAudioEmitter GetEmitter()
    {
        while (availableEmitters.Count > 0)
        {
            PooledAudioEmitter emitter = availableEmitters.Dequeue();
            if (emitter != null)
                return emitter;
        }

        return CreateEmitter();
    }

    /// <summary>
    /// Returns an emitter to the available pool queue.
    /// </summary>
    private void ReturnEmitterToPool(PooledAudioEmitter emitter)
    {
        if (emitter == null)
            return;

        availableEmitters.Enqueue(emitter);
    }

    /// <summary>
    /// Creates the initial emitter pool.
    /// </summary>
    private void CreateInitialEmitterPool()
    {
        for (int i = 0; i < initialEmitterPoolSize; i++)
        {
            PooledAudioEmitter emitter = CreateEmitter();
            emitter.gameObject.SetActive(false);
            availableEmitters.Enqueue(emitter);
        }
    }

    /// <summary>
    /// Creates a pooled emitter from the configured prefab or from a runtime fallback object.
    /// The fallback path adds the AudioSource first so PooledAudioEmitter.Awake can safely find it.
    /// </summary>
    private PooledAudioEmitter CreateEmitter()
    {
        if (emitterPrefab != null)
            return Instantiate(emitterPrefab, emitterPoolParent);

        GameObject emitterObject = new("AudioEmitter");
        if (emitterPoolParent != null)
            emitterObject.transform.SetParent(emitterPoolParent);

        AudioSource source = emitterObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.dopplerLevel = 0f;

        PooledAudioEmitter emitter = emitterObject.AddComponent<PooledAudioEmitter>();
        return emitter;
    }

    /// <summary>
    /// Gets the mixer group corresponding to the given bus.
    /// </summary>
    private AudioMixerGroup GetMixerGroupForBus(AudioBus bus)
    {
        return bus switch
        {
            AudioBus.Sfx2D => sfx2DGroup,
            AudioBus.Sfx3D => sfx3DGroup,
            AudioBus.UI => uiGroup,
            AudioBus.Ambient => ambientGroup,
            _ => sfx3DGroup
        };
    }

    #endregion

    #region Ambient Loop Logic

    /// <summary>
    /// Fades out the current ambient loop if needed, then starts and fades in the new loop.
    /// </summary>
    private IEnumerator FadeAmbientLoopTo(AudioClip clip, float targetVolume, float pitch, float fadeDuration)
    {
        if (ambientLoopSource == null)
            yield break;

        float duration = Mathf.Max(0.01f, fadeDuration);

        if (ambientLoopSource.isPlaying)
        {
            float startVolume = ambientLoopSource.volume;
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                ambientLoopSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            ambientLoopSource.Stop();
        }

        ambientLoopSource.outputAudioMixerGroup = ambientGroup;
        ambientLoopSource.clip = clip;
        ambientLoopSource.loop = true;
        ambientLoopSource.spatialBlend = 0f;
        ambientLoopSource.pitch = pitch;
        ambientLoopSource.volume = 0f;
        ambientLoopSource.Play();

        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                ambientLoopSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }
        }

        ambientLoopSource.volume = targetVolume;
        ambientRoutine = null;
    }

    /// <summary>
    /// Fades out and stops the ambient loop source.
    /// </summary>
    private IEnumerator FadeOutAmbientLoop(float fadeDuration)
    {
        if (ambientLoopSource == null || !ambientLoopSource.isPlaying)
        {
            ambientRoutine = null;
            yield break;
        }

        float duration = Mathf.Max(0.01f, fadeDuration);
        float startVolume = ambientLoopSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            ambientLoopSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        ambientLoopSource.Stop();
        ambientLoopSource.volume = 0f;
        ambientRoutine = null;
    }

    #endregion

    #region Mixer Helpers

    /// <summary>
    /// Assigns dedicated source mixer groups at startup.
    /// </summary>
    private void AssignMixerGroups()
    {
        if (musicSourceA != null)
            musicSourceA.outputAudioMixerGroup = musicGroup;

        if (musicSourceB != null)
            musicSourceB.outputAudioMixerGroup = musicGroup;

        if (ambientLoopSource != null)
            ambientLoopSource.outputAudioMixerGroup = ambientGroup;
    }

    /// <summary>
    /// Applies all stored channel volumes to the mixer.
    /// </summary>
    private void ApplyAllMixerVolumes()
    {
        SetMixerVolume(masterVolumeParameter, isMuted ? 0f : masterVolume);
        SetMixerVolume(musicVolumeParameter, musicVolume);
        SetMixerVolume(sfx2DVolumeParameter, sfx2DVolume);
        SetMixerVolume(sfx3DVolumeParameter, sfx3DVolume);
        SetMixerVolume(uiVolumeParameter, uiVolume);
        SetMixerVolume(ambientVolumeParameter, ambientVolume);
    }

    /// <summary>
    /// Converts a normalized 0-1 volume into decibels and writes it to the mixer.
    /// </summary>
    private void SetMixerVolume(string parameterName, float normalizedVolume)
    {
        if (mainMixer == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        float volume = Mathf.Clamp01(normalizedVolume);
        float dB = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
        mainMixer.SetFloat(parameterName, dB);
    }

    /// <summary>
    /// Returns true when this manager can safely start coroutines.
    /// </summary>
    private bool CanRunCoroutines()
    {
        return !isShuttingDown && isActiveAndEnabled;
    }

    #endregion

    #region Settings Persistence

    /// <summary>
    /// Saves current audio settings to PlayerPrefs.
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(PrefMaster, masterVolume);
        PlayerPrefs.SetFloat(PrefMusic, musicVolume);
        PlayerPrefs.SetFloat(PrefSfx2D, sfx2DVolume);
        PlayerPrefs.SetFloat(PrefSfx3D, sfx3DVolume);
        PlayerPrefs.SetFloat(PrefUI, uiVolume);
        PlayerPrefs.SetFloat(PrefAmbient, ambientVolume);
        PlayerPrefs.SetInt(PrefMuted, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads saved audio settings from PlayerPrefs.
    /// </summary>
    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(PrefMaster, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(PrefMusic, musicVolume);
        sfx2DVolume = PlayerPrefs.GetFloat(PrefSfx2D, sfx2DVolume);
        sfx3DVolume = PlayerPrefs.GetFloat(PrefSfx3D, sfx3DVolume);
        uiVolume = PlayerPrefs.GetFloat(PrefUI, uiVolume);
        ambientVolume = PlayerPrefs.GetFloat(PrefAmbient, ambientVolume);
        isMuted = PlayerPrefs.GetInt(PrefMuted, 0) == 1;
    }

    #endregion

    #region Music Track Setup

    /// <summary>
    /// Rebuilds the music track lookup dictionary from the serialized library list.
    /// </summary>
    private void BuildMusicLookup()
    {
        musicLookup.Clear();

        foreach (MusicTrackData track in musicTracks)
        {
            if (track == null)
                continue;

            if (musicLookup.ContainsKey(track.trackId))
            {
                Debug.LogWarning($"AudioManager: Duplicate track id '{track.trackId}'.");
                continue;
            }

            musicLookup.Add(track.trackId, track);
        }
    }

    /// <summary>
    /// Attempts to get music track data for the given id.
    /// </summary>
    private bool TryGetTrack(MusicTrackId trackId, out MusicTrackData track)
    {
        return musicLookup.TryGetValue(trackId, out track);
    }

    /// <summary>
    /// Requests audio data loading for all configured music clips.
    /// </summary>
    private void PreloadMusicClips()
    {
        foreach (MusicTrackData track in musicTracks)
        {
            if (track == null || track.clip == null)
                continue;

            track.clip.LoadAudioData();
        }
    }

    #endregion
}