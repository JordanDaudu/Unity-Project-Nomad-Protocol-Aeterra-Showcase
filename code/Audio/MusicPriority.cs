/// <summary>
/// Priority used when resolving which music track should currently be playing.
/// </summary>
/// <remarks>
/// Higher values win.
///
/// This is used by:
/// - <see cref="MusicTrackData.priority"/>
/// - <see cref="AudioManager"/> (request resolution)
/// - Legacy <see cref="SoundManager"/> (simple priority gate)
/// </remarks>
public enum MusicPriority
{
    Exploration = 0,
    Combat = 1,
    Boss = 2,
    Victory = 3,
    Critical = 4
}