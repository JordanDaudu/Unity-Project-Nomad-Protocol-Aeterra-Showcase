---
title: "Enemy Ranged AI"
summary: "Simplified ranged archetype with patrol → battle state and pooled projectile firing with cooldown windows."
order: 53
status: "Prototype"
tags: ["Enemy", "AI", "Ranged", "Projectiles"]
last_updated: "2026-03-05"
---

## 🧭 Overview
`EnemyRange` is the current ranged archetype. It extends `Enemy` and uses the shared FSM foundation with a smaller set of states:
- `IdleState_Range`
- `MoveState_Range`
- `BattleState_Range`

In battle state, it faces the player and fires pooled bullets at a configured fire rate, with a "magazine"-like burst size and cooldown.

## 🎯 Purpose
Establish a second enemy archetype (beyond melee) to validate that:
- The shared `Enemy` + `EnemyStateMachine` foundation supports multiple behaviors
- Projectile combat can be driven cleanly from AI state updates

## 🧠 Design Philosophy
- **Small state surface** initially: focus on battle loop first.
- **Performance-friendly projectiles**: bullets are pooled.
- **Tunable fire windows**: `bulletsToShoot` + `weaponCooldownTime` create predictable cadence.

## 📦 Core Responsibilities
**Does**
- Build and tick ranged states
- Enter battle mode when player is in aggression range
- In `BattleState_Range`:
  - Face player
  - Fire bullets at `fireRate`
  - Apply cooldown after `bulletsToShoot`

**Does NOT**
- Implement complex positioning/cover logic yet
- Share melee-specific variant systems (shield/dodge/axe)

## 🧱 Key Components
Classes
- `EnemyRange` (`code/Enemy/EnemyRange/EnemyRange.cs`)
- States
  - `IdleState_Range`
  - `MoveState_Range`
  - `BattleState_Range`
- `EnemyBullet` (`code/Enemy/EnemyBullet.cs`) pooled projectile used by ranged enemies

Unity refs (on `EnemyRange`)
- `gunPoint`, `bulletPrefab`
- `fireRate`, `bulletSpeed`
- `bulletsToShoot`, `weaponCooldownTime`

## 🔄 Execution Flow
1. Spawn
   - `EnemyRange.Awake()` builds states
   - `Start()` initializes FSM to `IdleState_Range` and calls `visuals.SetupLook()`
2. Patrol
   - Idle → Move loop (patrol)
3. Battle entry
   - `Enemy.EnterBattleMode()` then `EnemyRange.EnterBattleMode()` swaps state to `BattleState_Range`
4. Battle
   - State faces player via `enemy.FacePlayer()`
   - Shoots if `Time.time > lastTimeShot + 1/fireRate`
   - After `bulletsToShoot` shots, waits until `weaponCooldownTime` passes, then resets counter

## 🔗 Dependencies
**Depends On**
- `Enemy` base
- `EnemyStateMachine`
- `ObjectPool` (for `EnemyBullet`)

**Used By**
- Player combat loop (provides ranged threat)

## ⚠ Constraints & Assumptions
- `EnemyRange.FireSingleBullet()` uses `ObjectPool.Instance.GetObject(bulletPrefab)` and expects `EnemyBullet.BulletSetup()` to reset runtime state.
- Bullet mass is derived from `bulletSpeed` (`rb.mass = 20 / bulletSpeed`) for tuning.

## 📈 Scalability & Extensibility
- Add positioning states (keep distance / strafe / retreat) without changing the shared FSM.
- Add weapon types by creating additional ranged weapon models and drive selection through `EnemyVisuals` (future).

## ✅ Development Status
Prototype

## 📝 Notes
- This archetype is expected to evolve significantly (Devlog 10 lists ranged as the next major milestone).
