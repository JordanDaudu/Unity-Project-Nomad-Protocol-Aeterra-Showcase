---
title: "Enemy Range Perks & Abilities"
summary: "Perk flags on EnemyRange that change tactical behavior: CoverPerk (take cover/reposition), UnstoppablePerk (advance bias), and GrenadePerk (grenade ability)."
order: 59
status: "In Development"
tags: ["Enemy", "AI", "Ranged", "Perks", "Abilities", "Grenades", "Cover"]
last_updated: "2026-03-14"
---

## 🧭 Overview
`EnemyRange` supports small behavior variations via perk enums defined on the class:

- `CoverPerk`: whether the enemy can take cover and whether it can reposition later
- `UnstoppablePerk`: modifies advance behavior / cadence to create a “push-forward” threat
- `GrenadePerk`: enables grenade throws as an ability state

These perks are meant to create multiple ranged “personalities” without duplicating the entire AI architecture.

## 🎯 Purpose
- Increase encounter variety with small, composable toggles.
- Keep ranged AI scalable: add perks rather than copy-pasting a new enemy class.

## 🧠 Design Philosophy
- **Perks are simple gates**: most perks only enable/disable transitions or change a small set of tunables.
- **States remain the execution units**: perks influence which states are entered and what they do, not the other way around.
- **Data-driven where possible**: weapon behavior uses `EnemyRangeWeaponData`; perks stay as lightweight enums.

## 🧱 Perk Details

### 🛡️ CoverPerk
Enum: `CoverPerk { None, TakeCoverOnce, Reposition }`

Where it applies
- `EnemyRange.EnterBattleMode()`
    - if `coverPerk != None` → enters `RunToCoverState_Range`
    - if `coverPerk == None` → enters `BattleState_Range`
- `EnemyRange.CanRepositionCover`
    - true only when `coverPerk == Reposition`

Practical behavior
- **None**
    - Never reserves cover; fights/advances directly.
- **TakeCoverOnce**
    - Takes cover on battle entry, then stays (no reposition checks).
- **Reposition**
    - Takes cover on entry and may occasionally swap to better cover (via `EnemyCoverController.TryReserveBetterCover`).

Related system
- See `58_Cover_System.md`.

### 🧟 UnstoppablePerk
Enum: `UnstoppablePerk { None, Unstoppable }`

Where it applies
- `EnemyRange.InitializeSpeciality()`
    - sets `advanceSpeed` to `UnstoppableAdvanceSpeed`
    - sets Animator float `AdvanceAnimIndex` to select the appropriate animation set
- `BattleState_Range`
    - contains an “unstoppable walk” branch that can transition into `AdvanceToPlayer_Range` based on weapon cycle + distance

Important note (current implementation)
- Unstoppable does **not** automatically disable cover; cover usage is still controlled by `CoverPerk`.

### 💣 GrenadePerk
Enum: `GrenadePerk { None, CanThrowGrenade }`

Where it applies
- `BattleState_Range.Update()`
    - if `EnemyRange.CanThrowGrenade()` returns true → transitions into `ThrowGrenadeState_Range`
- `ThrowGrenadeState_Range.AbilityTrigger()`
    - calls `EnemyRange.ThrowGrenade()`

Gating rules (`EnemyRange.CanThrowGrenade()`)
- Requires the perk
- Requires recent target knowledge (`HasRecentTargetKnowledge()`)
- Requires sight-loss time within the `grenadeKnowledgeWindow` (uses `EnemyPerception.TimeSinceLastSeen`)
- Requires distance within `[grenadeMinThrowDistance, grenadeMaxThrowDistance]`
- Requires cooldown: `Time.time >= lastTimeThrewGrenade + grenadeCooldown`

Spawn + timing
- Grenade is pooled (`ObjectPool.Instance.GetObject(grenadePrefab, grenadeStartPoint)`)
- `EnemyGrenade.SetupGrenade(target, timeToTarget, explosionTimer, impactPower)` configures:
    - flight arc time
    - detonation timer

Death edge case
- `DeadState_Range` forces a throw if the enemy dies mid-throw so the ability doesn’t “vanish”.

## 🔗 Dependencies
Depends On
- `EnemyRange` (perk fields + logic)
- `EnemyCoverController` (cover perk)
- `EnemyPerception` (grenade knowledge window)
- `EnemyGrenade` + `ObjectPool` (grenade spawn)

## ✅ Development Status
In Development