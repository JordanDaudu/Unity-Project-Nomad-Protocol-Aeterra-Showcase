---
title: "Scripts → Systems Map"
summary: "Mapping of every custom script in the project to its owning system documentation."
order: 2
status: "In Development"
tags: ["Documentation", "Mapping"]
last_updated: "2026-03-14"
---

This page maps **every project script** to the system doc where it belongs.

> Note: Script paths are relative to `/code/`.

| System Doc | Script | Path |
|---|---|---|
| Core/Object Pooling | `ObjectPool` | `code/ObjectPool.cs` |
| Core/Object Pooling | `PooledObject` | `code/PooledObject.cs` |
| Core/Input Actions Integration | `InputActions` | `code/InputActions.cs` |
| Core/Input Actions Integration | `InputManager` | `code/InputManager.cs` |
| Player/Player Root Composition | `Player` | `code/Player.cs` |
| Player/Player Movement | `PlayerMovement` | `code/PlayerMovement.cs` |
| Player/Player Aim & Camera Target | `PlayerAimAndCameraTarget` | `code/PlayerAimAndCameraTarget.cs` |
| Player/Camera Manager | `CameraManager` | `code/CameraManager.cs` |
| Combat/Weapons Data | `WeaponData` | `code/WeaponData.cs` |
| Combat/Weapon Runtime Model | `Weapon` | `code/Weapon.cs` |
| Combat/Player Weapon Controller | `PlayerWeaponController` | `code/PlayerWeaponController.cs` |
| Combat/Weapon Visuals | `PlayerWeaponVisuals` | `code/PlayerWeaponVisuals.cs` |
| Combat/Weapon Visuals | `PlayerWeaponModel` | `code/PlayerWeaponModel.cs` |
| Combat/Weapon Visuals | `BackupWeaponModel` | `code/BackupWeaponModel.cs` |
| Combat/Weapon Visuals | `AimRigController` | `code/AimRigController.cs` |
| Combat/Weapon Visuals | `WeaponAnimEvents` | `code/WeaponAnimEvents.cs` |
| Combat/Projectiles | `Bullet` | `code/Bullet.cs` |
| Combat/Projectiles | `BulletImpactFx` | `code/BulletImpactFx.cs` |
| Combat/Projectiles | `EnemyBullet` | `code/Enemy/EnemyBullet.cs` |
| Combat/Projectiles | `EnemyGrenade` | `code/Enemy/EnemyGrenade.cs` |
| Interaction & Pickups/Interaction System | `Interactable` | `code/Interactable.cs` |
| Interaction & Pickups/Interaction System | `InteractionManager` | `code/InteractionManager.cs` |
| Interaction & Pickups/Pickups | `PickupWeapon` | `code/PickupWeapon.cs` |
| Interaction & Pickups/Pickups | `PickupAmmo` | `code/PickupAmmo.cs` |
| Interaction & Pickups/Pickups | `PickupHealth` | `code/PickupHealth.cs` |
| Enemy/Core Composition | `Enemy` | `code/Enemy/Enemy.cs` |
| Enemy/Perception System | `EnemyPerception` | `code/Enemy/Perception/EnemyPerception.cs` |
| Enemy/State Machine | `EnemyStateMachine` | `code/Enemy/StateMachine/EnemyStateMachine.cs` |
| Enemy/State Machine | `EnemyState` | `code/Enemy/StateMachine/EnemyState.cs` |
| Enemy/Melee AI | `EnemyMelee` | `code/Enemy/EnemyMelee/EnemyMelee.cs` |
| Enemy/Melee AI | `IdleState_Melee` | `code/Enemy/EnemyMelee/IdleState_Melee.cs` |
| Enemy/Melee AI | `MoveState_Melee` | `code/Enemy/EnemyMelee/MoveState_Melee.cs` |
| Enemy/Melee AI | `ChaseState_Melee` | `code/Enemy/EnemyMelee/ChaseState_Melee.cs` |
| Enemy/Melee AI | `AttackState_Melee` | `code/Enemy/EnemyMelee/AttackState_Melee.cs` |
| Enemy/Melee AI | `RecoveryState_Melee` | `code/Enemy/EnemyMelee/RecoveryState_Melee.cs` |
| Enemy/Melee AI | `AbilityState_Melee` | `code/Enemy/EnemyMelee/AbilityState_Melee.cs` |
| Enemy/Melee AI | `DeadState_Melee` | `code/Enemy/EnemyMelee/DeadState_Melee.cs` |
| Enemy/Ranged AI | `EnemyRange` | `code/Enemy/EnemyRange/EnemyRange.cs` |
| Enemy/Ranged AI | `IdleState_Range` | `code/Enemy/EnemyRange/IdleState_Range.cs` |
| Enemy/Ranged AI | `MoveState_Range` | `code/Enemy/EnemyRange/MoveState_Range.cs` |
| Enemy/Ranged AI | `BattleState_Range` | `code/Enemy/EnemyRange/BattleState_Range.cs` |
| Enemy/Ranged AI | `RunToCoverState_Range` | `code/Enemy/EnemyRange/RunToCoverState_Range.cs` |
| Enemy/Ranged AI | `AdvanceToPlayer_Range` | `code/Enemy/EnemyRange/AdvanceToPlayer_Range.cs` |
| Enemy/Ranged AI | `ThrowGrenadeState_Range` | `code/Enemy/EnemyRange/ThrowGrenadeState_Range.cs` |
| Enemy/Ranged AI | `DeadState_Range` | `code/Enemy/EnemyRange/DeadState_Range.cs` |
| Enemy/Ranged AI | `EnemyRangeWeaponData` | `code/Enemy/Data/EnemyRangeWeaponData.cs` |
| Enemy/Cover System | `Cover` | `code/Enemy/CoverSystem/Cover.cs` |
| Enemy/Cover System | `CoverPoint` | `code/Enemy/CoverSystem/CoverPoint.cs` |
| Enemy/Cover System | `EnemyCoverController` | `code/Enemy/CoverSystem/EnemyCoverController.cs` |
| Enemy/Visuals & Variants | `EnemyVisuals` | `code/Enemy/EnemyVisuals.cs` |
| Enemy/Visuals & Variants | `EnemyWeaponModel` | `code/Enemy/EnemyWeaponModel.cs` |
| Enemy/Visuals & Variants | `EnemyMeleeWeaponModel` | `code/Enemy/EnemyMeleeWeaponModel.cs` |
| Enemy/Visuals & Variants | `EnemyRangeWeaponModel` | `code/Enemy/EnemyRangeWeaponModel.cs` |
| Enemy/Visuals & Variants | `EnemySecondaryRangeWeaponModel` | `code/Enemy/EnemyRange/EnemySecondaryRangeWeaponModel.cs` |
| Enemy/Visuals & Variants | `EnemyCorruptionCrystal` | `code/Enemy/EnemyCorruptionCrystal.cs` |
| Enemy/Visuals & Variants | `EnemyMeleeWeaponData` | `code/Enemy/Data/EnemyMeleeWeaponData.cs` |
| Enemy/Death Pipeline | `EnemyRagdoll` | `code/Enemy/EnemyRagdoll.cs` |
| Enemy/Death Pipeline | `EnemyDeathDissolve` | `code/Enemy/EnemyDeathDissolve.cs` |
| Enemy/Shield & Reactions | `EnemyShield` | `code/Enemy/EnemyShield.cs` |
| Enemy/Animation Events | `EnemyAnimationEvents` | `code/Enemy/EnemyAnimationEvents.cs` |