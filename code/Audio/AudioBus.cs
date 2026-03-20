/// <summary>
/// Logical routing category for a sound.
/// </summary>
/// <remarks>
/// This is used by <see cref="AudioCue"/> to express intent ("this is UI" / "this is 3D") without
/// hard-coding mixer groups in gameplay code.
///
/// The routing decision is resolved by <see cref="AudioManager"/>)
/// which maps each bus to an <see cref="UnityEngine.Audio.AudioMixerGroup"/>.
/// </remarks>
public enum AudioBus
{
    /// <summary>Non-spatial sound effects (spatialBlend forced to 0).</summary>
    Sfx2D,

    /// <summary>Spatial 3D sound effects.</summary>
    Sfx3D,

    /// <summary>UI sounds (non-spatial).</summary>
    UI,

    /// <summary>Ambient sounds/loops (typically non-spatial).</summary>
    Ambient
}