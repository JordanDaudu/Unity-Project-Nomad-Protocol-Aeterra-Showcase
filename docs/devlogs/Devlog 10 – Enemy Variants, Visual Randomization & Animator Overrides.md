---
title: "Devlog 10 – Enemy Variants, Visual Randomization & Animator Overrides"
date: "2026-02-26"
summary: "Added an enemy variant pipeline with randomized skins/weapons/corruption, animator overrides (unarmed dodge), weapon trails, and attack data via ScriptableObjects."
order: 10
---

## 🎯 Goal
Expand the **Enemy Melee** into multiple **variants** by separating the base model from its visuals and making the enemy look/weapon/attack set **data-driven and randomizable**.

---

## 🧠 Design Approach
Instead of baking “variant identity” into the FBX, the enemy now follows a clear split:

- **Gameplay logic (AI + FSM)** stays the same across variants.
- **Visual identity** (texture, corruption pieces, weapon model) is handled by a dedicated `EnemyVisuals` system.
- **Attack sets** become **data assets** (ScriptableObjects) so each weapon/variant can override attack lists without duplicating code.
- **Animation behavior** can be swapped per-variant using **Animator Override Controllers** (ex: Dodge enemy uses unarmed attack animations).

This keeps the enemy scalable: adding a new variant becomes mostly *data + visuals*, not rewriting AI.

---

## 🏗 Implementation
### 1) Updated enemy model + animation setup
- Switched to an enemy model **without corruption attached by default**.
- Enabled changing the **BaseMap texture** at runtime to create different visual skins.
- Prepared the rig/animations so the same FSM can drive multiple animation sets.

### 2) `EnemyVisuals` system (randomization + setup)
Created `EnemyVisuals` as the central controller for:
- **Random look** (texture swap via `MaterialPropertyBlock`)
- **Random weapon model** filtered by `EnemyMeleeWeaponType` (OneHand / Throw / Unarmed)
- **Random corruption crystals** (enable a randomized subset)
- **Animator overrides** per weapon model when needed
- **Weapon trail toggling** during attacks

> Note: randomization is designed to work well with pooling by triggering on `OnEnable`.

### 3) Weapon models as “variant carriers”
Added `EnemyWeaponModel` to each weapon visual:
- Holds weapon type category
- Optional `AnimatorOverrideController`
- Weapon trail VFX toggles
- A reference to `EnemyMeleeWeaponData` (ScriptableObject) that defines attack list + turn speed

### 4) Attack data moved to ScriptableObjects
Introduced `EnemyMeleeWeaponData`:
- Stores an `attackData` list (attack variants)
- Stores shared tuning values (ex: turn speed)
- Allows the active weapon model to **override** the enemy’s attack list at runtime

### 5) Dodge enemy uses unarmed attacks
- Dodge-type enemies now force `EnemyMeleeWeaponType.Unarmed`
- Their weapon model can provide an animator override controller with unarmed attack clips
- Attack data is swapped accordingly to match the unarmed set

### 6) Weapon trail effect during attacks
Weapon trails are enabled only when needed:
- Turned **on** when entering the attack state
- Turned **off** when exiting the attack state

This makes attacks feel more readable without leaving permanent VFX running.

---

## ⚠ Problems Encountered
- **Corruption was baked into the model**, making “random corruption” impossible.
- Swapping textures directly on materials risked creating unwanted **material instances** and side-effects.
- Variant-specific animation sets (ex: unarmed dodge attacks) required a way to change clips **without duplicating the entire animator**.
- Attack list tuning was getting messy when different variants needed different attack sets.

---

## ✅ Solutions
- Replaced the enemy FBX with a version where corruption is **not attached by default**, and corruption crystals are separate toggles.
- Used **MaterialPropertyBlock** to swap the BaseMap texture per-enemy **without duplicating materials**.
- Added **Animator Override Controllers** on weapon models to switch animation clips per-variant cleanly.
- Moved attack data to **ScriptableObjects**, and let the active weapon model supply the enemy’s attack list at runtime.
- Added an explicit “weapon type category” (`EnemyMeleeWeaponType`) so each variant can filter which models are valid.

---

## 🚀 Result
- Enemies now spawn with a **randomized look** (texture), **random weapon**, and **random corruption layout**.
- Dodge variants can use **unarmed attacks** through animator overrides.
- Weapon trail VFX triggers during attacks for clearer combat feedback.
- Attack sets are now **data-driven**, easier to tune, and easier to expand.

---

## 📈 Engineering Takeaways
- A dedicated **visual pipeline** prevents variant logic from polluting AI code.
- **MaterialPropertyBlock** is the safest way to do per-instance texture swaps in Unity.
- **Animator Overrides** are a clean solution for variant-specific attacks without cloning controllers.
- Moving combat tuning to **ScriptableObjects** makes iteration dramatically faster.

---

## ➡ Next Steps
- Build the next full enemy archetype: **Enemy Ranged** (new AI + behaviors + combat loop).