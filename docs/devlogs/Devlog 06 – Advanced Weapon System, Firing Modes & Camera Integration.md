# Devlog 06 – Advanced Weapon System, Firing Modes & Camera Integration

Date: 2026-02-14

## 🎯 Goal
Transform the weapon system into a scalable combat framework supporting:

- Multiple weapons in inventory
- Equip and reload speed per weapon
- Fire rate control
- Semi-auto and automatic shooting
- Burst / multi-shot firing
- Dynamic bullet spread
- Weapon-specific gun distance
- Camera distance adjustments per weapon
- Bullet object pooling for performance

This phase marks the transition from functional shooting to a production-ready combat architecture.

---

## 🧠 Design Approach
The system was built around modularity and scalability:

- Weapon data lives inside a serializable `Weapon` class.
- Gameplay logic is handled by the controller.
- Visual synchronization is managed through animation events.
- Performance is addressed early through object pooling.
- Camera behavior adapts dynamically based on weapon type.

The goal was to avoid hardcoding behaviors and instead allow weapons to define their own properties.

---

## 🏗 Implementation

### 🔫 Weapon Core Structure
The `Weapon` class now defines:

- Weapon type
- Magazine capacity
- Current bullets
- Reserve ammo
- Reload speed
- Equip speed
- Fire rate
- Shooting type
- Spread configuration
- Burst configuration
- Gun distance

Reload and equip speeds are exposed in the Inspector using ranged values, allowing per-weapon animation tuning without duplicating clips.

---

### ⏱ Fire Rate Control
Added:

- `fireRate`
- `lastFireTime`

This allows control over how many bullets are fired per second.

Instead of firing every frame:
- The weapon checks whether enough time has passed since the last shot.
- Fire rate becomes fully weapon-dependent.

This enables realistic differentiation between pistols, rifles, and automatic weapons.

---

### 🚦 Centralized Weapon Ready State
Introduced a `weaponReady` boolean inside the controller.

It becomes `false` when:
- Reloading
- Switching weapons

It returns to `true` through animation events once the action is complete.

This prevents:
- Shooting mid-reload
- Firing during equip animations
- State conflicts

It acts as a lightweight combat state gate.

---

### 🔁 Semi-Auto & Automatic Shooting
Added a `ShootType` enum:

- SemiAuto
- Auto

Input system changes:
- Attack performed → `isShooting = true`
- Attack canceled → `isShooting = false`

In Update:
- Shooting only occurs while `isShooting` is true.
- If the weapon is SemiAuto, shooting automatically resets `isShooting` to false after one shot.

Result:
- Semi-auto fires once per click.
- Auto continues firing while holding input.

Clean, extensible design.

---

### 🎯 Dynamic Bullet Spread System
Implemented a progressive spread mechanic:

Weapon defines:
- Base spread
- Maximum spread
- Spread increase rate
- Spread cooldown

Behavior:
- Each shot increases spread.
- If enough time passes without shooting, spread resets to base.
- Spread is applied via randomized rotation offsets.

This creates:
- Recoil simulation
- Spray control mechanics
- Skill-based accuracy management

Spread now becomes part of weapon identity.

---

### 💥 Burst / Multi-Shot Mode
Added burst configuration:

- Burst availability
- Burst toggle state
- Bullets per burst
- Burst fire rate
- Delay between burst shots

This enables:
- 3-round burst weapons
- Controlled burst firing patterns
- Future expansion into shotgun-style multi-projectile firing

This system coexists with semi-auto and auto cleanly.

---

### 📏 Gun Distance Per Weapon
Each weapon now defines its effective gun distance.

Examples:
- Rifle → long distance
- Shotgun → short distance

This will influence:
- Shooting validation
- Hit logic
- Tactical weapon differentiation

---

## 🎥 Camera Integration (Cinemachine)

### 🎛 Camera Manager
Introduced a centralized `CameraManager` singleton.

Responsibilities:
- Manage Cinemachine camera distance dynamically
- Smoothly interpolate between distances
- Persist across scenes

When switching weapons:
- Camera distance changes based on weapon configuration.
- Rifle increases camera distance.
- Close-range weapons reduce camera distance.

Camera transitions are interpolated using Lerp for smooth visual shifts.

This creates:
- Tactical awareness differences per weapon
- Clear gameplay feedback
- Visual variety without multiple camera setups

---

## 🎒 Inventory Expansion
Inventory now supports more than two weapons.

Backup weapon visuals are intelligently assigned based on hang type:

- Low back hang
- Back hang
- Side hang

The system:
- Prevents overlap
- Skips currently equipped weapon
- Activates appropriate backup visuals based on inventory contents

This allows scaling beyond two weapons without visual conflicts.

---

## 🚀 Object Pooling Foundation

### 🧩 Bullet Pool
Implemented a reusable `ObjectPool` system:

- Pre-instantiates bullets
- Stores inactive bullets in a queue
- Returns bullets instead of instantiating
- Deactivates and recycles after use
- Persists across scenes

Benefits:
- Eliminates frequent Instantiate/Destroy calls
- Reduces garbage collection pressure
- Stabilizes performance under high fire rate or burst scenarios

This prepares the system for:
- Automatic weapons
- Burst fire
- Shotguns
- Future enemy projectiles

---

## ⚠ Problems Encountered
- Managing firing logic across multiple shooting modes.
- Preventing shooting during reload or equip.
- Keeping spread behavior intuitive and balanced.
- Ensuring camera transitions feel smooth.
- Scaling backup weapon visuals without overlap.
- Preventing performance drops from rapid bullet spawning.

---

## ✅ Solutions
- Introduced fire rate timing via timestamps.
- Centralized combat readiness through `weaponReady`.
- Implemented progressive spread with cooldown reset.
- Used input performed/canceled for clean auto fire control.
- Added weapon-driven camera distance control.
- Created an extensible object pool system.
- Structured backup weapon visuals by hang category.

---

## 🚀 Result
- Weapons support semi-auto, auto, and burst firing.
- Spread dynamically increases during sustained fire.
- Reload and equip speeds are weapon-driven.
- Camera adapts per weapon type.
- Inventory supports multiple weapons cleanly.
- Bullet instantiation no longer impacts performance.
- Combat system is modular and production-ready.

---

## 📈 Engineering Takeaways
- Combat systems require state control, not just shooting logic.
- Fire rate timing should never rely on frame updates.
- Animation events are essential for clean state transitions.
- Spread systems add depth with minimal complexity.
- Camera adjustments dramatically enhance weapon identity.
- Object pooling must be implemented before scaling combat.
- Designing for expansion early prevents exponential complexity later.

---

## ➡ Next Steps
- Refactor ObjectPool into a **global, expandable pooling system** (not bullet-only).
- Introduce proper **constructors** for cleaner weapon initialization.
- Transition weapon configuration into **Scriptable Objects**.
- Separate runtime weapon state from static **Weapon Data assets**.