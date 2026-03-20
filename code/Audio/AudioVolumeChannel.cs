/// <summary>
/// High-level volume channels exposed to UI/settings.
/// </summary>
/// <remarks>
/// These map to exposed parameters on the project's AudioMixer.
///
/// Key connections:
/// - <see cref="AudioManager.GetVolume"/> and <see cref="AudioManager.SetVolume"/> use this enum.
/// - Your settings menu should write normalized 0..1 values, which the manager converts to dB.
/// </remarks>
public enum AudioVolumeChannel
{
    Master,
    Music,
    Sfx2D,
    Sfx3D,
    UI,
    Ambient
}