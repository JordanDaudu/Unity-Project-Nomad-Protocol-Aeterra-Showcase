using UnityEngine;

/// <summary>
/// Marker/component for corruption crystal visuals attached to an enemy.
/// </summary>
/// <remarks>
/// Currently this is an empty behaviour used as a semantic tag in the hierarchy.
/// It gives you a clean place to add crystal-specific behavior later (glow pulses,
/// break-off on death, VFX hooks, etc.) without changing prefab structure.
///
/// Key connection:
/// - <see cref="EnemyVisuals"/> randomly enables/disables crystal objects to vary enemy appearance.
/// </remarks>
public class EnemyCorruptionCrystal : MonoBehaviour
{

}
