---
title: "Player Root Composition"
summary: "The Player script as a small composition root and shared access point to player subsystems."
order: 20
status: "In Development"
tags: ["Player", "Composition", "Core"]
last_updated: "2026-02-18"
---

## 🧭 Overview
`Player` is a lightweight “composition root” component that:
- Creates and owns the `InputSystem_Actions` instance.
- Caches references to the player subsystems (aim, movement, weapons, visuals, interaction).
- Enables/disables input with the Unity lifecycle.

## 🎯 Purpose
Avoid repeated `GetComponent<...>()` calls across scripts and provide a stable entry point for player-related systems to talk to each other through `player.<subsystem>` references.

## 🧠 Design Philosophy
- Keep `Player` minimal: it wires dependencies but doesn’t implement gameplay.
- Prefer cached component references over repeated lookups.
- Allow subsystems to depend on `Player` as a shared context.

Trade-off: this is a pragmatic “service locator on the player” pattern. It’s simple and efficient, but it does create coupling between subsystems.

## 📦 Core Responsibilities
**Does**
- Instantiate input actions wrapper (`controls`).
- Cache subsystem references:
  - `PlayerAim`
  - `PlayerMovement`
  - `PlayerWeaponController`
  - `PlayerWeaponVisuals`
  - `PlayerInteraction`
- Enable input in `OnEnable()` and disable input in `OnDisable()`.

**Does NOT**
- Contain player gameplay logic (movement/aim/shooting live in separate scripts).
- Manage UI, health, damage, etc. (not implemented in this repo).

## 🧱 Key Components
Classes
- `Player` (`Scripts/Player/Player.cs`)
  - Public read-only accessors for subsystems.

## 🔄 Execution Flow
1. `Awake()`
   - `controls = new InputSystem_Actions()`
   - `GetComponent<...>()` for each subsystem.
2. `OnEnable()` → `controls.Enable()`
3. `OnDisable()` → `controls.Disable()`

## 🔗 Dependencies
**Depends On**
- Generated `InputSystem_Actions`.
- All required player subsystem components must exist on the same GameObject (or it will return null).

**Used By**
- All player subsystems (they call `GetComponent<Player>()` and then use cached refs).

## ⚠ Constraints & Assumptions
- Assumes all subsystems exist on the Player object:
  - If a subsystem is missing, later usage can throw.
- Cursor visibility is currently commented out (future decision).

## 📈 Scalability & Extensibility
- Safe: adding new player subsystems and caching them here.
- If coupling becomes an issue later, move toward explicit interfaces or event-driven messaging.

## ✅ Development Status
In Development

## 📝 Notes
Related devlog:
- Devlog 01 – Input & Player Controller Setup
