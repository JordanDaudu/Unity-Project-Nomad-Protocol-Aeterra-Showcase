---
title: "Enemy Death Pipeline"
summary: "Death sequence combining NavMesh disable, ragdoll physics, hit impulse, dissolve visuals, and pool-safe reset."
order: 55
status: "In Development"
tags: ["Enemy", "Death", "Ragdoll", "VFX", "Pooling"]
last_updated: "2026-03-05"
---

## 🧭 Overview
Enemy death is treated as a controlled sequence:
- Disable navigation + animation
- Enable ragdoll physics
- Apply hit impulse at impact point
- Run dissolve shader sequence
- Freeze/cleanup for performance
- Reset to alive state when reused from pool

This is implemented as composed components rather than monolithic logic.

## 🎯 Purpose
Provide strong combat feedback (impact + visuals) while remaining stable under pooling.

## 🧠 Design Philosophy
- **Death is a pipeline**: predictable order prevents partial/broken states.
- **Pool-safe reset**: all toggles/material swaps must be reversible.
- **Performance-aware**: dissolve via property blocks, pooled FX, and freezing ragdolls after a delay.

## 📦 Core Responsibilities
**Does**
- Provide a consistent death sequence for enemy archetypes
- Apply hit force to ragdoll rigidbody at contact point
- Swap materials to dissolve variants and animate dissolve value
- Provide `ResetForReuse()` so pooled enemies can respawn cleanly

**Does NOT**
- Decide *when* an enemy dies (health logic lives in `Enemy` + archetypes)

## 🧱 Key Components
Classes
- `EnemyRagdoll` (`code/Enemy/EnemyRagdoll.cs`)
  - Toggles rigidbodies/colliders
- `EnemyDeathDissolve` (`code/Enemy/EnemyDeathDissolve.cs`)
  - Builds dissolve targets, swaps materials, animates dissolve, resets materials
- `Enemy` (`code/Enemy/Enemy.cs`)
  - Disables agent/anim, triggers ragdoll/dissolve reset on pool reuse

Integration
- `Bullet` applies:
  - `Enemy.GetHit()`
  - `Enemy.DeathImpact(force, hitPoint, hitRigidBody)`

## 🔄 Execution Flow
1. Enemy reaches death condition (archetype/state)
2. Dead state begins
   - Disable `NavMeshAgent`
   - Disable animator
   - Enable ragdoll (`EnemyRagdoll.RagdollActive(true)`)
3. Bullet impact
   - Bullet passes impulse direction/magnitude
   - Enemy applies force after a short delay (`DeathImpact()` coroutine)
4. Dissolve
   - `EnemyDeathDissolve` swaps to dissolve-compatible materials
   - Dissolve value animates over time
5. Post-death cleanup
   - Ragdoll can be frozen / colliders disabled to reduce physics cost
6. Pool reuse
   - `Enemy.OnSpawnedFromPool()` calls `ResetEnemyForReuse()`
   - Restores components
   - Calls `EnemyDeathDissolve.ResetForReuse()`

## 🔗 Dependencies
**Depends On**
- Unity physics (`Rigidbody`, colliders)
- Material/shader properties (dissolve support)

**Used By**
- Melee + ranged archetypes (death state)

## ⚠ Constraints & Assumptions
- Dissolve only works on renderers/materials that expose the expected dissolve property.
- Bullet hit uses `collision.collider.attachedRigidbody` which must exist for ragdoll bones.

## 📈 Scalability & Extensibility
- Add different death VFX sequences per archetype by extending dead states while keeping reset contract.
- Add pooling-friendly blood decals/impact FX (already compatible with pooled approach).

## ✅ Development Status
In Development

## 📝 Notes
- Devlog 09 introduced the ragdoll → dissolve pipeline and pool-safe reset goals.
