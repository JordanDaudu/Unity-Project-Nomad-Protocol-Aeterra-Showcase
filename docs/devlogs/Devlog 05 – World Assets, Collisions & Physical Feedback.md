# Devlog 05 – World Assets, Collisions & Physical Feedback

Date: 2026-02-04

## 🎯 Goal
Build the physical foundation of the game world by importing environment assets and defining how objects collide, react to bullets, and communicate weight, resistance, and impact feedback.

---

## 🧠 Design Approach
This phase focused on gameplay feel first and realism second, while maintaining strong performance.

Instead of treating all objects equally, each prop was evaluated based on:
- Gameplay importance
- Interaction frequency
- Required collision precision
- Performance cost

Physics and collisions were tuned to feel intentional and readable rather than physically perfect.

---

## 🏗 Implementation

### Asset Import
Imported a wide range of world assets, including:
- Cars and RVs
- Buildings and abandoned structures
- Dumpsters, chests, barrels
- Environmental props and debris

These assets form the basis of the playable environment and testing areas.

---

### Colliders
Started adding colliders incrementally and deliberately.

Key decisions:
- Mesh Colliders are used only when high precision is required
- Mesh Colliders are marked Static to reduce physics cost
- Convex Mesh Colliders are preferred when possible
- Box and Capsule Colliders are used wherever approximation is sufficient

This approach balances accuracy with performance.

---

### Rigidbodies & Physical Feel
Added Rigidbodies to selected props to test bullet interaction and object response.

Mass values were tuned to communicate object weight:
- Medium-weight props (e.g., barrels) react noticeably but feel grounded
- Light props (e.g., small table items) fly further and faster
- Heavy or steel objects resist movement almost entirely

This creates intuitive and satisfying physical feedback during combat.

---

### Drag & Damping
Adjusted Rigidbody damping values to control post-impact behavior:
- Linear damping (drag) prevents objects from sliding or flying too far
- Angular damping limits excessive rotation after impact

This keeps interactions responsive without feeling chaotic.

---

### Kinematic Objects
Identified objects that should not react to physics:
- Kinematic Rigidbodies are used for static or scripted props
- These objects are not affected by forces or collisions
- Ensures stability where physical reaction is not desired

---

### Collision Detection Modes
Learned and applied proper Rigidbody collision detection modes:
- Discrete for static or slow-moving objects
- Continuous for fast objects interacting with static or kinematic bodies
- Continuous Dynamic for fast objects interacting with any object type

Bullets now use Continuous Dynamic collision detection to guarantee hit registration, while limiting performance cost by using it only where necessary.

---

### Bullet Visual Feedback
- Added a bullet material with increased intensity so bullets visually stand out
- Applied the same material to bullet trails for clarity during testing
- Integrated a VFX effect pack so an impact effect plays when a bullet collides with an object

This significantly improves hit feedback and makes combat interactions easier to read.

---

### Testing Environment
- Added multiple props to the scene specifically for physics testing
- Used these props to compare mass, drag, and collision behavior
- Iteratively adjusted values based on how objects feel when shot

---

## ⚠ Problems Encountered
- Fast bullets occasionally missed collisions at high speed
- Overuse of Mesh Colliders caused unnecessary performance cost
- Some props felt either too floaty or unnaturally heavy
- Objects without damping behaved unrealistically after impact

---

## ✅ Solutions
- Applied Continuous Dynamic collision detection only to bullets
- Reduced Mesh Collider usage to essential cases
- Tuned mass and damping per prop category
- Used kinematic Rigidbodies where physical interaction was not needed
- Added impact VFX to reinforce hit confirmation

---

## 🚀 Result
- Reliable bullet collision detection at all speeds
- Props react consistently and intuitively to being shot
- Clear visual feedback for bullet impacts
- Stable performance despite increased physical complexity
- A solid physical sandbox for future combat systems

---

## 📈 Engineering Takeaways
- Collision detection mode is critical for fast projectiles
- Mesh Colliders should be used sparingly and intentionally
- Physics feel comes from tuning, not realism
- Visual feedback greatly improves gameplay clarity and debugging
- Treating props by gameplay importance leads to better performance decisions

---

## ➡ Next Steps
Design and implement a full Weapon System, including:
- Weapon base class and weapon types
- Weapon slots and weapon pickup
- Ammo handling and reload logic
- Weapon models and backup weapon visuals
- Equip and reload speed
- Bullet object pooling
- Fire rate and weapon readiness checks
- Single, automatic, burst, multi-shot, and shotgun fire modes
- Bullet spread logic
- Gun distance and camera distance integration
