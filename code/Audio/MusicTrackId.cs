/// <summary>
/// Stable identifiers for music tracks.
/// </summary>
/// <remarks>
/// This enum is the "address" used by gameplay systems to request music without holding clip references.
/// Actual clips/priority/transition settings live in <see cref="MusicTrackData"/> entries configured on
/// <see cref="AudioManager"/> (or legacy <see cref="SoundManager"/>).
/// </remarks>
public enum MusicTrackId
{
    None = 0,

    Exploration_Default = 1,

    Combat_Default = 2,
    Combat_Melee = 3,
    Combat_Ranged = 4,
    Combat_Boss = 5,

    Victory_Default = 6,
    GameOver_Default = 7
}