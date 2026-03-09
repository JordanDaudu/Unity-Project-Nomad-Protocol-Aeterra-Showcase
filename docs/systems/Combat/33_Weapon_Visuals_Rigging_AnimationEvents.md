---
title: "Weapon Visuals, Rigging & Animation Events"
summary: "Synchronizes weapon gameplay state with animator layers, rig weights, IK targets, and model switching."
order: 33
status: "In Development"
tags: ["Combat", "Animation", "Rigging", "Visuals"]
last_updated: "2026-02-18"
---

## 🧭 Overview
Weapon visuals are separated from weapon gameplay logic:
- `PlayerWeaponController` decides what happens.
- `PlayerWeaponVisuals` plays animations, manages rig weights, and swaps visible weapon models.
- `PlayerAnimationEvents` receives animation events and calls back into controller/visuals at exact frames.

Weapon models are represented by:
- `WeaponModel` (equipped/active weapon)
- `BackupWeaponModel` (hung weapons shown on body when not equipped)

## 🎯 Purpose
Make weapon handling look correct and remain gameplay-safe:
- No shooting while equipping/reloading.
- Correct IK/rig weights during transitions.
- Correct visible model (equipped vs backup).
- Animation layers per hold type.

## 🧠 Design Philosophy
- Use Animation Events as the “truth” for when gameplay state changes:
  - Weapon becomes ready only when equip animation finishes.
  - Reload refills ammo only when reload animation finishes.
  - Model switching happens at a timed point in animation.
- Keep visual responsibilities out of weapon controller.

Trade-off: animation parameter strings and layer indices become implicit contracts (documented below).

## 📦 Core Responsibilities
**Does**
- Trigger animations: Fire, Reload, EquipWeapon.
- Fade rig weight and left-hand IK back in after transitions.
- Switch animator layer based on weapon hold type.
- Activate the correct weapon model and correct backup models.
- Attach left hand IK target to the active weapon’s `holdPoint`.
- Provide animation event callbacks that:
  - Refill ammo at the correct time
  - Mark weapon ready when equip is done

**Does NOT**
- Decide weapon rules (fire rate, burst, ammo checks).
- Spawn bullets or pickups.

## 🧱 Key Components
Classes
- `PlayerWeaponVisuals` (`Scripts/Player/PlayerWeaponVisuals.cs`)
- `PlayerAnimationEvents` (`Scripts/Player/PlayerAnimationEvents.cs`)
- `WeaponModel` (`Scripts/Weapon/WeaponModel.cs`)
  - Contains `gunPoint` and `holdPoint` transforms
  - `HoldType` enum values start at 1 to match animator layer indices
- `BackupWeaponModel` (`Scripts/Weapon/BackupWeaponModel.cs`)
  - Contains `HangType` to control where the backup weapon appears

Unity components / references
- `Animator` (child)
- `Rig` (Animation Rigging)
- `TwoBoneIKConstraint` for left hand
- `Transform leftHandIK_Target`

Animator parameters used (string contracts)
- Triggers: `"Fire"`, `"Reload"`, `"EquipWeapon"`
- Floats: `"ReloadSpeed"`, `"EquipSpeed"`, `"EquipType"`
- Floats/Bool: `"xVelocity"`, `"zVelocity"`, `"isRunning"` (movement script)

## 🔄 Execution Flow
1. On equip (`PlayWeaponEquipAnimation()`):
   - Set left hand IK weight to 0
   - Reduce rig weight (snap to 0.15)
   - Trigger `"EquipWeapon"`
   - Set `"EquipType"` and `"EquipSpeed"` floats
2. Animation event calls:
   - `SwitchOnWeaponModel()` → visuals switch active model + layers + IK attachment
   - `WeaponEquipingIsOver()` → controller sets `weaponReady = true`
3. On reload (`PlayReloadAnimation()`):
   - Set `"ReloadSpeed"` float
   - Trigger `"Reload"`
   - Reduce rig weight
4. Animation event calls:
   - `ReloadIsOver()` → refill bullets + set ready true + start rig fade-in
   - `ReturnRig()` → fade rig and IK back up
5. Backup weapon visuals:
   - When multiple weapons exist, `SwitchOnBackupWeaponModel()` picks models by `HangType` categories and activates them.

## 🔗 Dependencies
**Depends On**
- `Player` (weapon state and inventory checks).
- Unity Animation Rigging package (`Rig`, `TwoBoneIKConstraint`).
- Animator controller configured with expected layers/parameters.

**Used By**
- `PlayerWeaponController` triggers equip/reload/fire and relies on animation events to toggle readiness.

## ⚠ Constraints & Assumptions
- `CurrentWeaponModel()` must exist for the current weapon type; otherwise equip logs an error.
- `HoldType` enum integer values must match animator layer indices (starts at 1).
- Rig weight is “snapped down” to 0.15 during transitions; the exact value is a tuning knob.
- Backup models are activated only if their weapon type exists in inventory and is not the currently equipped weapon.

## 📈 Scalability & Extensibility
- Add new weapon hold styles by adding a new animator layer and matching a new `HoldType`.
- Add new hang points by extending `HangType` and adding models.
- If you later support more complex rigs, keep animation events as the sync point.

## ✅ Development Status
In Development

## 📝 Notes
Related devlogs:
- Devlog 02 – Animation foundation
- Devlog 03 – Weapon rigging, visuals, and animation events
