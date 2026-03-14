---
title: "Enemy State Machine"
summary: "Reusable finite state machine used by enemies. States are pure behavior units; the machine owns the active state lifecycle."
order: 51
status: "In Development"
tags: ["Enemy", "AI", "State Machine"]
last_updated: "2026-03-14"
---

## 🧭 Overview
The project uses a lightweight FSM (`EnemyStateMachine`) where:
- Only one `EnemyState` is active at a time.
- States are responsible for their own entry/exit behavior.
- Transition logic stays explicit and readable.

This is used by both:
- `EnemyMelee`
- `EnemyRange`

## 🎯 Purpose
Provide a scalable architecture for enemy behavior without massive monolithic scripts:
- Add new behaviors by creating new states
- Keep each state focused (high cohesion)
- Avoid spaghetti branching inside a single Update loop

## 🧠 Design Philosophy
- **States are execution units**: each state does one job.
- **Transitions are explicit**: states request transitions; the machine performs them.
- **Enemy owns the machine**: archetypes construct their own states and decide how they connect.

## 📦 Core Responsibilities
**EnemyStateMachine**
- Holds `currentState`
- Calls:
  - `Enter()` once on transition
  - `Update()` each frame
  - `Exit()` once on transition
- Exposes `ChangeState(newState)`

**EnemyState**
- Defines the behavior contract:
  - `Enter()`
  - `Update()`
  - `Exit()`
  - `AnimationTrigger()` (optional)
  - `AbilityTrigger()` (optional)

## ⏱ Animation Timing Hooks
States can receive animation events through `EnemyAnimationEvents`:
- `AnimationTrigger()` (generic timing)
- `AbilityTrigger()` (ability-specific timing)

Ranged enemies use `AbilityTrigger()` to spawn grenades during the throw animation (`ThrowGrenadeState_Range`).

## 🔄 Execution Flow
1. Archetype constructs states (usually in `Awake()`).
2. Archetype starts the machine in an initial state (`IdleState_*`) in `Start()`.
3. Each frame:
  - Archetype calls `stateMachine.Update()`
4. When a transition is needed:
  - State calls `stateMachine.ChangeState(...)`
5. Machine handles:
  - `currentState.Exit()`
  - swap
  - `newState.Enter()`

## 🔗 Dependencies
Depends On
- `Enemy` archetypes to own and tick the machine

Used By
- `EnemyMelee`, `EnemyRange`

## ⚠ Constraints & Assumptions
- States should not allocate or do expensive work per frame.
- Expensive searches (cover scans, physics queries) should be throttled or cached.

## ✅ Development Status
In Development