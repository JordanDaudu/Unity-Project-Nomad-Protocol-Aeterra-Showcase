---
title: "Enemy Perception System"
summary: "Shared visibility + target-memory layer (FOV, LOS checks, last-known position) used by all enemies via EnemyPerception."
order: 57
status: "In Development"
tags: ["Enemy", "AI", "Perception", "FOV", "LOS", "Memory"]
last_updated: "2026-03-14"
---

## 🧭 Overview
`EnemyPerception` is a reusable component that provides a **fair awareness model** for enemies.

It answers:
- **Is the player visible right now?** (`IsTargetVisible`)
- **Do we still have recent knowledge?** (`HasTargetKnowledge`)
- **Where should we aim / move toward?** (`KnownTargetPosition`)

This allows enemies to continue behaving intelligently for a short time after losing sight, without “cheating” through walls forever.

## 🎯 Purpose
Replace purely distance-driven awareness with a shared model based on:
- Field-of-view (initial detection vs. combat)
- Line-of-sight raycast checks (occlusion)
- Short-term target memory (last seen position + timer)

## 🧠 Design Philosophy
- **Visibility is primary**: enemies enter battle when they actually see the target.
- **Memory is bounded**: enemies remember for a limited time (`memoryDuration`).
- **Combat can be more forgiving**: combat FOV can be wider than detection FOV so enemies don’t “forget” too easily mid-fight.
- **No state machine inside perception**: it’s a utility component; states decide what to do with its output.

## 📦 Core Responsibilities
**Does**
- Compute visibility each tick:
    - range check (`sightRange`)
    - angle check (`detectionViewAngle` / `combatViewAngle`)
    - line-of-sight raycast (`occlusionMask`)
- Track last-seen target position and timestamps:
    - `LastSeenPosition`
    - `lastSeenTime` / `lastVisibleTime`
- Expose “knowledge” utilities used by AI states:
    - `HasTargetKnowledge`
    - `TimeSinceLastSeen`
    - `KnownTargetPosition`

**Does NOT**
- Decide enemy actions (advance, take cover, shoot, etc.)
- Apply damage or gameplay effects
- Manage navigation

## 🧱 Key Components
Class
- `EnemyPerception` (`code/Enemy/Perception/EnemyPerception.cs`)

Key fields (tuning)
- Vision
    - `sightRange`
    - `detectionViewAngle` (pre-combat)
    - `combatViewAngle` (in combat)
    - `occlusionMask`
- Memory
    - `memoryDuration`
    - `lostSightGraceTime`

Key properties
- `IsTargetVisible`
- `HadVisualContact`
- `HasTargetKnowledge`
- `KnownTargetPosition`
- `TimeSinceLastSeen`

## 🔄 Execution Flow
1. **Setup**
    - Owning enemy calls `SetTarget(target, targetAimPoint)`
    - Ranged enemy typically supplies `playerBody` as aim point for better targeting

2. **Per-frame tick**
    - Owner calls `TickPerception(inCombat)`
    - `ComputeVisibility(inCombat)` evaluates:
        - range
        - angle (FOV)
        - raycast hit vs. target transform hierarchy

3. **Visibility results**
    - If visible:
        - `IsTargetVisible = true`
        - `HadVisualContact = true`
        - `LastSeenPosition = targetAimPoint.position`
        - `lastSeenTime = now`
    - If not visible:
        - `IsTargetVisible` stays true for a short `lostSightGraceTime` to avoid flicker
        - After grace, visibility becomes false while memory can still be valid via `HasTargetKnowledge`

4. **External knowledge refresh**
    - When an enemy is hit, `Enemy.GetHit()` calls:
        - `RegisterTargetKnowledge(knownThreatPosition)`
    - This refreshes memory without faking direct line-of-sight.

## 🔗 Dependencies
Depends On
- Unity Physics (`Physics.Raycast`)
- Correct layer setup (`occlusionMask` should include world + player)

Used By
- `Enemy` base (`TickPerception`, battle entry)
- `EnemyRange` (aim/move toward `KnownTargetPosition`)
- Any future archetype that needs “last known position” reasoning

## ⚠ Constraints & Assumptions
- Memory is time-based only (no pathfinding validation to last-known position).
- `KnownTargetPosition` returns live aim point only when `IsTargetVisible == true`; otherwise it returns `LastSeenPosition`.
- Correct occlusion depends on `occlusionMask` (misconfigured layers will make enemies “blind” or “all-seeing”).

## 📈 Scalability & Extensibility
- Add sound/alert systems by calling `RegisterTargetKnowledge(position)` without changing perception math.
- Add alternative aim points (head/chest transforms) via `SetTargetAimPoint(...)`.

## ✅ Development Status
In Development

## 📝 Notes
This perception system is intentionally shared across melee and ranged enemies so “awareness rules” remain consistent.