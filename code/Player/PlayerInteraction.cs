using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player interaction with nearby <see cref="Interactable"/> objects.
/// 
/// How it works:
/// - <see cref="Interactable"/> objects add/remove themselves from this component via trigger enter/exit.
/// - This component tracks the currently closest interactable and highlights it.
/// - On Interact input, calls <see cref="Interactable.Interaction"/> on the closest interactable.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    #region Runtime

    // Maintained by Interactable.OnTriggerEnter/Exit.
    private readonly List<Interactable> interactables = new List<Interactable>();

    private Interactable closestInteractable;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        AssignInputEvents();
    }

    #endregion

    #region Interaction

    private void InteractWithClosest()
    {
        // Debug.Log("Interacting with closest interactable: " + closestInteractable?.gameObject.name);
        closestInteractable?.Interaction();
        interactables.Remove(closestInteractable);

        UpdateClosestInteractable();
    }

    public void UpdateClosestInteractable()
    {
        // Remove highlight from the old closest.
        closestInteractable?.HighlightActive(false);
        closestInteractable = null;

        float closestDistance = float.MaxValue;

        foreach (Interactable interactable in interactables)
        {
            float distance = Vector3.Distance(transform.position, interactable.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        // Highlight the new closest (if any).
        closestInteractable?.HighlightActive(true);
    }

    /// <summary>
    /// Exposes the internal list so <see cref="Interactable"/> can add/remove itself.
    /// NOTE: Returning the raw list is convenient but allows external modification.
    /// </summary>
    public List<Interactable> GetInteractables() => interactables;

    #endregion

    #region Input Wiring

    private void AssignInputEvents()
    {
        Player player = GetComponent<Player>();

        // Interact uses performed so one press triggers once.
        player.controls.Player.Interact.performed += context => InteractWithClosest();
    }

    #endregion
}
