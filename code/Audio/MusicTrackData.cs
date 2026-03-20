using System;
using UnityEngine;

[Serializable]
/// <summary>
/// Serialized configuration for a single music track.
/// </summary>
/// <remarks>
/// This is pure data (clip + transition settings) referenced by:
/// - <see cref="AudioManager"/> to resolve and transition music.
/// - Legacy <see cref="SoundManager"/> for simple music switching.
///
/// Tip:
/// - Keep <see cref="trackId"/> stable; it acts as an external API for gameplay code.
/// </remarks>
public class MusicTrackData
{
    [Tooltip("Unique identifier used to request this music track.")]
    public MusicTrackId trackId;

    [Tooltip("Audio clip played for this track.")]
    public AudioClip clip;

    [Tooltip("How long it takes to crossfade to this track.")]
    [Min(0f)]
    public float fadeDuration = 1.5f;

    [Tooltip("Priority used to decide whether this track may override other active requests.")]
    public MusicPriority priority = MusicPriority.Exploration;

    [Tooltip("Target volume for this track.")]
    [Range(0f, 1f)]
    public float targetVolume = 1f;

    [Tooltip("Whether this track should loop.")]
    public bool loop = true;
}