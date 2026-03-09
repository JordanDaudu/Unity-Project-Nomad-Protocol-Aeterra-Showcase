using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls ragdoll activation for an enemy.
/// 
/// Responsibilities:
/// - Collect all ragdoll colliders/rigidbodies from a configured parent transform.
/// - Toggle Rigidbody.isKinematic to enable/disable ragdoll physics.
/// - Toggle Collider.enabled for interaction phases (e.g., disable after impact window).
/// - Support excluding specific colliders/rigidbodies (weapons, sensors, etc.).
/// 
/// Key connections:
/// - Activated by death states (e.g., <see cref="DeadState_Melee"/>) when enemy dies.
/// - Reset by <see cref="Enemy.ResetEnemyForReuse"/> when respawned from a pool.
/// </summary>
public class EnemyRagdoll : MonoBehaviour
{
    [Header("Ragdoll Source")]
    [SerializeField] private Transform ragdollParent;

    [Header("Auto-collected (debug)")]
    [SerializeField] private Collider[] ragdollColliders;
    [SerializeField] private Rigidbody[] ragdollRigidbodies;

    [Header("Exclude from ragdoll control")]
    [SerializeField] private Collider[] excludedColliders;
    [SerializeField] private Rigidbody[] excludedRigidbodies;

    private void Awake()
    {
        if (ragdollParent == null)
        {
            Debug.LogError($"[{nameof(EnemyRagdoll)}] ragdollParent is not assigned on {name}", this);
            return;
        }

        ragdollColliders = ragdollParent.GetComponentsInChildren<Collider>(true);
        ragdollRigidbodies = ragdollParent.GetComponentsInChildren<Rigidbody>(true);

        RemoveExcludedParts();

        RagdollActive(false); // alive state by default
    }

    /// <summary>
    /// Enables/disables ragdoll physics by toggling Rigidbody.isKinematic.
    /// </summary>
    public void RagdollActive(bool active)
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            //Debug.Log(rb.gameObject.name);
            rb.isKinematic = !active;
        }
    }

    /// <summary>
    /// Enables/disables ragdoll colliders (useful to prevent post-death interactions).
    /// </summary>
    public void CollidersActive(bool active)
    {
        foreach (Collider cd in ragdollColliders)
        {
            cd.enabled = active;
        }
    }

    private void RemoveExcludedParts()
    {
        ragdollColliders = FilterColliders(ragdollColliders, excludedColliders);
        ragdollRigidbodies = FilterRigidbodies(ragdollRigidbodies, excludedRigidbodies);
    }

    private Collider[] FilterColliders(Collider[] source, Collider[] excluded)
    {
        if (source == null || source.Length == 0)
            return source;

        if (excluded == null || excluded.Length == 0)
            return source;

        List<Collider> filtered = new List<Collider>();

        foreach (Collider cd in source)
        {
            if (cd == null)
                continue;

            bool isExcluded = false;
            foreach (Collider ex in excluded)
            {
                if (cd == ex)
                {
                    isExcluded = true;
                    break;
                }
            }

            if (!isExcluded)
                filtered.Add(cd);
        }

        return filtered.ToArray();
    }

    private Rigidbody[] FilterRigidbodies(Rigidbody[] source, Rigidbody[] excluded)
    {
        if (source == null || source.Length == 0)
            return source;

        if (excluded == null || excluded.Length == 0)
            return source;

        List<Rigidbody> filtered = new List<Rigidbody>();

        foreach (Rigidbody rb in source)
        {
            if (rb == null)
                continue;

            bool isExcluded = false;
            foreach (Rigidbody ex in excluded)
            {
                if (rb == ex)
                {
                    isExcluded = true;
                    break;
                }
            }

            if (!isExcluded)
                filtered.Add(rb);
        }

        return filtered.ToArray();
    }
}
