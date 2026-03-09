---
title: "Player Aim & Camera Target"
summary: "Mouse-world aiming, laser visualization, optional target lock, and camera look-ahead target movement."
order: 22
status: "In Development"
tags: ["Player", "Aim", "Camera", "Combat"]
last_updated: "2026-02-18"
---

## 🧭 Overview
`PlayerAim` controls:
- Converting mouse screen position into a world hit point (`GetMouseHitInfo()`).
- Positioning an `aim` Transform in the world.
- Rendering an aim laser using `LineRenderer`.
- Moving a `cameraTarget` Transform to create camera look-ahead.
- Optional target-lock behavior using a `Target` marker component.

## 🎯 Purpose
Create readable, satisfying top-down aiming:
- The player character rotates toward aim (via `PlayerMovement`).
- Bullets fire toward `aim` (via `PlayerWeaponController`).
- The camera subtly shifts toward aim direction for better situational awareness.

## 🧠 Design Philosophy
- Aim is treated as its own system, not a byproduct of camera rotation.
- Visual feedback (laser) is driven by weapon readiness to prevent “aiming while reloading/equipping”.
- Target lock uses a lightweight marker (`Target`) instead of heavy AI dependencies.

Trade-off: `PlayerAim` currently mixes aim + camera target logic in one script for simplicity.

## 📦 Core Responsibilities
**Does**
- Track mouse input from `controls.Player.Look`.
- Raycast from `Camera.main` into `aimLayerMask`.
- Keep a last-known hit point to avoid aim snapping when raycast misses.
- Update laser positions (including a small “laser tip” segment).
- Position `cameraTarget` using a clamped look-ahead distance.

**Does NOT**
- Change Cinemachine camera distance (handled by `CameraManager`).
- Handle actual firing logic (handled by `PlayerWeaponController`).

## 🧱 Key Components
Classes
- `PlayerAim` (`Scripts/Player/PlayerAim.cs`)
- `Target` (`Scripts/Target.cs`)
  - Simple marker; on `Start()` forces its GameObject layer to `Enemy`.

Unity components / references
- `LineRenderer` (aim laser)
- `Transform aim` (world-space aim point)
- `Transform cameraTarget` (camera follow/look-ahead target)
- LayerMask `aimLayerMask` (controls what aim raycast can hit)

## 🔄 Execution Flow
1. `Start()`
   - Cache `Player`
   - Subscribe to `Look` input action
2. `Update()`
   - (Temporary) debug toggles for precise aim / target lock using old input system (`P` and `L`)
   - `UpdateAimVisuals()`
     - Laser is enabled only when `player.weapon.WeaponReady() == true`
     - Weapon model is oriented to aim
     - Laser end point uses raycast against world to stop on obstacles
   - `UpdateAimPosition()`
     - If target lock enabled and a `Target()` exists → aim snaps to target center
     - Else aim uses mouse hit point
     - If not precise aim → aim.y is flattened to player height + 1
   - `UpdateCameraPosition()`
     - Moves `cameraTarget` toward `DesiredCameraPosition()` using `Lerp`

## 🔗 Dependencies
**Depends On**
- `Player` for weapon readiness and movement input.
- Unity: `Camera.main` raycasts (ScreenPointToRay), physics, `LineRenderer`.

**Used By**
- `PlayerMovement.ApplyRotation()` rotates toward `GetMouseHitInfo().point`.
- `PlayerWeaponController.BulletDirection()` uses `Aim()` and `Target()` to decide direction.

## ⚠ Constraints & Assumptions
- Uses `Camera.main` each aim raycast; if you ever remove the “MainCamera” tag, aiming will break.
- `Target()` checks only the transform hit by the aim raycast; no target prioritization.
- `Target` forces layer to `Enemy` on Start — relies on an “Enemy” layer existing in project settings.
- Precise aim / target lock toggles are currently using `Input.GetKeyDown` and marked as testing-only.

## 📈 Scalability & Extensibility
- Split camera target logic into its own system if this script grows.
- Expand target lock to select nearest target in range (only when you actually implement it).
- Add aim assist rules (snap strength, target filtering) without changing weapon firing code.

## ✅ Development Status
In Development

## 📝 Notes
Related devlogs:
- Devlog 04 – Camera, Aim Decomposition & Shooting Foundations
- Devlog 06 – Weapon system camera integration (distance changes per weapon)
