---
title: "Cover System"
summary: "Reusable tactical cover system: Cover objects own CoverPoints; EnemyCoverController searches, scores, reserves, and releases points (supports repositioning)."
order: 58
status: "In Development"
tags: ["Enemy", "AI", "Cover", "Navigation", "Tactics"]
last_updated: "2026-03-14"
---

## 🧭 Overview
The cover system provides **reservable combat positions** for tactical enemies (currently used by `EnemyRange`).

Core idea:
- The world contains `Cover` objects.
- Each `Cover` owns one or more `CoverPoint`s (slots).
- Enemies reserve **a point**, not the whole cover object.

This enables multiple enemies to use the same cover object without overlapping, while still reasoning about which *side* is safe relative to the threat.

## 🎯 Purpose
- Make ranged combat feel tactical and stable (commit to a position, don’t jitter).
- Avoid enemies “stacking” into the same cover spot.
- Keep cover evaluation reusable and independent from any specific enemy archetype.

## 🧠 Design Philosophy
- **Points, not objects**: reserving a single slot avoids overly coarse ownership.
- **Reservation is explicit**: the controller claims a point and must release it.
- **Repositioning is throttled**: the system can look for “better” cover occasionally, not every frame.
- **Scoring is tunable**: distance, side safety, and LOS-blocking weights live in `EnemyCoverController`.

## 📦 Core Responsibilities
**Does**
- Discover nearby cover candidates (`Physics.OverlapSphere` with `coverLayerMask`)
- Score all available `CoverPoint`s and select the best
- Reserve one point per enemy (`CoverPoint` tracks its current occupant)
- Release reserved points on disable/destroy or explicit state changes
- Optionally look for better cover while already in combat (reposition mode)

**Does NOT**
- Move the enemy (movement is performed by states like `RunToCoverState_Range`)
- Decide *when* to take cover (archetype states decide; ranged uses `CoverPerk`)
- Handle shooting/aiming logic

## 🧱 Key Components
World objects
- `Cover` (`code/Enemy/CoverSystem/Cover.cs`)
    - Owns / generates `CoverPoint`s
    - Can auto-generate default points if none exist (`autoGeneratePoints`)
- `CoverPoint` (`code/Enemy/CoverSystem/CoverPoint.cs`)
    - Represents a single reservable slot around a cover object
    - Knows its side (`CoverPointSide`) and outward direction for scoring
    - Tracks occupancy (`IsOccupied`, `Claim(...)`, `Release(...)`)

Enemy-side controller
- `EnemyCoverController` (`code/Enemy/CoverSystem/EnemyCoverController.cs`)
    - Searches and caches nearby covers
    - Builds scored candidates and selects best point
    - Exposes APIs used by enemies:
        - `AttemptToFindCover(threat)` (transform convenience)
        - `TryReserveBestCover(threat, out point)`
        - `TryReserveBetterCover(threat, out point)`
        - `ReleaseCurrentCover()`

## 🔄 Execution Flow
1. **Scene setup**
    - Level contains `Cover` objects with colliders in `coverLayerMask`.
    - `Cover.Awake()` caches existing child points or auto-generates them.

2. **Enemy requests cover**
    - Ranged enemy calls:
        - `EnemyRange.AttemptToFindCover()` → `EnemyCoverController.AttemptToFindCover(player)`
    - Controller refreshes nearby covers (throttled by `coverRefreshInterval`)
    - Best point is selected and reserved.

3. **Enemy moves to point**
    - `RunToCoverState_Range` sets agent destination to reserved point position.
    - On arrival, enemy transitions to `BattleState_Range`.

4. **Repositioning (optional)**
    - If `CoverPerk == Reposition`, ranged battle periodically calls:
        - `TryReserveBetterCover(...)`
    - Controller will only switch if:
        - enough time passed (`repositionCheckInterval`)
        - score improves by at least `minimumScoreImprovementToSwitch`
        - new point is within `maxRepositionDistance`

5. **Release**
    - Enemy releases cover when:
        - advancing (`AdvanceToPlayer_Range.Enter()` calls `ReleaseReservedCover()`)
        - exiting battle
        - dying / disabling / destroying

## 🔗 Dependencies
Depends On
- Unity Physics (`OverlapSphere`, LOS raycasts)
- Proper layer configuration:
    - `coverLayerMask` for cover objects
    - `coverBlockingMask` for LOS-block evaluation

Used By
- `EnemyRange` (via wrapper methods: `AttemptToFindCover`, `TryReserveBetterCover`, `ReleaseReservedCover`)
- Future tactical archetypes (intended reuse)

## ⚠ Constraints & Assumptions
- Cover scoring is geometric; it does not consider navmesh cost/path difficulty beyond distance.
- Reservation is per-point; designers must place/generate enough points to support expected enemy counts.
- If `requireLineOfSightBlock` is enabled, the controller may reject many points depending on `coverBlockingMask`.

## 📈 Scalability & Extensibility
- Add new scoring terms (height advantage, angle to threat, etc.) inside `EnemyCoverController` without touching enemy states.
- Add more points per cover object for richer multi-enemy usage.

## ✅ Development Status
In Development