---
title: "Enemy Visuals & Variant Pipeline"
summary: "Data-driven enemy look/weapon/corruption randomization, weapon model selection, animator overrides, and weapon trail control."
order: 54
status: "In Development"
tags: ["Enemy", "Visuals", "Variants", "Animation"]
last_updated: "2026-03-05"
---

## 🧭 Overview
Enemy variants are implemented by separating **gameplay logic** from **visual identity**:
- AI/FSM stays in `EnemyMelee` / `EnemyRange`
- Visual randomization + setup lives in `EnemyVisuals`
- Weapon models act as "variant carriers" and can override animations and attack data

This enables adding new variants primarily through *prefab setup + data assets*.

## 🎯 Purpose
Allow enemies to spawn with variety without duplicating AI code:
- Random skin/texture
- Random weapon model (filtered by type)
- Random corruption crystals
- Variant-specific animator overrides (e.g., unarmed dodge attacks)
- Weapon trail VFX control during attacks

## 🧠 Design Philosophy
- **Per-instance material changes via MaterialPropertyBlock** (avoid material instancing).
- **Weapon model = configuration hub**: optional animator override + data asset links.
- **Pooling-friendly**: randomization should be safe to run on spawn/reuse.

## 📦 Core Responsibilities
**Does**
- Choose and apply a randomized visual setup
- Select an appropriate `EnemyWeaponModel` based on enemy type/weapon category
- Apply optional `AnimatorOverrideController` to the enemy animator
- Toggle weapon trail VFX at attack timing
- Enable a randomized subset of corruption crystals

**Does NOT**
- Decide combat behavior (owned by the FSM)
- Implement attack timing (owned by animation events + states)

## 🧱 Key Components
Classes
- `EnemyVisuals` (`code/Enemy/EnemyVisuals.cs`)
- Weapon models
  - `EnemyWeaponModel` (base) (`code/Enemy/EnemyWeaponModel.cs`)
  - `EnemyMeleeWeaponModel` (`code/Enemy/EnemyMeleeWeaponModel.cs`)
  - `EnemyRangeWeaponModel` (`code/Enemy/EnemyRangeWeaponModel.cs`)
- Corruption
  - `EnemyCorruptionCrystal` (`code/Enemy/EnemyCorruptionCrystal.cs`)

Data
- `EnemyMeleeWeaponData` (`code/Enemy/Data/EnemyMeleeWeaponData.cs`)

## 🔄 Execution Flow
1. Enemy spawns
2. `EnemyVisuals` selects:
   - Skin (texture via property block)
   - Weapon model matching category (OneHand/Throw/Unarmed)
   - Optional animator override
   - Corruption crystals subset
3. Combat states can query weapon model data to configure attack lists/turn speed
4. Attack state toggles weapon trail on enter/exit (via weapon model)

## 🔗 Dependencies
**Depends On**
- Unity: `Renderer`, `MaterialPropertyBlock`, `AnimatorOverrideController`
- Enemy archetype for filtering compatibility

**Used By**
- Enemy archetypes (`EnemyMelee`, `EnemyRange`) call visuals setup
- Melee combat uses weapon model data overrides to set attack lists

## ⚠ Constraints & Assumptions
- Prefabs must include weapon model children/components and corruption crystal objects.
- Animator override controllers must match the base controller parameters.

## 📈 Scalability & Extensibility
- Add new variants by adding new weapon models and data assets.
- Extend selection rules (rarity weighting, biome-based skins) without changing AI.

## ✅ Development Status
In Development

## 📝 Notes
- This doc corresponds to the pipeline introduced in Devlog 10.
