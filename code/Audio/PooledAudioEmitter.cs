using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Reusable pooled sound emitter used by <see cref="AudioManager"/> to play one-shot or looping audio cues.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Own a single <see cref="AudioSource"/> instance.</description></item>
/// <item><description>Configure that AudioSource from an <see cref="AudioCue"/> at playback time.</description></item>
/// <item><description>Optionally follow a target transform while the sound is playing.</description></item>
/// <item><description>Detect when non-looping playback finishes and recycle itself back to the pool.</description></item>
/// <item><description>Notify the owning pool callback when it is ready to be reused.</description></item>
/// </list>
///
/// Design notes:
/// - This component is intended to be created and managed by <see cref="AudioManager"/>.
/// - Looping sounds do not automatically recycle and must be stopped manually through <see cref="StopAndRecycle"/>.
/// - The same emitter class is used for 2D, 3D, UI, and ambient playback. The cue decides routing and spatial behavior.
/// </remarks>
[RequireComponent(typeof(AudioSource))]
public class PooledAudioEmitter : MonoBehaviour
{
    #region Components

    private AudioSource audioSource;

    #endregion

    #region Runtime

    private Transform followTarget;
    private Action<PooledAudioEmitter> onFinished;
    private Coroutine lifetimeRoutine;

    /// <summary>
    /// Returns true while this emitter is actively assigned to a cue and has not yet been recycled.
    /// </summary>
    public bool IsBusy { get; private set; }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Caches the required <see cref="AudioSource"/> and applies safe default startup behavior.
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Updates the emitter position while it is configured to follow a target transform.
    /// </summary>
    private void Update()
    {
        if (followTarget != null)
            transform.position = followTarget.position;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Configures and starts playback of an <see cref="AudioCue"/>.
    /// </summary>
    /// <param name="cue">Cue that defines which clip to play and how it should be configured.</param>
    /// <param name="mixerGroup">Mixer group the internal AudioSource should route into.</param>
    /// <param name="position">World position to use when no follow target is provided.</param>
    /// <param name="follow">Optional transform to follow while the sound is playing.</param>
    /// <param name="finishedCallback">Callback invoked when the emitter is finished and ready to return to the pool.</param>
    public void Play(AudioCue cue, AudioMixerGroup mixerGroup, Vector3 position, Transform follow, Action<PooledAudioEmitter> finishedCallback)
    {
        if (cue == null)
        {
            finishedCallback?.Invoke(this);
            return;
        }

        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
        {
            finishedCallback?.Invoke(this);
            return;
        }

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        IsBusy = true;
        onFinished = finishedCallback;
        followTarget = follow;

        transform.position = follow != null ? follow.position : position;

        audioSource.outputAudioMixerGroup = mixerGroup;
        audioSource.clip = clip;
        audioSource.loop = cue.loop;
        audioSource.volume = cue.GetRandomVolume();
        audioSource.pitch = cue.GetRandomPitch();
        audioSource.spatialBlend = cue.GetResolvedSpatialBlend();
        audioSource.minDistance = cue.minDistance;
        audioSource.maxDistance = cue.maxDistance;

        audioSource.Play();

        if (!cue.loop)
            lifetimeRoutine = StartCoroutine(WaitUntilFinished());
    }

    /// <summary>
    /// Stops playback immediately, clears emitter state, disables the GameObject,
    /// and notifies the owning pool that this emitter can be reused.
    /// </summary>
    public void StopAndRecycle()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        audioSource.Stop();
        audioSource.clip = null;

        followTarget = null;
        IsBusy = false;

        // Copy the callback to avoid re-entrancy issues if the callback immediately reuses this emitter.
        Action<PooledAudioEmitter> finishedCallback = onFinished;
        onFinished = null;

        gameObject.SetActive(false);
        finishedCallback?.Invoke(this);
    }

    #endregion

    #region Internal

    /// <summary>
    /// Waits until the internal AudioSource finishes playback, then recycles the emitter.
    /// Only used for non-looping cues.
    /// </summary>
    private IEnumerator WaitUntilFinished()
    {
        while (audioSource != null && audioSource.isPlaying)
            yield return null;

        lifetimeRoutine = null;
        StopAndRecycle();
    }

    #endregion
}