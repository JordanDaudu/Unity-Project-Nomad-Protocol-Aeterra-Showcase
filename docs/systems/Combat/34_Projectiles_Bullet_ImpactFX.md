---
title: "Projectiles: Bullet & Impact FX"
summary: "Pooled bullet behavior: travel distance, trail fade-out, collision impact VFX, enemy/shield hit routing, and pool return."
order: 34
status: "In Development"
tags: ["Combat", "Projectiles", "Pooling", "VFX", "Enemy"]
last_updated: "2026-03-05"
---

## 🧭 Overview
Bullets are pooled GameObjects controlled by `Bullet`. They are spawned and launched by `PlayerWeaponController`.

At runtime a bullet:
- Tracks its start position and max fly distance
- Disables collider/mesh after traveling far enough (trail continues fading)
- Spawns pooled impact VFX on collision
- Routes hit logic (shield-first, then enemy)
- Returns itself to the pool

## 🎯 Purpose
Provide consistent, performant projectile behavior with readable feedback:
- Trails make shot direction readable
- Impact VFX confirms contact
- Pooling keeps high fire rate viable

## 🧠 Design Philosophy
- Keep projectile lifecycle inside `Bullet`.
- Keep firing orchestration inside `PlayerWeaponController`.
- Use a distance-based lifetime (no destroy).
- Route enemy interactions in the bullet collision handler to keep hit feedback deterministic.

## 📦 Core Responsibilities
**Does**
- Reset bullet state on spawn via `BulletSetup(flyDistance, impactForce)`.
- Disable collision and mesh once past max distance.
- Fade trail near the end of travel.
- Spawn pooled impact FX on `OnCollisionEnter`.
- Hit routing:
  1) If the hit collider has `EnemyShield` → reduce durability and stop.
  2) Else, find `Enemy` via `GetComponentInParent<Enemy>()` → `GetHit()` + `DeathImpact()`.
- Return bullet + impact FX to pool.

**Does NOT**
- Implement damage values (health decrement is currently in `Enemy.GetHit()`).
- Perform hitscan; it uses physics collisions.

## 🧱 Key Components
Classes
- `Bullet` (`code/Bullet.cs`)

Unity components (required)
- `Rigidbody`
- `BoxCollider`
- `MeshRenderer`
- `TrailRenderer`

Prefab refs
- Impact FX prefab (`bulletImpactFX`) (also pooled)

## 🔄 Execution Flow
1. Spawn
   - `PlayerWeaponController` spawns a pooled bullet and calls `BulletSetup(currentWeapon.gunDistance, impactForce)`.
2. Flight (`Update()`)
   - `FadeTrailIfNeeded()` reduces `TrailRenderer.time` near end-of-life.
   - `DisableBulletIfNeeded()` disables collider/mesh after exceeding fly distance.
   - `ReturnToPoolIfNeeded()` returns bullet when the trail fully fades.
3. Collision (`OnCollisionEnter`)
   - Spawn pooled impact FX at contact point.
   - Return bullet to pool (pool return is slightly delayed so collision logic can still run).
   - Shield-first routing:
     - If `EnemyShield` on hit object → `ReduceDurability()` and return.
     - Else if `Enemy` in parent → `GetHit()` and `DeathImpact(force, hitPoint, attachedRigidbody)`.

## 🔗 Dependencies
**Depends On**
- `ObjectPool` for bullet and impact FX pooling
- Physics collisions (`OnCollisionEnter`)
- Enemy systems (`Enemy`, `EnemyShield`) for hit routing

**Used By**
- `PlayerWeaponController`

## ⚠ Constraints & Assumptions
- `BulletSetup()` adds a small distance buffer (`+ 0.5f`) to match the aim laser “tip” segment.
- Trail fade uses tuned constants for visuals (start fading at `flyDistance - 1.5f`, fade speed multiplier `8f`).
- `DeathImpact()` assumes the collided ragdoll bone collider has an `attachedRigidbody`.

## 📈 Scalability & Extensibility
- Add surface-based impact variations (tag/material based) by branching in `CreateImpactFx()`.
- Add penetration/ricochet by changing collision behavior before returning to pool.

## ✅ Development Status
In Development

## 📝 Notes
Related devlogs:
- Devlog 04 – Shooting foundations
- Devlog 05 – Collision reliability + impact VFX
- Devlog 06/07 – Pooling evolution
- Devlog 09 – Bullet → enemy/shield integration and death impact
