# Devlog 08 – Interaction System, Weapon Pickups & Ammo Architecture

Date: 2026-02-18

## 🎯 Goal
Design and implement a scalable interaction system that supports inheritance-based interactables, closest-object detection, weapon pickups, weapon drops, and ammo box logic — fully integrated with the existing weapon and pooling architecture.

---

## 🧠 Design Approach
This phase focused on creating a **generic interaction foundation** instead of hardcoding pickup logic directly into the player.

Key architectural decisions:

- Create a base `Interactable` class to unify all interactable behaviors.
- Use inheritance for specific interaction types (weapon, ammo, future interactables).
- Let the player track nearby interactables and determine the closest one.
- Separate interaction logic from weapon logic.
- Integrate object pooling to avoid unnecessary instantiation/destruction.
- Maintain clean state transitions between inventory, visuals, and world objects.

The goal was to make adding future interactables trivial and scalable.

---

## 🏗 Implementation

### 🧩 Base Interactable System
Created a base `Interactable` class responsible for:

- Trigger enter/exit registration
- Automatic closest-interactable tracking
- Highlight activation/deactivation
- Defining a virtual `Interaction()` method

This ensures future interactables (doors, terminals, NPCs, etc.) can plug into the system without modifying player logic.

---

### 👤 Player Interaction Controller
Implemented `PlayerInteraction` which:

- Maintains a list of nearby interactables
- Dynamically determines the closest one
- Highlights only the closest object
- Executes interaction via input event

This isolates interaction management from gameplay systems.

---

### 🔫 Weapon Pickup System (Rule-Based Logic)

`PickupWeapon` inherits from `Interactable`.

Three structured pickup rules were implemented:

1. If the weapon already exists in inventory → transfer only bullets.
2. If inventory is full and weapon type differs → replace current weapon.
3. If slots are available → add weapon normally.

Weapon dropping:

- Converts the current weapon into a pooled pickup object.
- Uses ObjectPool instead of instantiating new objects.
- Maintains weapon runtime data via constructor architecture.

---

### 💣 Ammo Pickup System

`PickupAmmo` inherits from `Interactable`.

Features:

- Small and big ammo box types.
- Per-weapon ammo configuration via structured `AmmoData`.
- Randomized but clamped ammo distribution.
- Adds ammo only to owned weapons.
- Returns to pool after interaction.

Fully data-driven and extensible.

---

### ♻ Object Pool Enforcement

All pickup objects:

- Use `PooledObject` to track original prefab.
- Are returned to the global ObjectPool.
- Avoid runtime instantiation/destruction.

Important workflow rule:
Every prefab intended for pooling must include a `PooledObject` component.

---

### 🎨 Visual Improvements

- Added small and big chest models.
- Big boxes provide higher ammo values.
- Highlight system provides visual clarity for interaction feedback.

---

## ⚠ Problems Encountered

- Managing multiple interactables within range without ambiguity.
- Preventing duplicate weapons in inventory.
- Ensuring smooth weapon replacement logic.
- Maintaining pooling consistency for scene-placed instances.
- Keeping interaction decoupled from weapon implementation.

---

## ✅ Solutions

- Closest interactable recalculated dynamically on trigger events.
- Clear rule-based pickup hierarchy.
- Enforced pooled object requirements.
- Inheritance-based interaction system.
- Clean separation between interaction and weapon systems.

---

## 🚀 Result

- Fully functional interaction framework.
- Intelligent weapon pickup and replacement logic.
- Ammo box system with scalable configuration.
- Seamless pooling integration.
- Extensible architecture ready for more interaction types.

---

## 📈 Engineering Takeaways

- Interaction systems scale best when built on inheritance or interfaces.
- Clear rule hierarchies prevent edge-case chaos.
- Pooling must be enforced consistently across all reusable prefabs.
- Decoupling interaction from gameplay logic simplifies expansion.
- Designing for future extensibility reduces technical debt.

---

## ➡ Next Steps – Enemy Melee System

- Design enemy base architecture
- Implement AI navigation and state machine
- Add core enemy states: Idle, Move, Chase, Attack, Recovery, Dead
- Integrate enemy animations and ragdoll
- Implement enemy abilities (shield, dodge, axe throw, multi-slash)
- Add simple health and damage system

This marks the transition into full AI-driven combat.