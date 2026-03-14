---
title: "Enemy Visuals & Variant Pipeline"
summary: "Enemy presentation layer: randomized skins/weapons/corruption, weapon model selection, animation-layer overrides, weapon trails, and Animation Rigging (IK + aim) coordination."
order: 54
status: "In Development"
tags: ["Enemy", "Visuals", "Variants", "Animation", "Rigging", "IK"]
last_updated: "2026-03-14"
---

## 🧭 Overview
Enemy variants are implemented by separating **gameplay logic** from **visual identity**:

- AI/FSM stays in `EnemyMelee` / `EnemyRange`
- Visual randomization + setup lives in `EnemyVisuals`
- Weapon models act as “variant carriers” and can:
    - expose compatibility rules
    - provide animation overrides (layer index / optional override controller)
    - provide weapon-specific targets (gun points, IK targets)
    - toggle extra visuals (secondary model, grenade model, trails)

This enables adding new variants primarily through **prefab setup + data assets**, without rewriting AI logic.

## 🎯 Purpose
Keep enemy visuals scalable and data-driven:
- Random skin/texture
- Random compatible weapon model
- Random corruption crystals
- Variant-specific animation configuration (e.g., unarmed dodge)
- Weapon trails during attacks
- Ranged combat readability (aim IK, secondary model swaps, grenade model)

## 🧠 Design Philosophy
- **Single coordinator**: `EnemyVisuals` is the “one place” states talk to for visuals.
- **Weapon models are modular**: each model is a self-contained visual package.
- **Rigging is blended, not snapped**: IK weights can be smoothly enabled/disabled to avoid popping.
- **Gameplay doesn’t depend on visuals**: visuals read from enemy state, but don’t own AI decisions.

## 📦 Core Responsibilities
**Does**
- Randomize look
    - picks a texture from `colorTextures`
    - applies it to the enemy mesh (`SkinnedMeshRenderer`)
- Randomize corruption
    - enables a subset of corruption crystal objects (per enemy spawn)
- Select a compatible weapon model
    - iterates through available `EnemyWeaponModel` instances
    - activates a compatible one based on `EnemyWeaponModel.IsCompatibleWith(Enemy)`
- Configure ranged weapon hardpoints + rigging
    - `EnemyRangeWeaponModel.GunPoint` is used by `EnemyRange` to spawn bullets
    - weapon models can provide left-hand / elbow IK targets when required
- Coordinate combat visual toggles used by states
    - weapon root on/off (`EnableWeaponModel`)
    - secondary weapon root (`EnableSecondaryWeaponModel`) used for throw animations
    - grenade model (`EnableGrenadeModel`) used during grenade throw state
    - IK weights (`EnableIK`) used in ranged combat states

**Does NOT**
- Decide variant gameplay behavior (that lives in AI + perks)
- Spawn projectiles or apply damage

## 🧱 Key Components
Core
- `EnemyVisuals` (`code/Enemy/EnemyVisuals.cs`)

Weapon model system
- `EnemyWeaponModel` (base abstract visual model)
- Melee-specific models
    - `EnemyMeleeWeaponModel` (and subclasses) exposing melee weapon type rules
- Ranged-specific models
    - `EnemyRangeWeaponModel`
        - `weaponType` + `AnimationLayerIndex`
        - `GunPoint` used for bullets
        - optional IK targets (left hand/elbow)
        - `primaryVisualRoot` / `secondaryVisualRoot`
    - `EnemySecondaryRangeWeaponModel` (marker to locate secondary model)
- Optional VFX
    - `WeaponTrail` / trail toggles used during attacks

Rigging
- Uses Unity Animation Rigging constraints (e.g., aim rig, left-hand IK rig)
- `EnableIK(leftHand, aim)` is called by ranged states to keep aiming readable

## 🔄 Execution Flow
1. Spawn / reuse
    - Enemy calls `EnemyVisuals.SetupLook()`
    - Visuals:
        - randomize texture
        - select weapon model
        - randomize corruption

2. Combat states toggle readability
    - Ranged:
        - `BattleState_Range` enables IK (aim + left-hand if needed)
        - `ThrowGrenadeState_Range` disables IK and swaps to secondary + grenade model
    - Melee:
        - trails and animator overrides can be enabled per weapon model / variant

## 🔗 Dependencies
Depends On
- Enemy prefab wiring (models, crystals, textures)
- Unity Animation Rigging package (constraints + rigs)

Used By
- `EnemyMelee`, `EnemyRange`
- Ranged states (IK + model swap coordination)

## ⚠ Constraints & Assumptions
- Weapon model compatibility must be correctly authored in prefabs (wrong compatibility ⇒ wrong visuals).
- IK targets must exist for models that set `RequiresLeftHandIK = true`.

## 📈 Scalability & Extensibility
- Add a new enemy variant by adding a new `EnemyWeaponModel` prefab child + compatibility rule.
- Add new ranged weapons by:
    - adding `EnemyRangeWeaponData` assets
    - adding a matching `EnemyRangeWeaponModel` visual
    - selecting via `EnemyRangeWeaponType`

## ✅ Development Status
In Development