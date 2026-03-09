---
title: "Input Actions Integration"
summary: "How the project uses Unity Input System via the generated InputSystem_Actions wrapper."
order: 11
status: "In Development"
tags: ["Core", "Input", "Unity Input System"]
last_updated: "2026-02-18"
---

## 🧭 Overview
The project uses Unity’s **New Input System** via a generated wrapper class named `InputSystem_Actions`. Custom scripts subscribe to action callbacks (`performed`, `canceled`) rather than polling input every frame.

## 🎯 Purpose
Provide a scalable input foundation where:
- Input is centralized in a single actions asset (generated wrapper).
- Gameplay scripts subscribe to high-level intents (Move, Attack, Reload, Interact, etc.).
- Continuous actions (auto fire, sprint) are controlled by `performed/canceled` state.

## 🧠 Design Philosophy
- Prefer **event-driven** input over manual polling.
- Keep input subscriptions close to the system that owns the behavior (movement subscribes to Move/Sprint, weapon subscribes to Attack/Reload/etc.).

Trade-off: multiple scripts subscribe to the same `controls` instance, so unsubscribe patterns matter later (see constraints).

## 📦 Core Responsibilities
**Does**
- `Player` owns the `controls` instance and enables/disables it with the player lifecycle.
- Systems subscribe to the actions they need.

**Does NOT**
- Provide a custom input abstraction layer (no interfaces or rebinding UI code in this repo yet).
- Automatically unsubscribe action callbacks (current code relies on object lifetime).

## 🧱 Key Components
Classes
- `Player` (`Scripts/Player/Player.cs`)
  - Creates `controls = new InputSystem_Actions()` in `Awake()`.
  - Enables in `OnEnable()`, disables in `OnDisable()`.

Subscription points (real code)
- `PlayerMovement.AssignInputEvents()`
  - `Move` (performed/canceled), `Sprint` (performed/canceled)
- `PlayerAim.AssignInputEvents()`
  - `Look` (performed/canceled)
- `PlayerWeaponController.AssignInputEvents()`
  - `Attack` (performed/canceled), EquipSlot1–5, DropCurrentWeapon, Reload, ToggleWeaponMode
- `PlayerInteraction.AssignInputEvents()`
  - `Interact` (performed)

## 🔄 Execution Flow
1. `Player.Awake()` creates the controls object.
2. Each system subscribes to its needed actions in `Start()`.
3. `Player.OnEnable()` enables the input map.
4. Actions trigger callbacks which update system state (e.g., `isShooting`, `moveInput`) or invoke operations (e.g., `Reload()`, `EquipWeapon()`).

## 🔗 Dependencies
**Depends On**
- Unity Input System generated wrapper: `InputSystem_Actions`.

**Used By**
- Movement, aim, weapons, interaction.

## ⚠ Constraints & Assumptions
- Subscriptions are not explicitly removed; if you ever disable/enable these components independently, you may double-subscribe.
- `PlayerAim` currently has **temporary debug toggles** using the old input system:
  - `P` toggles precise aim
  - `L` toggles target lock
  These are explicitly marked in code as testing-only.

## 📈 Scalability & Extensibility
- Add new actions to the Input Actions asset and subscribe in the owning system.
- If you later add menus / remapping, introduce a dedicated “Input Router” system—but only when needed.

## ✅ Development Status
In Development

## 📝 Notes
Related devlog:
- Devlog 01 – Input & Player Controller Setup
