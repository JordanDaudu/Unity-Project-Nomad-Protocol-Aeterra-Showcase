---
title: "Enemy Melee AI"
summary: "NavMesh-driven patrol/chase + animation-driven melee attacks, recovery decisions, variant abilities, and death sequencing (with shared perception + target memory)."
order: 52
status: "In Development"
tags: ["Enemy", "AI", "Melee", "NavMesh", "Animation", "Perception"]
last_updated: "2026-03-14"
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
- **Shared perception layer**: battle entry is driven by `EnemyPerception` visibility, and target memory refreshes when hit.

## 📦 Core Responsibilities
**Does**
- Build and tick melee states (`EnemyStateMachine`)
- Patrol between points (Idle/Move)
- Enter battle mode when the player is *seen* (via `EnemyPerception.IsTargetVisible`) or when hit
- Chase + attack using NavMesh + animation timing
- Route variant abilities through dedicated states (e.g., axe throw)
- Run the shared death pipeline (ragdoll + dissolve)

**Does NOT**
- Handle perception logic itself (delegated to `EnemyPerception`)
- Apply projectile damage (handled by projectiles / hit logic)
- Implement cover logic (ranged-only system currently)

## 🧱 Key Components
Classes
- `EnemyMelee`
    - Builds melee FSM and owns melee configuration (attack ranges, ability toggles)
- Melee states (selection varies by variant)
    - `ChaseState_Melee`, `AttackState_Melee`, `RecoveryState_Melee`, `AbilityState_Melee`, `DeadState_Melee`, etc.
- `EnemyPerception`
    - Shared visibility + memory component used by `Enemy` base (and therefore by melee)

Variant helpers
- `EnemyShield` (`code/Enemy/EnemyShield.cs`) for shield variants
- `EnemyAxe` (`code/Enemy/EnemyAxe.cs`) for axe throw ability

## 🔄 Execution Flow
1. Spawn
    - `EnemyMelee.Awake()` builds all melee states
    - `Start()` initializes FSM to `IdleState_Melee`
2. Patrol loop
    - Idle → Move between patrol points via agent
3. Battle-mode entry
    - `Enemy.Update()` ticks perception and calls `EnterBattleMode()` when `EnemyPerception.IsTargetVisible` becomes true
    - Being hit also refreshes target knowledge and forces battle (`Enemy.GetHit()`)
4. Combat loop
    - **Chase**: sets agent destination toward player (currently uses live `player.position`)
    - **Attack**: triggers animation; movement/rotation is controlled by animation events
    - **Recovery**: selects next action (chase/attack/ability)
    - **Ability**: executes variant ability (e.g., axe throw) on ability event timing
5. Death
    - Dead state disables agent/animator, enables ragdoll, starts dissolve, and schedules collider shutdown

## 🔗 Dependencies
Depends On
- `Enemy` base (health, perception integration, patrol, reset)
- Unity: `NavMeshAgent`, `Animator`
- `EnemyAnimationEvents` (timing)
- Variant components (`EnemyShield`, `EnemyAxe`) when enabled

Used By
- Encounter spawning
- Player bullet pipeline (`Bullet` → `Enemy.GetHit()`)

## ⚠ Constraints & Assumptions
- The melee chase state currently keeps steering toward the player’s live position; it does not yet fully commit to `GetKnownPlayerPosition()` (target memory usage is minimal for melee today).
- Perception memory is still used indirectly:
    - Battle entry is visibility-driven
    - Memory refresh occurs on `GetHit()`

## 📈 Scalability & Extensibility
- Add new melee variants by:
    - extending `EnemyVisuals` weapon model selection
    - adding a new ability state or recovery decision branch
    - keeping the same state machine foundation

## ✅ Development Status
In Development

## 📝 Notes
For the shared perception model, see `57_Enemy_Perception_System.md`.