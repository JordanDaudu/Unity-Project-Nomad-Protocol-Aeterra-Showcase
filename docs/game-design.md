# Game Design Overview (System-Oriented)

## Project Intent

This project is a long-term solo portfolio project built in Unity (C#). While presented as a 3D top-down shooter, its primary purpose is to demonstrate software engineering skills such as system architecture, modular design, scalability, performance awareness, and technical documentation.

The game acts as a real-time interactive system used to showcase engineering decision-making rather than a purely entertainment-focused product.

---

## Core Gameplay Loop

1. Player navigates a top-down 3D environment
2. Player aims and engages enemies using modular weapons
3. Enemies react using state-driven AI behaviors
4. Combat results in rewards, progression, or mission completion
5. Player upgrades equipment and continues to new encounters

This loop is intentionally simple to allow focus on system quality and extensibility.

---

## Player Capabilities

### Movement & Control

* Top-down movement with gravity handling
* Smooth rotation and aim decomposition
* Input abstraction layer to decouple input sources from movement logic
* Animation-driven movement states (idle, walk, run, fire)

### Combat

* Weapon-based shooting (single, automatic, burst)
* Reload mechanics
* Bullet spread and multi-shot logic
* Object-pooled projectiles

### Interaction

* Generic interaction system
* Weapon pickup and drop
* Ammo box interaction
* Vehicle entry and exit

---

## Weapon System Overview

Weapons are implemented as modular, data-driven entities.

### Weapon Features

* Weapon slots and equip system
* Ammo management and reload speed
* Fire rate and fire mode
* Bullet spread and shot patterns
* Camera and weapon distance parameters

### Design Constraints

* Weapon logic is separated from visual representation
* Weapon data is stored in ScriptableObjects
* New weapons should be addable without modifying core systems

---

## Enemy Systems

### Enemy Types

#### Melee Enemies

* NavMesh-based navigation
* Finite State Machine (FSM)
* States: Idle, Patrol, Chase, Attack, Recovery, Dead
* Ability extensions (shield, dodge, axe throw)
* Ragdoll on death

#### Ranged Enemies

* Tactical positioning
* Attack cooldowns
* Cover system
* Grenade throw state
* Weapon-based attacks

#### Boss Enemies

* Large-scale enemy with unique abilities
* Multiple behavioral phases
* Ability-driven combat (e.g. flamethrower)

---

## AI Design Principles

* State-machine-driven behavior
* Clear state responsibility boundaries
* Separation between decision-making and execution
* Extensible via composition rather than deep inheritance

---

## Damage & Combat Resolution

### Damage System Goals

* Centralized damage resolution
* Support for friendly fire
* Hitbox-based damage detection
* Interface-driven damage sources and receivers

### Design Constraints

* Weapons do not directly modify health
* Damage flows through a common interface
* Combat balance handled via data, not hardcoded logic

---

## Procedural Level Generation

### Goals

* Automated level creation
* Configurable generation rules
* Zone boundaries and placement constraints

### Constraints

* Generation logic must be deterministic where possible
* Systems should support future extension

---

## Mission & Quest System

### Mission Types

* Timed missions
* Hunt missions
* Delivery missions (e.g. car delivery)

### Architecture

* Central mission manager
* Decoupled mission definitions
* Event-driven mission progress tracking

---

## User Interface

### UI Elements

* Player health bar
* Weapon and ammo display
* Mission objectives
* Main menu and pause menu

### Design Principles

* UI reflects game state, not logic
* UI updated through events
* Minimal direct coupling to gameplay systems

---

## Vehicles

### Vehicle Features

* Drivable cars
* Acceleration, braking, and drifting
* Vehicle health system
* Camera switching when entering vehicles

### Constraints

* Vehicles integrate with the interaction system
* Shared damage and health logic

---

## Audio System

### Audio Scope

* Sound effects
* Background music

### Architecture Goals

* Centralized audio service
* Event-driven playback
* No direct audio calls from gameplay logic

---

## Non-Goals

* Multiplayer support
* Narrative-driven gameplay
* Photorealistic visuals
* Complex simulation realism

---

## Summary

This project prioritizes clean architecture, scalability, and long-term maintainability. Gameplay features exist to serve system design goals, making the project suitable as a professional software engineering portfolio piece rather than a purely entertainment-focused game.
