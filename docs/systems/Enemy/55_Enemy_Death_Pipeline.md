---
title: "Enemy Death Pipeline"
summary: "Death sequence combining NavMesh/Animator disable, ragdoll physics, hit impulse, dissolve visuals, interaction shutdown, and pool-safe reset (supports melee + ranged edge cases)."
order: 55
status: "In Development"
tags: ["Enemy", "Death", "Ragdoll", "VFX", "Pooling"]
last_updated: "2026-03-14"
---

## 🧭 Overview
Enemy death is treated as a controlled sequence:
- Disable navigation + animation
- Enable ragdoll physics
- Apply hit impulse at impact point
- Run dissolve shader sequence
- Disable colliders/interactions after a short delay (performance + stability)
- Reset to alive state when reused from the pool

This is implemented via composed components + an FSM “dead state”, rather than monolithic logic.

## 🎯 Purpose
Provide strong combat feedback (impact + visuals) while remaining stable under pooling.

## 🧠 Design Philosophy
- **Death is a pipeline**: predictable ordering prevents partial/broken states.
- **Pool-safe reset**: all toggles/material swaps must be reversible on respawn.
- **Separate visuals from rules**: `EnemyDeathDissolve` owns shader control; states own timing and cleanup.
- **Handle archetype edge cases**: ranged grenade throw must not “vanish” if the enemy dies mid-animation.

## 📦 Core Responsibilities
**Does**
- Transition into a dead state when health reaches 0
- Disable `NavMeshAgent` and `Animator` so ragdoll owns the body
- Enable ragdoll and apply delayed hit impulse (`Enemy.DeathImpact`)
- Trigger dissolve sequence (`EnemyDeathDissolve.PlayDeathDissolve()`)
- Disable ragdoll colliders after a short delay (avoids ongoing bullet/physics interactions)
- Provide pool reset hooks to restore the enemy to a clean alive baseline

**Does NOT**
- Decide when an enemy *should* die (health logic decides)
- Destroy objects (pooling is preferred)

## 🧱 Key Components
States
- `DeadState_Melee` / `DeadState_Range`
    - Archetype-specific entry logic and cleanup timing

Presentation components
- `EnemyRagdoll`
    - Toggles rigidbodies/colliders/animation control
- `EnemyDeathDissolve`
    - Builds dissolve target list and drives dissolve shader parameters

Shared helper
- `Enemy.DeathImpact(...)`
    - Applies a delayed `AddForceAtPosition` so ragdoll settles before impulse

## 🔄 Execution Flow
1. **Hit**
    - `Bullet` hits enemy → calls `Enemy.GetHit()` (and shield logic if present)

2. **Health reaches 0**
    - Archetype transitions its FSM into `DeadState_*`

3. **Dead state enter**
    - Disable `Animator` + `NavMeshAgent`
    - Enable ragdoll
    - Start dissolve effect
    - Start a timer to disable ragdoll colliders after ~1.5s

4. **Archetype-specific edge cases**
    - **Ranged**: `DeadState_Range` forces `EnemyRange.ThrowGrenade()` if the enemy died mid-throw
        - This prevents the grenade animation from consuming the ability without spawning the grenade.

5. **Pool reuse**
    - `OnSpawnedFromPool()` restores:
        - health
        - battle mode flags
        - agent + animator enabled state
        - ragdoll colliders off / kinematic
        - dissolve materials reset

## 🔗 Dependencies
Depends On
- Unity physics + Animator + NavMesh
- Correct dissolve shader setup on all dissolve targets

Used By
- `EnemyMelee`, `EnemyRange`

## ⚠ Constraints & Assumptions
- Dissolve only affects renderers using a compatible shader (missing dissolve shader will be logged by `EnemyDeathDissolve`).
- Ragdoll colliders are disabled after death to prevent extra hits/forces while pooled objects remain in the world.

## 📈 Scalability & Extensibility
- Add new archetype-specific death edge cases inside the archetype’s dead state, without changing ragdoll/dissolve components.
- Death visuals can evolve independently (e.g., different dissolve profiles per enemy type).

## ✅ Development Status
In Development