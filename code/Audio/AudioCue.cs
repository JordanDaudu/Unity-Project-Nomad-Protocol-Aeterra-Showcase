using UnityEngine;

/// <summary>
/// Data-driven definition of a sound that can be played by the audio system.
/// </summary>
/// <remarks>
/// An <see cref="AudioCue"/> is a ScriptableObject that describes:
/// - which bus the sound routes through (<see cref="AudioBus"/>)
/// - one or more possible clips (randomized per playback)
/// - playback configuration (looping, spatial blend, 3D min/max distance)
/// - simple variation ranges (volume and pitch)
///
/// Key connections:
/// - <see cref="AudioManager"/> uses cues to configure pooled emitters (<see cref="PooledAudioEmitter"/>).
/// - Gameplay scripts should reference cues (not clips) so audio can be tuned without code edits.
/// </remarks>
[CreateAssetMenu(fileName = "AudioCue", menuName = "Audio/Audio Cue")]
public class AudioCue : ScriptableObject
{
    #region Inspector

    [Header("Routing")]
    public AudioBus bus = AudioBus.Sfx3D;

    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Playback")]
    public bool loop = false;

    [Tooltip("0 = fully 2D, 1 = fully 3D (ignored for non-spatial buses).")]
    [Range(0f, 1f)] public float spatialBlend = 1f;

    public float minDistance = 1f;
    public float maxDistance = 25f;

    [Header("Variation")]
    public Vector2 volumeRange = new(1f, 1f);
    public Vector2 pitchRange = new(1f, 1f);

    #endregion

    #region Clip Selection

    /// <summary>
    /// Returns a random clip from <see cref="clips"/>.
    /// </summary>
    /// <returns>The selected clip, or null if no clips are assigned.</returns>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
            return clips[0];

        int index = Random.Range(0, clips.Length);
        return clips[index];
    }

    #endregion

    #region Variation

    /// <summary>
    /// Returns a randomized volume value within <see cref="volumeRange"/>.
    /// </summary>
    public float GetRandomVolume()
    {
        float min = Mathf.Min(volumeRange.x, volumeRange.y);
        float max = Mathf.Max(volumeRange.x, volumeRange.y);
        return Random.Range(min, max);
    }

    /// <summary>
    /// Returns a randomized pitch value within <see cref="pitchRange"/>.
    /// </summary>
    public float GetRandomPitch()
    {
        float min = Mathf.Min(pitchRange.x, pitchRange.y);
        float max = Mathf.Max(pitchRange.x, pitchRange.y);
        return Random.Range(min, max);
    }

    #endregion

    #region Spatial Rules

    /// <summary>
    /// Returns the spatial blend that should be used for this cue,
    /// taking the bus into account.
    /// </summary>
    /// <remarks>
    /// Certain buses are treated as non-spatial regardless of the authoring value:
    /// - 2D SFX
    /// - UI
    /// - Ambient
    /// </remarks>
    public float GetResolvedSpatialBlend()
    {
        return bus switch
        {
            AudioBus.Sfx2D => 0f,
            AudioBus.UI => 0f,
            AudioBus.Ambient => 0f,
            _ => spatialBlend
        };
    }

    #endregion
}