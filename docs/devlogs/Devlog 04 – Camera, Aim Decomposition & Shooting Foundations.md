# Devlog 04 – Camera, Aim Decomposition & Shooting Foundations

Date: 16/01/26

## 🎯 Goal
Establish a solid top-down camera and aiming foundation while introducing the first iteration of shooting mechanics.

The focus was on precision, modularity, and player accessibility: smooth character rotation, independent camera and aim control, reliable bullet behavior, and visual feedback for aiming.

## 🧠 Design Approach
This phase treated camera, aim, movement, and shooting as independent but cooperating systems.

### Core Principles
- Camera behavior should enhance player intent, not fight it.
- Aim logic must remain independent from camera orientation.
- Character rotation should feel responsive but natural.
- Shooting mechanics should be deterministic and readable.
- The system should support both beginner-friendly assistance and advanced precision play.

By decomposing these responsibilities early, future systems (weapons, enemies, animations, effects) can be layered without refactoring core logic.

---

## 🏗 Implementation

### Camera & Aim Decomposition
- Separated aim logic and camera logic into independent responsibilities.
- Aim tracks player intent using world-space raycasts.
- Camera target responds smoothly to aim direction using lookahead.
- Camera movement uses interpolation to prevent jitter and sudden jumps.
- Camera distance dynamically adjusts based on player movement to improve spatial readability.

---

### Camera Lookahead & Player Safety Framing

The camera system was designed to bias forward in the direction of the player’s aim, rather than remaining statically centered on the character.

Key behaviors:

- The camera target shifts toward the aim direction to improve visibility and reaction time.
- Camera distance is dynamically clamped to ensure the player always remains on screen.
- Forward bias increases when aiming ahead and reduces when aiming backward or close to the player.
- Aim logic and camera positioning are fully decoupled, allowing each system to evolve independently.

This approach improves situational awareness while preserving consistent player framing.

---

### Smooth Character Rotation
- Replaced snap rotation with smooth spherical interpolation (Slerp).
- Character rotates toward aim direction over time.
- Weapon direction leads the rotation, with the body following naturally.
- Improves animation blending and visual feedback.

---

### Bullet & Shooting Foundations
- Created a temporary bullet prefab to validate shooting flow.
- Bullets spawn from a dedicated gun point on the weapon.
- Bullet direction is derived from the aim position.
- Bullet velocity is applied via rigidbody physics.
- Bullets freeze on impact to avoid unwanted physics artifacts.

---

### Collision Layers & Safety
- Introduced dedicated layers:
  - Player
  - Aim
  - Bullet
- Configured the collision matrix so bullets do not collide with:
  - The player
  - The aim object
- Ensures clean and predictable projectile behavior.

---

### Vertical Axis Control & Precision Aiming
- Identified an issue where bullets traveled vertically based on aim height.
- Default behavior clamps bullet Y-axis for top-down consistency.
- Weapon and gun point are oriented toward the aim object.
- Added an optional precise aiming mode:
  - Allows vertical bullet movement.
  - Intended for experienced players seeking higher skill expression.

---

### Target Lock Assistance
- Implemented an optional target lock system using raycasts.
- When enabled:
  - Aim snaps to valid enemy targets under the cursor.
- Designed to assist beginner players without automating combat.
- Fully modular and toggleable.

---

### Visual Aim Feedback (Laser)
- Added a laser visualization from the gun barrel.
- Laser fades at the tip for clarity.
- Laser shortens dynamically on collision.
- Provides immediate and intuitive aiming feedback.

---

## ⚠ Problems Encountered
- Bullet trajectory varied vertically in a top-down context.
- Character rotation felt rigid when snapping instantly.
- Camera and aim coupling reduced aiming precision.
- New players struggled with accurate aiming.
- Lack of visual feedback made shooting unclear.

---

## ✅ Solutions
- Clamped bullet Y-axis by default.
- Introduced Slerp-based smooth rotation.
- Fully decoupled camera and aim systems.
- Added optional precision aiming and target lock.
- Implemented a laser-based aim visualization system.
- Used collision layers to prevent self-interaction.

---

## 🚀 Result
- Smooth and responsive top-down camera behavior.
- Modular aim and camera architecture.
- The camera dynamically looks ahead toward the aim direction while ensuring the player always remains visible.
- Natural character rotation driven by aim intent.
- Reliable bullet direction and collision behavior.
- Clear visual feedback during combat.
- Scalable foundation for advanced weapon systems.

---

## 📈 Engineering Takeaways
- Early system decomposition prevents compounding complexity.
- Visual feedback dramatically improves gameplay feel.
- Accessibility features can coexist with depth.
- Temporary systems are valuable validation tools.
- Camera and aim deserve first-class architectural attention.

---

## ➡ Next Steps
### Devlog 05 – Environment & Interaction Foundations
- Import 3D environment models (buildings, props, vehicles, nature).
- Set up accurate colliders and pivot points.
- Configure rigidbodies and collision detection.
- Introduce flexible bullet mass.
- Implement shiny bullet materials using URP.
- Add bullet impact visual effects.
- Upgrade the training ground scene for advanced testing.
