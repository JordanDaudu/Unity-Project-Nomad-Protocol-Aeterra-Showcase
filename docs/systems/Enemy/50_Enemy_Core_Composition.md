---
title: "Enemy Core Composition"
summary: "Base Enemy architecture shared by melee and ranged enemies: health, NavMesh, perception + target memory, battle-mode trigger, pooling reset, and animation-event relays."
order: 50
status: "In Development"
tags: ["Enemy", "AI", "Combat", "Pooling", "Perception"]
last_updated: "2026-03-14"
---

## 🧭 Overview
`Enemy` is the shared base class for all enemy archetypes (currently: `EnemyMelee`, `EnemyRange`).

It provides shared infrastructure:
- Health + battle-mode lifecycle (`EnterBattleMode()` / `ExitBattleMode()`)
- **Perception + target memory** via the required `EnemyPerception` component
- Navigation helpers (`NavMeshAgent`, steering/player facing helpers)
- Animation-event relays (states receive `AnimationTrigger()` / `AbilityTrigger()` through `EnemyAnimationEvents`)
- Pool-safe reset contract (`IPoolable` → `OnSpawnedFromPool()`)
- Death effect composition hooks (`EnemyRagdoll`, `EnemyDeathDissolve`)

## 🎯 Purpose
Keep common enemy concerns in one place so each archetype can focus on its *behavior loop* (states + actions) without duplicating setup/reset logic.

## 🧠 Design Philosophy
- **Inheritance for shared infrastructure**: `Enemy` owns references + reset contract; archetypes extend.
- **State-driven behavior**: `Enemy` does not implement an FSM; archetypes do.
- **Perception as a shared layer**: visibility + memory is handled by `EnemyPerception`, keeping AI decisions consistent across types.
- **Animation-driven timing**: gameplay-critical timing is triggered via animation events instead of hard-coded delays.

## 📦 Core Responsibilities
**Does**
- Track health (`maxHealth`, `currentHealth`) and decrement on `GetHit()`
- Tick perception each frame (`perception.TickPerception(inBattleMode)`)
- Decide when to enter battle mode using **visibility-first** logic:
   - `Enemy.ShouldEnterBattleMode()` checks `EnemyPerception.IsTargetVisible` when available
- Provide shared helper queries used by states:
   - `CanSeePlayer()`
   - `HasRecentTargetKnowledge()`
   - `GetKnownPlayerPosition()`
- Refresh target memory when hit (`GetHit()` calls `perception.RegisterTargetKnowledge(...)`)
- Provide movement helpers (`StopAgentImmediately()`, `FaceTarget()`, `FaceSteeringTarget()`)

**Does NOT**
- Define archetype-specific decisions (melee vs ranged tactics live in states)
- Implement cover scoring (delegated to `EnemyCoverController` by ranged archetype)
- Implement projectile logic (player bullets in `Bullet`, ranged enemy bullets in `EnemyBullet`)

## 🧱 Key Components
Classes
- `Enemy`
   - Shared runtime state + perception integration
- `EnemyPerception`
   - Visibility, FOV rules, and last-seen memory
- `EnemyRagdoll`, `EnemyDeathDissolve`
   - Death presentation pipeline
- `EnemyAnimationEvents`
   - Animation-event bridge into the active `EnemyState`

Interfaces / Data
- `IPoolable`
   - Standard pool reset hook (`OnSpawnedFromPool()`)

## 🔄 Execution Flow
1. `Start()`
   - Initializes patrol points
   - Configures perception target with `perception.SetTarget(player)`

2. `Update()`
   - Ticks perception every frame
   - If `ShouldEnterBattleMode()` returns true → calls `EnterBattleMode()`
   - (Archetypes tick their own state machine after base update)

3. `GetHit()`
   - Refreshes perception knowledge (last known threat position)
   - Forces battle mode on hit
   - Decrements health

4. Pool reuse
   - `OnSpawnedFromPool()` restores baseline state and resets perception via `EnemyPerception.ResetPerception()` (through `ResetEnemyForReuse()`)

## 🔗 Dependencies
Depends On
- Unity: `NavMeshAgent`, `Animator`, physics
- `EnemyPerception` (required component)

Used By
- `EnemyMelee`, `EnemyRange`
- `Bullet` / `EnemyBullet` (calls `GetHit()`, death impact hook)

## ⚠ Constraints & Assumptions
- Battle-mode entry is visibility-driven when perception exists; if perception is missing (shouldn’t happen due to `RequireComponent`), fallback is distance check.
- `GetHit()` currently decrements health by 1; damage scaling is not implemented here.
- Perception memory duration is configured in `EnemyPerception`.

## 📈 Scalability & Extensibility
- New archetypes can reuse the same perception + battle-mode contract without duplicating detection logic.
- Additional “knowledge sources” (sound, alerts, squad comms) can integrate via `EnemyPerception.RegisterTargetKnowledge(position)`.

## ✅ Development Status
In Development

## 📝 Notes
See `57_Enemy_Perception_System.md` for the full visibility + memory model.