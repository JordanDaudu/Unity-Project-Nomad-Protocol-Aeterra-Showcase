---
title: "Player Movement"
summary: "CharacterController-based movement with sprint, gravity, and aim-relative rotation."
order: 21
status: "In Development"
tags: ["Player", "Movement", "Input"]
last_updated: "2026-02-18"
---

## 🧭 Overview
Movement is implemented with Unity’s `CharacterController` and driven by input actions:
- `Move` (Vector2)
- `Sprint` (press/hold)

Rotation is aim-driven: the player turns to face the world point returned by `PlayerAim.GetMouseHitInfo()`.

## 🎯 Purpose
Provide a robust locomotion base for a top-down shooter:
- Responsive movement
- Consistent gravity handling
- Smooth aim-relative rotation
- Animator parameters for blend trees

## 🧠 Design Philosophy
- Movement and rotation are independent but coordinated:
  - Translation comes from WASD input
  - Rotation comes from aim (mouse world point)
- Keep the logic deterministic and animation-friendly.

Trade-off: movement is currently “direct” (no acceleration curves), which is simpler but less physically nuanced.

## 📦 Core Responsibilities
**Does**
- Read movement input and translate via `CharacterController.Move()`.
- Apply custom gravity using a `verticalVelocity` accumulator.
- Rotate the player toward aim point (using `Quaternion.Slerp`).
- Drive animator parameters:
  - `xVelocity`, `zVelocity`
  - `isRunning`

**Does NOT**
- Handle weapon firing, interaction, or camera behavior.
- Implement jump or advanced movement abilities.

## 🧱 Key Components
Classes
- `PlayerMovement` (`Scripts/Player/PlayerMovement.cs`)
  - Owns movement input, speed selection, gravity, rotation, animator updates.

Unity components (required)
- `CharacterController`
- `Animator` (in children)

## 🔄 Execution Flow
1. `Start()`
   - Cache `Player`, `CharacterController`, `Animator`
   - Set initial speed to walk
   - Subscribe to input actions
2. `Update()`
   - `ApplyMovement()`
   - `ApplyGravity()`
   - `ApplyRotation()` (faces `player.aim.GetMouseHitInfo().point`)
   - `AnimatorControllers()` updates blend parameters

## 🔗 Dependencies
**Depends On**
- `Player` for `controls` and `aim`.
- Unity: `CharacterController`, `Animator`.

**Used By**
- `PlayerAim` reads `player.movement.moveInput` to influence camera target distance.

## ⚠ Constraints & Assumptions
- Gravity uses magic values (e.g., `verticalVelocity = -0.5f` when grounded) for grounding stability.
- Assumes `PlayerAim.GetMouseHitInfo()` always returns a usable point (it caches last hit as fallback).

## 📈 Scalability & Extensibility
- Add acceleration/deceleration without changing the input contract.
- Add “strafing vs forward locomotion” animation support by extending animator parameters.
- Add “movement modifiers” (slow, knockback) by adjusting `speed` and movementDirection scaling.

## ✅ Development Status
In Development

## 📝 Notes
Related devlogs:
- Devlog 01 – Input & Player Controller Setup
- Devlog 02 – Player Rigging & Locomotion / Combat Animations
