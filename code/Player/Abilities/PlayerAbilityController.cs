using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central router for player ability usage.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Discover and cache all <see cref="PlayerAbility"/> components on the Player.</description></item>
/// <item><description>Provide typed ability lookup via <see cref="GetAbility{T}"/>.</description></item>
/// <item><description>Provide a safe activation attempt API via <see cref="TryActivate{T}"/>.</description></item>
/// <item><description>Wire input actions to ability activations (e.g., DiveRoll -> <see cref="PlayerRollAbility"/>).</description></item>
/// </list>
///
/// Why a controller:
/// - Keeps input wiring out of each ability.
/// - Keeps abilities modular and data-driven.
/// - Allows multiple abilities with consistent activation rules.
///
/// Key connections:
/// - Relies on <see cref="Player.controls"/> being created in <see cref="Player.Awake"/>.
/// - Activates concrete abilities such as <see cref="PlayerRollAbility"/>.
/// </remarks>
public class PlayerAbilityController : MonoBehaviour
{
    #region Components

    private Player player;

    #endregion

    #region Runtime

    /// <summary>
    /// Cache of abilities by runtime type.
    /// </summary>
    /// <remarks>
    /// Using Type keys allows fast lookups and avoids manual inspector wiring.
    /// </remarks>
    private readonly Dictionary<Type, PlayerAbility> abilitiesByType = new();

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        player = GetComponent<Player>();
        CacheAbilities();
    }

    private void Start()
    {
        AssignInputEvents();
    }

    private void OnDestroy()
    {
        UnassignInputEvents();
    }

    #endregion

    #region Ability Caching

    /// <summary>
    /// Finds all <see cref="PlayerAbility"/> components on this GameObject and stores them by their concrete type.
    /// </summary>
    private void CacheAbilities()
    {
        abilitiesByType.Clear();

        PlayerAbility[] abilities = GetComponents<PlayerAbility>();
        foreach (PlayerAbility ability in abilities)
            abilitiesByType[ability.GetType()] = ability;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets a cached ability of type <typeparamref name="T"/> if present on this Player.
    /// </summary>
    public T GetAbility<T>() where T : PlayerAbility
    {
        if (abilitiesByType.TryGetValue(typeof(T), out PlayerAbility ability))
            return ability as T;

        return null;
    }

    /// <summary>
    /// Attempts to activate an ability of type <typeparamref name="T"/>.
    /// </summary>
    /// <returns>True if the ability existed and successfully activated.</returns>
    public bool TryActivate<T>() where T : PlayerAbility
    {
        T ability = GetAbility<T>();

        if (ability == null || !ability.CanActivate())
            return false;

        ability.Activate();
        return true;
    }

    #endregion

    #region Input Wiring

    /// <summary>
    /// Subscribes to input actions that trigger abilities.
    /// </summary>
    /// <remarks>
    /// Kept defensive checks so missing input assets won't hard-crash during development.
    /// </remarks>
    private void AssignInputEvents()
    {
        if (player == null || player.controls == null)
            return;

        // Keep this as DiveRoll if that is the actual name in your Input Actions asset.
        player.controls.Player.DiveRoll.performed += OnDiveRollPerformed;
    }

    private void UnassignInputEvents()
    {
        if (player == null || player.controls == null)
            return;

        player.controls.Player.DiveRoll.performed -= OnDiveRollPerformed;
    }

    private void OnDiveRollPerformed(InputAction.CallbackContext context)
    {
        // Roll ability is activated via the typed controller API.
        TryActivate<PlayerRollAbility>();
    }

    #endregion
}