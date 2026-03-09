using UnityEngine;

/// <summary>
/// Simple marker/test component used for aim/lock-on logic.
/// 
/// Current behavior:
/// - Forces this object to be on the "Enemy" layer at runtime.
/// 
/// Notes:
/// - <see cref="PlayerAim"/> checks for this component when determining a lock-on target.
/// - The Rigidbody requirement ensures the object participates in physics interactions (e.g., bullet collision).
/// </summary>
[RequireComponent(typeof(Rigidbody))]

public class Target : MonoBehaviour
{
    private void Start()
    {
        // Ensures consistent layer for aiming and collision filtering.
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }
}
