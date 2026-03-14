---
title: "Projectiles: Bullet, EnemyBullet & Impact FX"
summary: "Pooled projectile behavior: Bullet lifecycle (distance + trail fade), impact VFX, enemy/shield hit routing, and enemy-side projectile variants (EnemyBullet + EnemyGrenade)."
order: 34
status: "In Development"
tags: ["Combat", "Projectiles", "Pooling", "VFX", "Enemy"]
last_updated: "2026-03-14"
---

## 🧭 Overview
Projectile combat uses pooling and a shared lifecycle:

- `Bullet` is the base pooled projectile (player weapons).
- `EnemyBullet` extends `Bullet` to reuse the same pooling/trail/lifetime behavior, but overrides collision routing for enemy-fired bullets.
- `EnemyGrenade` is a pooled physics grenade used by `EnemyRange` (spawns pooled explosion FX on detonation).

## 🎯 Purpose
Provide consistent, performant projectile behavior with readable feedback:
- Trails make shot direction readable
- Impact VFX confirms contact
- Pooling keeps high fire rate viable

## 🧠 Design Philosophy
- Keep projectile lifecycle inside the projectile itself (pool-safe).
- Keep firing orchestration inside the owner (player weapon controller / enemy state).
- Use distance/timer lifetimes (no destroy).
- Route gameplay hit logic in collision handlers for deterministic feedback.

## 📦 Core Responsibilities

### `Bullet` (base)
**Does**
- Reset state on spawn via `BulletSetup(flyDistance, impactForce)`
- Track fly distance and disable collider/mesh after max distance
- Fade trail near end of travel (then return to pool)
- Spawn pooled impact FX on collision
- Route enemy interactions (shield-first, then enemy lookup)

**Does NOT**
- Manage player weapon cadence (handled by `PlayerWeaponController`)
- Apply complex damage logic (currently `Enemy.GetHit()` decrements by 1)

### `EnemyBullet`
**Does**
- Inherits pooling + trail fade + lifetime from `Bullet`
- Overrides collision to:
    - spawn impact FX
    - return to pool
    - detect `Player` in the hit hierarchy (currently logs hit; damage is TODO)

### `EnemyGrenade`
**Does**
- Computes ballistic launch velocity to hit a target in a given time
- Counts down to detonation (flight time + explosion timer)
- Spawns pooled explosion VFX
- Applies explosion force to the player (impulse-only currently)
- Returns itself to pool

## 🧱 Key Components
Classes
- `Bullet` (`code/Weapons/Bullet.cs`)
- `EnemyBullet` (`code/Enemy/EnemyBullet.cs`)
- `EnemyGrenade` (`code/Enemy/EnemyGrenade.cs`)

FX / pooling
- `ObjectPool` + `PooledObject`
- `bulletImpactFX` (pooled impact)

Enemy interaction
- `EnemyShield` (shield collider resolution)
- `Enemy` (`GetHit()` and death triggers)

## 🔄 Execution Flow
1. Spawn
- Player fires → `PlayerWeaponController` spawns pooled `Bullet`
- Ranged enemy fires → `EnemyRange.FireSingleBullet()` spawns pooled `EnemyBullet`
- Ranged enemy throws grenade → `ThrowGrenadeState_Range.AbilityTrigger()` → `EnemyRange.ThrowGrenade()` spawns pooled `EnemyGrenade`

2. Travel / lifetime
- Bullets track distance and fade out trails near max distance
- Grenade uses physics + timer until detonation

3. Collision / detonation
- Bullets spawn impact FX and return to pool
- Grenade spawns explosion FX, applies force, returns to pool

## 🔗 Dependencies
Depends On
- `ObjectPool`
- Physics collision layers configured so bullets collide with world + targets

Used By
- Player weapons (`PlayerWeaponController`)
- Ranged enemies (`EnemyRange`)

## ⚠ Constraints & Assumptions
- `Bullet` assumes it is spawned from `ObjectPool` and will be returned (not destroyed).
- `EnemyBullet` currently does not apply health damage to player (TODO).
- Grenade uses a fixed-time ballistic arc; if obstacles intervene, it may collide early depending on physics setup.

## ✅ Development Status
In Development