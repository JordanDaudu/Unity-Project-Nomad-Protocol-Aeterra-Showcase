---
title: "Enemy Ranged AI"
summary: "Tactical ranged archetype with perception + target memory, cover reservation, smart aiming, grenade ability, and state-driven combat."
order: 53
status: "In Development"
tags: ["Enemy", "AI", "Ranged", "Perception", "Cover", "Projectiles", "Grenades"]
last_updated: "2026-03-14"
---

## 🧭 Overview
`EnemyRange` is the tactical ranged enemy archetype. It extends `Enemy` and runs its own `EnemyStateMachine` with ranged-specific states:

- `IdleState_Range`
- `MoveState_Range`
- `RunToCoverState_Range`
- `BattleState_Range`
- `AdvanceToPlayer_Range`
- `ThrowGrenadeState_Range`
- `DeadState_Range`

Unlike melee, ranged combat is built around **target perception + memory**, **position commitment (cover / advance)**, and **aim-readability** (smart aim lock before firing).

## 🎯 Purpose
Provide a ranged archetype that feels meaningfully different from melee by introducing:
- A reusable perception layer (`EnemyPerception`) so enemies react based on *visibility + memory*, not only distance
- A reusable cover controller (`EnemyCoverController`) so enemies can reserve and fight from cover points
- A state-driven ranged loop that supports weapon cadence, repositioning, and grenade ability timing

## 🧠 Design Philosophy
- **Ranged is not “melee but shooting”**: it has a different decision loop and commitment to positions.
- **Reuse core infrastructure**: `Enemy` owns shared references + battle-mode contract; ranged behavior lives in states.
- **Modular tactical features**: cover evaluation is separated into `EnemyCoverController`, not embedded in `EnemyRange`.
- **Animation-friendly abilities**: grenade throw spawns via `AbilityTrigger()` (animation event timing).

## 📦 Core Responsibilities
**Does**
- Initialize and tick the ranged state machine (`EnemyStateMachine`)
- Use `EnemyPerception` to decide:
    - when to enter battle (`Enemy.ShouldEnterBattleMode()`)
    - when to exit battle (loss of target knowledge)
    - what position to reason about (`Enemy.GetKnownPlayerPosition()`)
- Select a ranged weapon preset (`EnemyRangeWeaponData`) and fire pooled `EnemyBullet` projectiles
- Coordinate tactical movement:
    - `RunToCoverState_Range` reserves + runs to cover
    - `AdvanceToPlayer_Range` pushes toward known target position when needed
- Optionally throw grenades when permitted by perk + cooldown + knowledge (`ThrowGrenadeState_Range` → `EnemyGrenade`)

**Does NOT**
- Score cover points directly (handled by `EnemyCoverController`)
- Store weapon behavior constants on the enemy (handled by `EnemyRangeWeaponData`)
- Apply player damage from enemy bullets (currently `EnemyBullet` logs a hit; gameplay damage is TODO)

## 🧱 Key Components
Classes
- `EnemyRange`
    - Holds perk toggles and tunables (cover/advance/grenade/aim)
    - Bridges ranged states to reusable systems (`EnemyPerception`, `EnemyCoverController`)
    - Spawns `EnemyBullet` and `EnemyGrenade` via `ObjectPool`
- `EnemyPerception`
    - Visibility + field-of-view rules + short-term target memory
- `EnemyCoverController`
    - Searches, scores, reserves, and releases `CoverPoint`s
- Ranged states
    - `BattleState_Range`: aim + fire cadence + (optional) reposition check + grenade gating
    - `RunToCoverState_Range`: reserve cover point and move to it
    - `AdvanceToPlayer_Range`: advance toward known target position when out of range/LOS
    - `ThrowGrenadeState_Range`: animation-driven grenade throw
    - `DeadState_Range`: ragdoll + dissolve + grenade edge-case resolution

Interfaces / Data
- `EnemyRangeWeaponData` (ScriptableObject)
    - Fire rate, bullets-per-attack roll, cooldown roll, bullet speed, spread
- `Cover`, `CoverPoint`
    - World cover objects and reservable tactical points

## 🔄 Execution Flow
1. **Start / Setup**
    - `EnemyRange.Start()` sets up:
        - aim transform baseline (stores original parent/local pos)
        - perception target (`perception.SetTarget(player, playerBody)`)
        - visuals (`EnemyVisuals.SetupLook()`)
        - weapon preset selection (`SetupWeapon()` picks from `availableWeaponData`)
        - optional cover cache refresh (`coverController.RefreshNearbyCovers(true)`)

2. **Detection → Enter Battle**
    - `Enemy.Update()` ticks perception (`perception.TickPerception(inBattleMode)`)
    - If `EnemyPerception.IsTargetVisible` becomes true and `inBattleMode` is false:
        - `EnemyRange.EnterBattleMode()` is called
        - If `CoverPerk != None` → transition to `RunToCoverState_Range`
        - Else → transition directly to `BattleState_Range`

3. **Run to Cover**
    - `RunToCoverState_Range.Enter()` calls `EnemyRange.AttemptToFindCover()`
    - If a point is reserved, the agent runs to it
    - On arrival, transitions to `BattleState_Range`
    - If cover can’t be found, falls back to `BattleState_Range`

4. **Battle (core loop)**
    - `BattleState_Range.Update()`:
        - Updates aim (`EnemyRange.UpdateAimPosition()`) and faces target
        - If target knowledge expires → `ExitBattleMode()` → `IdleState_Range`
        - If grenade is allowed now → `ThrowGrenadeState_Range`
        - If LOS is lost or target is out of combat range (and not unstoppable) → `AdvanceToPlayer_Range`
        - If repositioning is allowed and a better cover point is found → `RunToCoverState_Range`
        - Fires bullets when aim is “locked” long enough (`EnemyRange.CanFireAtPlayer()`)

5. **Advance**
    - `AdvanceToPlayer_Range` moves toward `Enemy.GetKnownPlayerPosition()`
    - Returns to `BattleState_Range` once the enemy can see the player and is within `advanceStoppingDistance`
    - If target knowledge expires, or last known position is reached without vision → exit battle and idle

6. **Grenade Throw**
    - `ThrowGrenadeState_Range` disables IK and swaps visuals (secondary weapon + grenade model)
    - Grenade spawns on animation event (`AbilityTrigger()` → `EnemyRange.ThrowGrenade()`)

7. **Death**
    - `EnemyRange.GetHit()` transitions to `DeadState_Range` when health reaches 0
    - `DeadState_Range`:
        - resolves grenade “dies mid-throw” edge case
        - disables Animator + NavMeshAgent
        - enables ragdoll + dissolve sequence

## 🔗 Dependencies
Depends On
- `Enemy` (base: health, battle-mode, perception helpers)
- `EnemyPerception` (required by `Enemy`)
- `EnemyCoverController`, `Cover`, `CoverPoint` (when `CoverPerk != None`)
- `EnemyVisuals` (IK toggles + weapon/grenade model swaps)
- `ObjectPool` (enemy bullets, grenades, explosion FX)

Used By
- Encounter design / spawners (spawns ranged archetype)
- `Bullet` / `EnemyBullet` (combat feedback pipeline)

## ⚠ Constraints & Assumptions
- Ranged state decisions depend on `HasRecentTargetKnowledge()`; memory duration is controlled by `EnemyPerception.memoryDuration`.
- `UnstoppablePerk` currently influences advance speed + battle cadence behavior; cover usage is still controlled separately via `CoverPerk`.
- Grenade distance checks currently use the player’s *current* position for min/max distance gating (see `EnemyRange.CanThrowGrenade()`).
- Enemy bullets currently do not apply damage to the player (logged only).

## 📈 Scalability & Extensibility
- Add new ranged behaviors by introducing new states and perk flags without changing `Enemy` base.
- Cover scoring rules are encapsulated in `EnemyCoverController` and can be tuned without touching ranged states.
- Perception can be reused by melee and future archetypes by calling `GetKnownPlayerPosition()` and respecting `HasRecentTargetKnowledge()`.

## ✅ Development Status
In Development (playable + expanding tactical behaviors)

## 📝 Notes
- Related systems:
    - `Enemy Perception System` (see `57_Enemy_Perception_System.md`)
    - `Cover System` (see `58_Cover_System.md`)
    - `Enemy Range Perks & Abilities` (see `59_Enemy_Range_Perks_and_Abilities.md`)