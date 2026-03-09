---
title: "Interaction System"
summary: "Closest-interactable tracking with highlight feedback and a virtual Interaction() contract."
order: 40
status: "In Development"
tags: ["Interaction", "Gameplay", "UI Feedback"]
last_updated: "2026-02-18"
---

## 🧭 Overview
The interaction framework is built around:
- `Interactable` base class (trigger registration + highlight + virtual `Interaction()`).
- `PlayerInteraction` controller (tracks nearby interactables, finds closest, triggers interaction on input).

Pickups (weapons/ammo) derive from `Interactable` and plug in without adding new “ifs” to the player.

## 🎯 Purpose
Create a scalable interaction foundation where adding new interactables (doors, terminals, NPCs) is done by:
- Creating a new script inheriting `Interactable`
- Overriding `Interaction()`

No changes are required in the player logic.

## 🧠 Design Philosophy
- Use inheritance for a simple “interaction contract”.
- Keep the player responsible only for:
  - maintaining candidates
  - selecting closest
  - invoking interaction
- Keep interactables responsible for:
  - registering/unregistering via triggers
  - highlighting feedback
  - performing the actual interaction logic

Trade-off: inheritance is simple, but you may switch to interfaces/events later if multiple interaction styles emerge.

## 📦 Core Responsibilities
**Does**
- Register interactables entering/exiting player trigger.
- Track closest interactable continuously based on distance.
- Highlight only the closest interactable.
- Invoke `Interaction()` on the closest when input is pressed.

**Does NOT**
- Decide what interaction means for each object (that’s per subclass).
- Handle UI prompts (not implemented yet).

## 🧱 Key Components
Classes
- `Interactable` (`Scripts/Interactable.cs`)
  - Trigger enter/exit registration
  - `HighlightActive(bool)`
  - `virtual Interaction()`
- `PlayerInteraction` (`Scripts/Player/PlayerInteraction.cs`)
  - List of nearby interactables
  - Closest selection + highlight toggling
  - Input binding to interact

Unity components / references
- Interactables require a trigger collider.
- Highlight uses a `Material highlightMaterial` and swaps MeshRenderer materials.

## 🔄 Execution Flow
1. Player enters an interactable trigger:
   - `Interactable.OnTriggerEnter()`
   - Adds itself to `PlayerInteraction` list
   - Calls `UpdateClosestInteractable()`
2. Player exits:
   - Removed from list
   - Closest recalculated
3. On Interact input:
   - `PlayerInteraction.InteractWithClosest()`
   - Calls `closestInteractable.Interaction()`
   - Removes interacted object from list
   - Recalculates closest

## 🔗 Dependencies
**Depends On**
- `PlayerInteraction` component on the player.
- `PlayerWeaponController` is cached in `Interactable` for pickup subclasses.
- Unity: trigger colliders, `MeshRenderer`.

**Used By**
- `PickupWeapon`, `PickupAmmo` (inherit `Interactable`).

## ⚠ Constraints & Assumptions
- `Interactable.Start()` caches `meshRenderer` using `GetComponentInChildren<MeshRenderer>()`.
  - If an interactable has no MeshRenderer in children, highlighting will break.
- Highlight swaps `meshRenderer.material` which can create runtime material instances (allocations) if used heavily.
- Closest selection is recalculated by scanning the list; fine at current scale.

## 📈 Scalability & Extensibility
- Add UI “Press E” prompt by exposing `closestInteractable` changes (not implemented yet).
- Add priority rules (e.g., always prefer doors over pickups) by extending distance check logic.
- Convert highlight to a shader property block later if material allocations become an issue.

## ✅ Development Status
In Development

## 📝 Notes
Related devlog:
- Devlog 08 – Interaction system + closest interactable + highlighting
