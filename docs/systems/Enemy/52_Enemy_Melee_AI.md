---
title: "Enemy Melee AI"
summary: "NavMesh-driven patrol/chase + animation-driven melee attacks, recovery decisions, variant abilities, and death sequencing."
order: 52
status: "In Development"
tags: ["Enemy", "AI", "Melee", "NavMesh", "Animation"]
last_updated: "2026-03-05"
---

## 🧭 Overview
`EnemyMelee` is the first full combatant archetype. It uses:
- `NavMeshAgent` for patrol/chase
- A reusable FSM with melee-specific states
- Animation events for movement/rotation gating and ability timing
- Variant behaviors (Shield / Dodge / AxeThrow)
- A death pipeline (ragdoll + dissolve)

(See Devlog 09 for the development narrative.)

## 🎯 Purpose
Provide a scalable template for enemy combatants where:
- Navigation feels natural (patrol → chase)
- Attacks are synced to animation
- Adding variants mostly becomes *data + visuals*, not rewriting AI

## 🧠 Design Philosophy
- **Decision hub via Recovery state**: actions funnel through a predictable decision point.
- **Manual movement/rotation during attacks**: animation events explicitly enable/disable manual movement.
- **Variants as configuration**: same state machine, different flags/weapon models/attack data.

## 📦 Core Responsibilities
**Does**
- Construct melee states and tick the FSM
- Switch from patrol loop to combat loop when entering battle mode
- Select between chase/attack/ability based on range, cooldowns, and variant
- Use animation events to sync:
  - attack progress
  - ability release timing
  - manual movement/rotation windows

**Does NOT**
- Own weapon/variant visuals (delegates to `EnemyVisuals`)
- Implement pooling logic (delegates to `Enemy` reset contract + `ObjectPool`)

## 🧱 Key Components
Classes
- `EnemyMelee` (`code/Enemy/EnemyMelee/EnemyMelee.cs`)
- States:
  - `IdleState_Melee`
  - `MoveState_Melee`
  - `ChaseState_Melee`
  - `AttackState_Melee`
  - `RecoveryState_Melee`
  - `AbilityState_Melee`
  - `DeadState_Melee`

Supporting
- `EnemyAnimationEvents` (`code/Enemy/EnemyAnimationEvents.cs`) → calls `Enemy.AnimationTrigger()` and toggles manual movement/rotation.
- `EnemyShield` (`code/Enemy/EnemyShield.cs`) for shield variants.
- `EnemyAxe` (`code/Enemy/EnemyAxe.cs`) for axe throw ability.

## 🔄 Execution Flow
1. Spawn
   - `EnemyMelee.Awake()` builds all melee states
   - `Start()` initializes FSM to `IdleState_Melee`
2. Patrol loop
   - Idle → Move between patrol points via agent
3. Battle-mode entry
   - `Enemy.ShouldEnterBattleMode()` calls `EnterBattleMode()` once
   - Melee transitions into combat loop (typically via `RecoveryState_Melee` or `ChaseState_Melee` depending on implementation)
4. Combat loop
   - **Chase**: sets agent destination toward player with throttling
   - **Attack**: triggers animation; movement/rotation is controlled by animation event toggles
   - **Recovery**: selects next action (chase/attack/ability)
   - **Ability**: executes variant ability (e.g., axe throw) on ability event timing
5. Death
   - Dead state disables agent/animator, enables ragdoll, starts dissolve sequence

## 🔗 Dependencies
**Depends On**
- `Enemy` base (health, battle-mode, patrol, reset)
- Unity `NavMeshAgent`
- `EnemyStateMachine`
- `EnemyVisuals` and weapon model data overrides

**Used By**
- `Bullet` interacts with melee via `Enemy.GetHit()` + ragdoll impact and via `EnemyShield.ReduceDurability()`.
- `PlayerWeaponController` can trigger dodge roll via raycast along bullet path.

## ⚠ Constraints & Assumptions
- Animator parameters and animation event names are part of the runtime contract.
- Patrol points are assigned on the prefab; points are hidden at runtime.

## 📈 Scalability & Extensibility
- Add new melee variants by introducing new weapon model data (attack sets) and/or new states.
- Add perception (LOS/hearing) without changing the state interface.

## ✅ Development Status
In Development

## 📝 Notes
- The system is designed to expand beyond two enemy archetypes; keep additions state-driven.
