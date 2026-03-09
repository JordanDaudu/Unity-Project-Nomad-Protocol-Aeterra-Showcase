---
title: "Pickups: Weapons & Ammo"
summary: "Interactable-derived pickups that integrate with weapon inventory and global pooling."
order: 41
status: "In Development"
tags: ["Interaction", "Pickups", "Combat"]
last_updated: "2026-02-18"
---

## 🧭 Overview
Pickups are implemented as interactables:
- `PickupWeapon : Interactable`
- `PickupAmmo : Interactable`

Both call into `PlayerWeaponController` via the inherited `weaponController` reference and return themselves to the `ObjectPool` after use.

## 🎯 Purpose
Support scalable world items without adding special-case player logic:
- Weapon pickups can add/replace weapons and preserve ammo state.
- Ammo boxes can add reserve ammo for weapons the player owns.
- All pickup objects are pooled for performance.

## 🧠 Design Philosophy
- Pickups are “dumb”: they hold data + implement `Interaction()`.
- Inventory rules live in `PlayerWeaponController`.
- Pickups return to pool instead of destroying.

Trade-off: `PickupWeapon` and `PickupAmmo` rely on `Interactable` to cache `weaponController` on trigger enter.

## 📦 Core Responsibilities
**Does**
- Weapon pickup:
  - Store `WeaponData` / runtime `Weapon`
  - On interaction: call `weaponController.PickupWeapon(weapon)`
- Ammo pickup:
  - Define ammo lists per box type (small/big)
  - Randomize ammo amount within min/max
  - Add ammo only to weapons currently owned
- Both:
  - Update visual model and mesh renderer for highlighting
  - Return to pool after interaction

**Does NOT**
- Spawn themselves (spawning is handled by scene placement or weapon drop logic).
- Implement UI feedback beyond highlight.

## 🧱 Key Components
Classes
- `PickupWeapon` (`Scripts/Pickups/PickupWeapon.cs`)
  - Fields: `weaponData`, `weapon`, `models`, `oldWeapon`
  - `SetupPickupWeapon(Weapon weapon, Transform dropper)`
  - `SetupGameObject()` and `SetupWeaponModel()`
- `PickupAmmo` (`Scripts/Pickups/PickupAmmo.cs`)
  - `AmmoData` struct and `AmmoBoxType` enum
  - Two ammo tables: `smallBoxAmmo`, `bigBoxAmmo`
  - `SetupBoxModel()`

Assets / models
- Weapon pickup uses `BackupWeaponModel[] models` as the world representation.
- Ammo pickup uses `GameObject[] boxModel` (index matches `AmmoBoxType` enum).

## 🔄 Execution Flow
Weapon pickup (scene-placed)
1. `Start()`
   - If `oldWeapon == false` → `weapon = new Weapon(weaponData)`
   - `SetupGameObject()` activates correct model and updates MeshRenderer
2. Player interacts:
   - Controller handles inventory rules
   - Pickup returns to pool

Weapon drop (runtime)
1. `PlayerWeaponController.CreateWeaponOnTheGround()`
   - Gets pooled pickup object and calls `SetupPickupWeapon(currentWeapon, playerTransform)`
2. Player interacts with dropped weapon:
   - `Interaction()` passes stored `Weapon` instance to controller
   - Pickup returns to pool

Ammo pickup
1. `Start()` picks correct box model
2. On interaction:
   - Select ammo list by box type
   - For each entry: if player owns that weapon type → add randomized reserve ammo
   - Return to pool

## 🔗 Dependencies
**Depends On**
- `Interactable` registration + `weaponController` caching.
- `PlayerWeaponController` inventory functions:
  - `PickupWeapon(Weapon)`
  - `HasWeaponInSlots(WeaponType)`
- `ObjectPool` for returning pickup objects.

**Used By**
- `PlayerWeaponController` uses a pooled `weaponPickupPrefab` to drop weapons.

## ⚠ Constraints & Assumptions
- **Pooling + Start()**: `Start()` runs only once per pooled object.
  - `PickupWeapon` relies on `Start()` for initial SetupGameObject.
  - When using pooled pickups for dropped weapons, `SetupPickupWeapon()` does not call `SetupGameObject()` in current code.
    - This can cause the wrong model/mesh to remain active after reuse.
- `PickupAmmo` similarly sets its model only in `Start()`; safe as long as `ammoBoxType` doesn’t change at runtime.
- `Interactable` expects a MeshRenderer for highlighting; pickup model switching calls `UpdateMeshAndMaterial()` to align highlight with active model.

## 📈 Scalability & Extensibility
- Add new pickup types by inheriting `Interactable` and overriding `Interaction()`.
- Add new ammo box tiers by extending `AmmoBoxType` and adding models + lists.
- Add “weapon pickup UI” later without changing pickup logic (hook into closest interactable selection).

## ✅ Development Status
In Development

## 📝 Notes
Related devlog:
- Devlog 08 – Weapon pickup/drop rules + ammo pickups
