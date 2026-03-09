using UnityEngine;

/// <summary>
/// Base class for world objects the player can interact with.
/// 
/// Responsibilities:
/// - Register/unregister itself into <see cref="PlayerInteraction"/> when the player enters/exits the trigger.
/// - Provide a virtual <see cref="Interaction"/> entry point to override in subclasses (pickups, doors, etc.).
/// - Handle highlight visuals for "closest interactable" feedback.
/// 
/// Usage:
/// - Subclasses should have a Trigger Collider and call base trigger methods (default behavior).
/// - <see cref="PlayerInteraction"/> decides which interactable is closest and calls <see cref="Interaction"/>.
/// </summary>
public class Interactable : MonoBehaviour
{
    #region Inspector

    [SerializeField] private Material highlightMaterial;

    #endregion

    #region Runtime / Cached References

    // Cached when the player enters this interactable's trigger.
    protected PlayerWeaponController weaponController;

    // Renderer used for highlighting (often a child mesh on the pickup model).
    protected MeshRenderer meshRenderer;

    protected Material defaultMaterial;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        // Allow subclasses to assign meshRenderer before Start (e.g., after swapping models).
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        // sharedMaterial keeps the original asset reference (avoid instantiating a material here).
        defaultMaterial = meshRenderer.sharedMaterial;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Override this in subclasses to implement the actual interaction behavior.
    /// </summary>
    public virtual void Interaction()
    {
        // Intentionally empty: subclasses implement pickup/door logic, etc.
        // Debug.Log("Interacted with " + gameObject.name);
    }

    /// <summary>
    /// Enables/disables a highlight material when this interactable becomes the closest one.
    /// </summary>
    public void HighlightActive(bool active)
    {
        // NOTE: .material instantiates a unique material instance (per renderer).
        // This is acceptable for small counts of interactables, but be aware of memory if scaling up.
        if (active)
            meshRenderer.material = highlightMaterial;
        else
            meshRenderer.material = defaultMaterial;
    }

    #endregion

    #region Protected Helpers

    /// <summary>
    /// Used by subclasses when they change the active mesh/model and want highlighting to keep working.
    /// </summary>
    protected void UpdateMeshAndMaterial(MeshRenderer newMesh)
    {
        meshRenderer = newMesh;
        defaultMaterial = meshRenderer.sharedMaterial;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Cache the player's weapon controller for subclasses that need it (weapon pickup/ammo box).
        if (weaponController == null)
            weaponController = other.GetComponent<PlayerWeaponController>();

        PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

        if (playerInteraction == null)
            return;

        // PlayerInteraction maintains the active list and picks the closest interactable.
        playerInteraction.GetInteractables().Add(this);
        playerInteraction.UpdateClosestInteractable();
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

        if (playerInteraction == null)
            return;

        playerInteraction.GetInteractables().Remove(this);
        playerInteraction.UpdateClosestInteractable();
    }

    #endregion
}
