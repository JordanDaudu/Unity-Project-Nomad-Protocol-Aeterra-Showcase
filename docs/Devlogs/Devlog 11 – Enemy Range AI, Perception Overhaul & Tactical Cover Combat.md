---
title: "Devlog 11 – Enemy Range AI, Perception System & Tactical Cover Combat"
date: "2026-03-14"
summary: "Implemented a new ranged enemy archetype featuring perception-based AI, tactical cover behavior, smart aiming, grenade combat, and animation IK integration."
order: 11
---

## 🎯 Goal
Design and implement a completely new enemy archetype: **Enemy Range**.

This phase focused on building a ranged enemy that feels tactically different from melee enemies by introducing:
- A new ranged combat state machine
- A reusable **perception system** for enemies
- A smart **cover system**
- Ranged weapon, bullet, and grenade combat logic
- Animation, IK, and secondary weapon visual support for ranged combat

---

## 🧠 Design Approach
This phase was built around one major idea:

**Ranged enemies should not just be “melee enemies that shoot.”**

Instead, Enemy Range was designed as a tactical combatant with its own decision-making loop:
- Detect the player through a shared perception system
- Decide whether to hold position, advance, or take cover
- Fight from cover when possible
- Use perks to vary behavior between enemy types
- Support animation-driven combat without breaking movement and aiming readability

At the architecture level, this devlog also marks a major evolution of the base enemy system:
- The base `Enemy` class was expanded to support a shared **perception and target memory system**
- Cover logic was intentionally separated into its own reusable controller
- Ranged combat behavior was split into states instead of being handled by one large script

This keeps the system scalable for future ranged enemy variants.

---

## 🏗 Implementation

### 👁️ Enemy Perception System Overhaul
A new `EnemyPerception` component was added as a shared perception layer for enemies.

It is responsible for:
- Detecting whether the player is currently visible
- Handling field-of-view logic
- Storing recent target knowledge / last known position
- Letting enemies continue acting intelligently even after briefly losing sight of the player

This was a major milestone because enemies are no longer purely distance-driven.
They now operate with a more believable awareness model based on:
- **Current vision**
- **Known target position**
- **Recent memory of the player**

The base `Enemy` class now uses this perception system to decide when to:
- Enter battle mode
- Keep pursuing
- Return to idle if the player is truly lost

---

### 🔫 Enemy Range Core Architecture
Created a full new `EnemyRange` type built on top of the shared `Enemy` base.

It introduces ranged-specific combat responsibilities such as:
- Weapon setup and weapon data selection
- Bullet firing
- Smart aiming
- Grenade logic
- Cover interaction
- Ranged-only state machine decisions

This enemy is not a small extension of melee behavior — it is a distinct combat archetype.

---

### 🧠 Ranged State Machine
Implemented a dedicated ranged state machine with the following states:
- **Idle**
- **Move**
- **Battle**
- **RunToCover**
- **Advance**
- **ThrowGrenade**
- **Dead**

Each state has a clear role:

- **Idle / Move**  
  Non-combat patrol behavior.

- **Battle**  
  The core ranged combat state. The enemy stops, faces the player, aims, shoots, and may evaluate better cover.

- **RunToCover**  
  Used when the enemy is allowed to take cover and has successfully reserved a cover point.

- **Advance**  
  Used when the enemy should push forward instead of staying at current position, especially when the player is out of effective combat distance or when no valid cover is available.

- **ThrowGrenade**  
  Handles grenade throw preparation, animation timing, visual weapon swaps, and projectile spawn timing.

- **Dead**  
  Similar death pipeline to melee enemies, including grenade edge-case handling.

This separation made the ranged logic much easier to reason about and tune.

---

### 🛡️ Cover System
A full reusable cover system was implemented.

Core components:
- `Cover`
- `CoverPoint`
- `EnemyCoverController`

#### Cover Objects
`Cover` represents a world object that can provide protection.
Each cover object owns one or more `CoverPoint`s.

#### Cover Points
Enemies do not reserve an entire cover object.
They reserve a specific **cover point**.
This allows the system to reason about:
- Which side of the object is safer
- Whether the point is already occupied
- Whether it is valid relative to the player’s current position

#### Enemy Cover Controller
`EnemyCoverController` handles:
- Searching nearby covers
- Evaluating available cover points
- Reserving one point for this specific enemy
- Releasing the point when the enemy leaves, dies, or changes plan
- Trying to upgrade to better cover later if allowed

This system keeps cover logic out of `EnemyRange` itself and makes the behavior much more modular.

---

### 🧭 How the Cover System Works from the Enemy’s Perspective
Enemy Range uses the cover system in a tactical sequence:

1. **Enemy enters battle mode**  
   It checks its `CoverPerk`.

2. **Enemy decides whether it can use cover**
   - `None` → never uses cover
   - `TakeCoverOnce` → may take cover when battle starts
   - `Reposition` → may take cover and later move to better cover

3. **Enemy requests a cover point**
   The enemy asks its `EnemyCoverController` to find and reserve the best valid point.

4. **If cover is found**
   - The point is reserved
   - The enemy moves to it through `RunToCoverState_Range`

5. **If cover is not found**
   - The enemy falls back to direct ranged battle behavior instead of getting stuck

6. **Enemy fights from cover**
   In `BattleState_Range`, it faces the player, aims, shoots, and treats the reserved cover point as its combat position

7. **Reposition-capable enemies may search for better cover later**
   If a clearly better point is found, the enemy re-enters `RunToCoverState_Range`

8. **Reserved cover is released when no longer needed**
   This prevents enemies from permanently owning cover spots

This makes ranged enemies feel much more stable and intentional instead of constantly bouncing between positions.

---

### 🎯 Smart Aim System
Enemy Range uses a smarter aiming model than simply rotating toward the player.

The system supports:
- Hidden aim speed vs visible aim speed
- Aim locking behavior
- Aim tolerance radius
- Time-on-target requirements before full accuracy

This creates a more believable ranged combat loop:
- The enemy needs time to properly line up a shot
- Aiming behavior can be tuned per weapon and encounter type
- The player has a better chance to read and respond to enemy pressure

---

### 🔫 Ranged Weapons, Bullets & Firing Logic
Enemy Range now supports:
- Weapon type selection
- Weapon data
- Enemy bullets
- Controlled attack delay
- Fire cadence through battle state logic

Ranged enemies are no longer abstract damage dealers — they now fully participate in the same projectile-based combat language as the player.

This also makes balancing easier because enemy weapons can be tuned using similar mental models:
- Fire rate
- Accuracy
- Burst / shot count
- Effective range

---

### 🛡️ UnstoppablePerk

The **Unstoppable** perk changes how a ranged enemy handles pressure and positioning.

Normally, ranged enemies stop advancing once they reach a valid combat range and have a clear line of sight to the player.

An enemy with **Unstoppable** behaves more aggressively:
- It does not rely on the normal combat range checks to stop advancing
- It tends to **slowly push toward the player instead of holding defensive positions**
- It may still interact with cover if cover behavior is also enabled
- It will **maintain pressure even under fire**
- It typically has **higher health pools** to support this aggressive role

This creates a different combat dynamic compared to standard ranged enemies:

- **Cover shooters** → defensive ranged pressure  
- **Grenadiers** → area denial and forced movement  
- **Unstoppable enemies** → slow, relentless forward pressure

This variation allows encounters to mix different ranged enemy roles while still using the same core AI architecture.

---

### 💣 Grenade System
Added grenade logic for ranged enemies with the `GrenadePerk`.

Features include:
- Grenade cooldown
- Minimum and maximum throw distances
- Knowledge window (enemy can still throw using recent target knowledge)
- Ballistic launch toward the player’s position
- Visual throw setup with secondary weapon / grenade model swaps
- Timed explosion window so the player can react

This gives ranged enemies area denial and pressure tools without making grenades feel unfair.

---

### 🧬 Enemy Range Behavior Perks
Enemy Range now supports three behavioral perks:

#### CoverPerk
Controls how the enemy interacts with cover:
- `None`
- `TakeCoverOnce`
- `Reposition`

#### UnstoppablePerk
Makes the enemy act like a forward-pushing ranged threat:
- Ignores normal cover behavior
- Keeps advancing like a “terminator”
- Better suited for higher-health enemy variants

#### GrenadePerk
Enables grenade-throwing behavior as an additional tactical tool.

These perks make it possible to create multiple ranged enemy personalities without rewriting the core system.

---

### 🎞️ Range Animation, IK & Weapon Visuals
A large amount of work also went into the ranged enemy presentation layer.

This included:
- Setting up ranged animation states
- Weapon-specific IK handling
- Secondary weapon model usage for animation purposes
- Grenade model toggling during throw animation
- Weapon model enable/disable flow during combat actions

This was important because ranged enemies require much more visual state coordination than melee enemies:
- Aim pose
- Shoot pose
- Secondary weapon swap
- Grenade throw setup
- Cover / advance transitions

The result is a much more readable and polished ranged combatant.

---

## ⚠ Problems Encountered
- Ranged combat required much more tactical logic than melee enemies.
- Distance-only enemy behavior felt too simplistic and unfair.
- Cover logic could easily become unstable or jittery if enemies constantly re-evaluated positions.
- Without target memory, enemies behaved too “all-knowing” or too forgetful.
- Ranged animation and weapon visuals required more complex coordination than melee.
- Grenades needed to feel threatening without becoming unavoidable.
- Several animation issues appeared when different weapons and poses were used:
  - Hands did not always correctly align with the weapon
  - Aiming animations could break depending on weapon type
  - Grenade and secondary weapon poses sometimes conflicted with the base animation rig

---

## ✅ Solutions
- Added a shared `EnemyPerception` system with target memory and visibility checks.
- Separated ranged behavior into dedicated states.
- Moved cover evaluation and reservation into `EnemyCoverController`.
- Introduced perk-based behavior variation instead of duplicating enemy classes.
- Added smart aim logic with lock time and aim tolerance.
- Built grenade throwing with strict range / cooldown / knowledge rules.
- Coordinated animations, IK, and weapon model swapping for ranged combat readability.
- Implemented **Inverse Kinematics (IK)** for weapon handling and aiming:
  - Weapon IK targets are assigned per weapon
  - IK can be **enabled or disabled depending on the animation state**
  - Ensures the enemy hands properly align with weapons during aiming, shooting, and grenade actions

---

## 🚀 Result
- A fully functional new enemy archetype: **Enemy Range**
- Shared perception system now improves all enemy AI architecture
- Tactical cover-based ranged behavior
- Smart aiming and projectile combat
- Grenade-capable ranged enemies
- Configurable perk-based enemy personalities
- Much stronger foundation for future enemy variants and combat encounters

This is one of the biggest AI milestones in the project so far.

---

## 📈 Engineering Takeaways
- Ranged enemies need a different decision model than melee enemies — simply adding bullets is not enough.
- A reusable perception system is a major architectural upgrade for enemy AI.
- Cover behavior should be separated from combat state logic to stay maintainable.
- Tactical behavior feels better when enemies commit to positions instead of constantly re-evaluating.
- Animation and visual state coordination becomes significantly more important for ranged combatants.
- Perk systems are a strong way to create variety without duplicating architecture.

---

## ➡ Next Steps
- Build a new major enemy archetype: **Enemy Boss**
- Set up the boss model, rig, and animation pipeline
- Design a dedicated boss state machine
- Implement boss-specific abilities and attack patterns
- Build the full gameplay/visual combat loop for the boss encounter