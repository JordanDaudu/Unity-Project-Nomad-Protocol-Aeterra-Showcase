---
title: "Player Weapon Controller"
summary: "Owns weapon inventory, equips weapons, drives shooting and reloading, and spawns pooled bullets/pickups."
order: 32
status: "In Development"
tags: ["Combat", "Weapons", "Player", "Enemy"]
last_updated: "2026-03-05"
---

## 🧭 Overview
`PlayerWeaponController` orchestrates the weapon gameplay loop:
- Weapon inventory (slots)
- Equip / drop / pickup behavior
- Firing logic (semi/auto/burst)
- Bullet spawning (pooled)
- Reload flow (animation-driven readiness gate)
- Camera distance change when equipping a weapon
- Enemy reaction hook (dodge trigger along bullet path)

## 🎯 Purpose
Provide one place that translates player input into weapon gameplay outcomes while keeping:
- Weapon rules inside `Weapon`
- Visuals inside `PlayerWeaponVisuals`
- Pooling inside `ObjectPool`

## 🧠 Design Philosophy
- Treat `weaponReady` as a simple combat gate:
  - false during equip/reload/burst
  - true when animation events confirm completion
- Use input `performed/canceled` to support auto firing without per-frame input polling.
- Keep pickup/drop logic rule-based to avoid “if spaghetti”.

Trade-off: controller currently owns multiple concerns (inventory + firing + pickups). This is fine at current scale, but may split later.

## 📦 Core Responsibilities
**Does**
- Create starting weapon from `defaultWeaponData`.
- Equip weapon by slot index and trigger equip animation.
- Set camera distance via `CameraManager`.
- Handle pickup rules:
  1) If weapon already owned → convert pickup into ammo (adds picked weapon’s `bulletsInMagazine` into existing weapon’s reserve)
  2) If inventory full and different type → replace current weapon and drop old as pickup
  3) Else → add weapon and enable backup visuals
- Drop current weapon as a pooled pickup object.
- Fire bullets using pooling and per-weapon spread.
- Trigger reload animation and rely on animation event to refill ammo.
- Trigger enemy dodge reaction with a raycast along the fired shot direction (before spread is applied).

**Does NOT**
- Control weapon visuals directly (delegates to `PlayerWeaponVisuals`).
- Implement aim raycasting (delegates to `PlayerAim`).
- Apply damage numbers (enemy health decrement lives in `Enemy.GetHit()` today).

## 🧱 Key Components
Classes
- `PlayerWeaponController` (`code/Player/PlayerWeaponController.cs`)
- `Weapon` runtime model
- `WeaponData` (starting weapon)
- `ObjectPool` (bullets + dropped weapon pickups)

Unity references
- `bulletPrefab`
- `weaponPickupPrefab`
- `Transform weaponHolder` (declared but not used in current code)

## 🔄 Execution Flow
1. `Start()`
   - Cache `Player`
   - Subscribe to input events
   - `Invoke(EquipStartingWeapon, 0.1f)` (delayed initialization)
2. `Update()`
   - If `isShooting` → call `Shoot()`
3. Equip flow
   - `EquipWeapon(slotIndex)`
     - `weaponReady = false`
     - Set `currentWeapon`
     - `player.weaponVisuals.PlayWeaponEquipAnimation()`
     - `CameraManager.ChangeCameraDistance(currentWeapon.cameraDistance)`
   - Animation event `WeaponEquipingIsOver()` sets `weaponReady = true`
4. Shooting flow
   - If not ready → return
   - If `currentWeapon.CanShoot()` false → return
   - **Enemy reaction hook**: `TriggerEnemyDodge()` raycasts along `BulletDirection()` before spread is applied
   - Trigger fire animation
   - If semi-auto → reset `isShooting = false`
   - If burst active → coroutine fires bullets with delay, then sets ready true
   - Else fire single bullet
5. Reload flow
   - Input triggers `Reload()` only if `currentWeapon.CanReload()` and `weaponReady`
   - `Reload()` sets ready false and triggers reload animation
   - Animation event `ReloadIsOver()` calls `RefillBullets()` and sets ready true
6. Drop flow
   - If only one weapon → ignore
   - Spawn pooled `weaponPickupPrefab` and `SetupPickupWeapon(currentWeapon, playerTransform)`
   - Remove current weapon and equip slot 0

## 🔗 Dependencies
**Depends On**
- `Player` (aim + visuals)
- `ObjectPool` (bullets + pickups)
- `CameraManager` (camera distance)
- Unity: coroutines, Rigidbody physics

**Used By**
- `PickupWeapon` calls `weaponController.PickupWeapon(weapon)` (via inherited reference in `Interactable`).

## ⚠ Constraints & Assumptions
- `weaponSlots[0] = new Weapon(defaultWeaponData);` assumes `weaponSlots` list is pre-sized in Inspector.
- EquipSlot1..5 input calls `EquipWeapon(0..4)` but `maxWeaponSlots` defaults to 2 — extra slots will log “No weapon in this slot!” unless list is populated.
- `weaponHolder` is currently unused (potential future attachment point).
- Enemy dodge trigger uses a single raycast and triggers `EnemyMelee.ActivateDodgeRoll()` if an enemy is hit.

## 📈 Scalability & Extensibility
- Add dedicated inventory model if weapon logic grows (not required yet).
- Add UI ammo display by reading `CurrentWeapon()` state.
- Add new weapon types by creating new `WeaponData` assets and adding weapon models.
- Extend enemy reaction hooks (stagger, suppression) while keeping the firing order deterministic.

## ✅ Development Status
In Development

## 📝 Notes
- Ammo persistence on drop/re-pickup is achieved by passing the same runtime `Weapon` instance through the pooled `PickupWeapon` object.
