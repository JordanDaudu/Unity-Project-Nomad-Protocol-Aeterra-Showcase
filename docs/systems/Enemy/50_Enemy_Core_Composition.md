---
title: "Enemy Core Composition"
summary: "Base Enemy architecture shared by melee and ranged enemies: health, NavMesh, battle-mode trigger, pooling reset, and animation-event relays."
order: 50
status: "In Development"
tags: ["Enemy", "AI", "Combat", "Pooling"]
last_updated: "2026-03-05"
---

## 🧭 Overview
`Enemy` is the base class for all enemy archetypes (currently: `EnemyMelee`, `EnemyRange`).

It provides shared infrastructure:
- Health + battle-mode detection
- Navigation helpers (`NavMeshAgent`, steering/player facing)
- FSM ownership (`EnemyStateMachine`) and animation-event relays
- Pool-safe reset contract (`IPoolable` → `OnSpawnedFromPool()`)
- Death effect composition hooks (`EnemyRagdoll`, `EnemyDeathDissolve`)

## 🎯 Purpose
Keep common enemy concerns in one place so each archetype can focus on its *behavior loop* (states + actions) without duplicating setup/reset logic.

## 🧠 Design Philosophy
- **Inheritance for shared infrastructure**: `Enemy` owns references + reset contract; archetypes extend.
- **State-driven behavior**: `Enemy` does not implement decisions; the FSM does.
- **Animation-driven timing**: gameplay-critical timing is triggered via animation events and forwarded to the active state.
- **Pool-safe by default**: enemies are expected to be reused via pooling, so reset is explicit.

## 📦 Core Responsibilities
**Does**
- Cache common components (`Animator`, `NavMeshAgent`, `EnemyRagdoll`, `EnemyDeathDissolve`, `EnemyVisuals`).
- Track health (`maxHealth`, `currentHealth`) and `inBattleMode`.
- Detect aggression via `ShouldEnterBattleMode()` and call `EnterBattleMode()` once.
- Provide facing helpers (`FacePlayer()`, `FaceSteeringTarget()`) and patrol target selection.
- Relay animation hooks to the current state: `AnimationTrigger()` and `AbilityTrigger()`.
- Reset the enemy for reuse through `IPoolable` (`OnSpawnedFromPool()` → `ResetEnemyForReuse()`).

**Does NOT**
- Define AI decisions or state transitions (owned by `EnemyStateMachine` + concrete `EnemyState` subclasses).
- Implement concrete attacks/shooting logic (archetype-specific).

## 🧱 Key Components
Classes
- `Enemy` (`code/Enemy/Enemy.cs`)
- `EnemyStateMachine` (`code/Enemy/StateMachine/EnemyStateMachine.cs`)
- `EnemyState` (`code/Enemy/StateMachine/EnemyState.cs`)

Composition (attached on prefab)
- `NavMeshAgent`
- `Animator` (in child)
- `EnemyRagdoll`
- `EnemyDeathDissolve`
- `EnemyVisuals`

Interfaces / Data
- `IPoolable` (`code/Object Pool/IPoolable.cs`)

## 🔄 Execution Flow
1. `Awake()`
   - Initializes `currentHealth`
   - Creates a per-enemy `EnemyStateMachine`
   - Caches agent/anim/player/ragdoll/dissolve/visuals
2. `Update()`
   - Runs battle-mode enter check (`ShouldEnterBattleMode()`)
   - (FSM tick is done by subclasses)
3. On hit (`GetHit()`)
   - Enters battle mode
   - Decrements health
4. Death impact (`DeathImpact()`)
   - Applies delayed force to ragdoll rigidbody at hit point (timed to allow ragdoll enable)
5. Pool reuse (`OnSpawnedFromPool()`)
   - Resets health + flags
   - Re-enables animator/agent, resets path
   - Restores ragdoll/colliders
   - Restores dissolve materials
   - Calls `OnResetEnemyStateMachineForReuse()` for archetype-specific FSM reset

## 🔗 Dependencies
**Depends On**
- Unity: `NavMeshAgent`, `Animator`, `Coroutine`
- `EnemyStateMachine` / `EnemyState`
- `EnemyRagdoll`, `EnemyDeathDissolve`, `EnemyVisuals`

**Used By**
- `EnemyMelee`, `EnemyRange`
- `EnemyAnimationEvents` (forwards animation triggers)
- `Bullet` (calls `GetHit()` + `DeathImpact()`)

## ⚠ Constraints & Assumptions
- `player` is currently resolved via `GameObject.Find("Player")`.
- `ResetEnemyForReuse()` assumes the agent is on a valid NavMesh; it guards with `agent.isOnNavMesh` before `ResetPath()`.

## 📈 Scalability & Extensibility
- Add new archetypes by inheriting from `Enemy` and building states (e.g., `EnemySniper`, `EnemySummoner`).
- Extend battle-mode logic to use perception/line-of-sight without changing the FSM contract.
- Replace `GameObject.Find` with a proper player reference provider when you introduce multiple scenes/spawn flows.

## ✅ Development Status
In Development

## 📝 Notes
- In docs, classes are referenced as `Enemy` and functions as `ResetEnemyForReuse()`.
