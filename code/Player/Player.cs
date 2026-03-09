using UnityEngine;

/// <summary>
/// Player "hub" component.
/// 
/// Responsibilities:
/// - Owns the generated Input Actions asset instance (<see cref="InputSystem_Actions"/>)
/// - Caches references to the core player systems (aim, movement, weapons, visuals, interaction)
/// 
/// Notes:
/// - Systems access each other through this component (e.g., movement reads aim hit point).
/// - Controls are enabled/disabled via OnEnable/OnDisable for correct lifecycle behavior.
/// </summary>
public class Player : MonoBehaviour
{
    #region Public Properties

    public InputSystem_Actions controls { get; private set; }
    public PlayerAim aim { get; private set; }
    public PlayerMovement movement { get; private set; }
    public PlayerWeaponController weapon { get; private set; }
    public PlayerWeaponVisuals weaponVisuals { get; private set; }
    public PlayerInteraction interaction { get; private set; }

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        // Input Actions are created once per player and then enabled/disabled with GameObject lifecycle.
        controls = new InputSystem_Actions();

        // Cache system components for fast access.
        aim = GetComponent<PlayerAim>();
        movement = GetComponent<PlayerMovement>();
        weapon = GetComponent<PlayerWeaponController>();
        weaponVisuals = GetComponent<PlayerWeaponVisuals>();
        interaction = GetComponent<PlayerInteraction>();

        //Cursor.visible = false;
    }

    private void OnEnable()
    {
        controls.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }

    #endregion
}
