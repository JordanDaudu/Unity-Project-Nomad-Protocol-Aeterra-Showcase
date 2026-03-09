---
title: "Systems"
summary: "Architecture documentation for the implemented Unity gameplay systems."
order: 0
status: "In Development"
tags: ["Systems", "Documentation"]
last_updated: "2026-03-05"
---

This section documents **the systems that actually exist in the codebase** as of **2026-03-05**.

## 🗂 Index
## 🔎 Documentation
- [Doc Conventions](./01_Doc_Conventions.md)
- [Scripts → Systems Map](./02_Scripts_To_Systems_Map.md)

### Combat
- [Weapons Data (ScriptableObjects)](./Combat/30_Weapons_Data_ScriptableObjects.md)
- [Weapon Runtime Model](./Combat/31_Weapon_Runtime_Model.md)
- [Player Weapon Controller](./Combat/32_Player_Weapon_Controller.md)
- [Weapon Visuals, Rigging & Animation Events](./Combat/33_Weapon_Visuals_Rigging_AnimationEvents.md)
- [Projectiles: Bullet & Impact FX](./Combat/34_Projectiles_Bullet_ImpactFX.md)

### Core
- [Object Pooling](./Core/10_Object_Pooling.md)
- [Input Actions Integration](./Core/11_Input_Actions_Integration.md)

### Interaction & Pickups
- [Interaction System](./Interaction & Pickups/40_Interaction_System.md)
- [Pickups: Weapons & Ammo](./Interaction & Pickups/41_Pickups_Weapons_and_Ammo.md)

### Player
- [Player Root Composition](./Player/20_Player_Root_Composition.md)
- [Player Movement](./Player/21_Player_Movement.md)
- [Player Aim & Camera Target](./Player/22_Player_Aim_and_Camera_Target.md)
- [Camera Manager (Cinemachine Distance)](./Player/23_Camera_Manager.md)

### Enemy
- [Enemy Core Composition](./Enemy/50_Enemy_Core_Composition.md)
- [Enemy State Machine](./Enemy/51_Enemy_State_Machine.md)
- [Enemy Melee AI](./Enemy/52_Enemy_Melee_AI.md)
- [Enemy Ranged AI](./Enemy/53_Enemy_Ranged_AI.md)
- [Enemy Visuals & Variant Pipeline](./Enemy/54_Enemy_Visuals_and_Variants.md)
- [Enemy Death Pipeline](./Enemy/55_Enemy_Death_Pipeline.md)
- [Enemy Shield & Reactions](./Enemy/56_Enemy_Shield_and_Reactions.md)

## ✅ Scope Rule
These docs describe **implemented systems only**. Future features (new enemy archetypes, missions, etc.) are referenced only as *extension points*.
