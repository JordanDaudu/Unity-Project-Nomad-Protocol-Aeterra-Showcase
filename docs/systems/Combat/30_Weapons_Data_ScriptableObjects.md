---
title: "Weapons Data (ScriptableObjects)"
summary: "WeaponData assets define static tuning and defaults for each weapon type."
order: 30
status: "In Development"
tags: ["Combat", "Weapons", "Data-Driven", "ScriptableObjects"]
last_updated: "2026-02-19"
---

## 🧭 Overview
Weapons are configured using a `WeaponData : ScriptableObject` asset. These assets define **static defaults** and tuning values. Runtime state is stored in the `Weapon` class (see Weapon Runtime Model doc).

Current weapon assets in the repo:
- `Weapon_Auto-Rifle_D.asset`, `Weapon_Pistol_D.asset`, `Weapon_Revolver_D.asset`, `Weapon_Rifle_D.asset`, `Weapon_Shotgun_D.asset`

## 🎯 Purpose
Enable data-driven iteration:
- Balance weapon stats in the Inspector.
- Add new weapons by creating new assets (without changing code).

## 🧠 Design Philosophy
- Keep static configuration (design-time) separate from runtime state (ammo usage, cooldown timestamps).
- Prefer ScriptableObjects for tuning in Unity.

Trade-off: the runtime weapon still stores ammo values that originate from data; changes to an asset won’t retroactively change an already-instantiated `Weapon` instance.

## 📦 Core Responsibilities
**Does**
- Store default values for:
  - Ammo (magazine, capacity, reserve)
  - Shoot mode (semi/auto)
  - Fire rate, bullets-per-shot
  - Burst settings (optional)
  - Spread settings
  - Weapon-specific tuning (reload/equip speed, gun distance, camera distance)
- Provide a single source of truth for weapon defaults.

**Does NOT**
- Track runtime weapon state (current ammo after firing, last fire timestamps, current spread).
- Implement weapon behavior (shooting, reloading, equipping are in controller/runtime model).

## 🧱 Key Components
Data
- `WeaponData` (`Scripts/Weapon/WeaponData.cs`)
  - `CreateAssetMenu`: `"ScriptableObjects/Weapon Data"`

## 🔄 Execution Flow
1. Designer creates/edits `WeaponData` assets in Inspector.
2. Runtime creates a `Weapon` instance using `new Weapon(weaponData)`.
3. `Weapon` copies data values into runtime fields.

## 🔗 Dependencies
**Depends On**
- Unity `ScriptableObject`.
- Enums `WeaponType`, `ShootType` (declared in `Weapon.cs`).

**Used By**
- `Weapon` constructor reads from `WeaponData`.
- `PlayerWeaponController` spawns a starting weapon from `defaultWeaponData`.
- `PickupWeapon` uses `weaponData` to create pickup weapons.

## ⚠ Constraints & Assumptions
- Ammo fields exist in `WeaponData` even though they are runtime-like; they serve as defaults for the runtime instance.
- Ranges are used for Inspector tuning (reload/equip/gun/camera distance).

## 📈 Scalability & Extensibility
- Add new tuning fields here only when they are truly “static config”.
- Future: upgrades/mods would likely be separate assets layered on top (not implemented yet).

## ✅ Development Status
In Development

## 📝 Notes
Related devlog:
- Devlog 07 – Data-driven weapons (ScriptableObjects)
