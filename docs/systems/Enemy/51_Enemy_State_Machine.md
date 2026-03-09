---
title: "Enemy State Machine"
summary: "Reusable finite state machine used by enemies. States control decisions, animation sync, and ability timing."
order: 51
status: "Stable"
tags: ["Enemy", "AI", "FSM"]
last_updated: "2026-03-05"
---

## 🧭 Overview
Enemies run their behavior through a finite state machine (FSM):
- `EnemyStateMachine` holds the active state
- `EnemyState` defines a shared lifecycle and timing hooks
- Concrete states implement behavior loops (idle/patrol/chase/attack/battle/dead/etc.)

Melee and ranged archetypes both use the same FSM foundation.

## 🎯 Purpose
Keep AI logic modular and readable:
- Add/replace behavior by adding a state class
- Keep transitions explicit
- Provide consistent hooks for animation-driven timing

## 🧠 Design Philosophy
- **State-driven decisions**: avoid giant `Update()` condition chains.
- **Animation as authority**: critical moments (attack hit frames, ability release frames) are synced via events.
- **Archetype-owned FSM**: each enemy instance creates its own `EnemyStateMachine`.

## 📦 Core Responsibilities
**Does**
- Initialize a starting state (`Initialize()`)
- Switch states (`ChangeState()`)
- Provide a standard lifecycle to states (`Enter()`, `Update()`, `Exit()`)
- Support animation timing callbacks (`AnimationTrigger()`, `AbilityTrigger()`)

**Does NOT**
- Decide *which* states exist (archetypes construct their states)
- Handle navigation/physics directly (states call into `Enemy`/archetype)

## 🧱 Key Components
Classes
- `EnemyStateMachine` (`code/Enemy/StateMachine/EnemyStateMachine.cs`)
  - Holds `currentState`
  - `Initialize(state)` and `ChangeState(state)`
- `EnemyState` (`code/Enemy/StateMachine/EnemyState.cs`)
  - Base class for states
  - Stores references to `Enemy`, `EnemyStateMachine`, animator bool name
  - Exposes `AnimationTrigger()` and `AbilityTrigger()` hooks

## 🔄 Execution Flow
1. Archetype `Awake()` constructs states
2. Archetype `Start()` calls `stateMachine.Initialize(startState)`
3. Archetype `Update()` calls `stateMachine.currentState.Update()`
4. Animation events call `Enemy.AnimationTrigger()` / `Enemy.AbilityTrigger()`
   - Enemy forwards to `stateMachine.currentState.AnimationTrigger()` / `AbilityTrigger()`
5. State calls `stateMachine.ChangeState(nextState)` as needed

## 🔗 Dependencies
**Depends On**
- `Enemy` (base archetype)
- Unity Animator (state bool parameter convention)

**Used By**
- Melee states (`code/Enemy/EnemyMelee/*State_Melee.cs`)
- Ranged states (`code/Enemy/EnemyRange/*State_Range.cs`)

## ⚠ Constraints & Assumptions
- The animator bool name is used to switch animation groups per-state (convention-driven).
- Archetypes are responsible for ticking the FSM each frame.

## 📈 Scalability & Extensibility
- Add new timing hooks by extending the base `EnemyState` interface (e.g., `OnHitFrame()` if needed).
- Add hierarchical states later if AI grows (not required yet).

## ✅ Development Status
Stable

## 📝 Notes
- This FSM intentionally stays lightweight; behavior complexity lives inside states.
