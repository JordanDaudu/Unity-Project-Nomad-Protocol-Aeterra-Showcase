---
title: "Devlog 09 – Enemy Melee AI, State Machine, Abilities & Death Pipeline"
date: "2026-02-25"
summary: "Implemented full melee enemy AI with NavMesh navigation, state machine, combat abilities, ragdoll physics, and dissolve death effects."
order: 9
---

## 🎯 Goal
Build the first complete enemy combatant (“Enemy Melee”) with:
- NavMesh-based navigation and patrol/chase behavior
- A scalable enemy state machine (Idle/Move/Chase/Attack/Recovery/Dead/Ability)
- Multiple enemy variants (Regular / Shield / Dodge / AxeThrow)
- Animation-driven gameplay hooks (manual movement/rotation + ability timing)
- Ragdoll + death impact + dissolve shader sequence
- Bullet → enemy integrations (damage, shield durability, dodge reaction)

---

## 🧠 Design Approach
This phase treated the enemy as a **modular gameplay system**, not a single script.

Core principles:
- **Inheritance-first**: a base `Enemy` class holds shared behavior; specific enemies (like `EnemyMelee`) extend it.
- **State-driven AI**: decisions and transitions live in states; the enemy “brain” stays readable and extensible.
- **Animation as a driver**: animation events control when to rotate, move, or trigger abilities (instead of guessing in code).
- **Variants via configuration**: enemy behavior branches by type (Shield/Dodge/AxeThrow) while sharing the same core pipeline.
- **Death as a sequence**: disable agent + enable ragdoll + apply impact + dissolve visually, then prepare for reuse (pool-safe).

---

## 🏗 Implementation

### 🧭 AI Navigation Setup (NavMesh)
- Added a `NavMeshAgent` to the enemy.
- Baked a NavMesh surface including Ground + Obstacles layers.
- Implemented:
  - Patrol destinations using waypoints
  - Smooth steering rotation toward `agent.steeringTarget`
  - Chase destination updates with a small throttle (avoids constant SetDestination spam)

Result: enemies patrol naturally and transition into chase when the player enters aggression range.

---

### 🧠 Enemy State Machine Architecture
Implemented a reusable finite state machine:
- `EnemyStateMachine` manages current state and transitions.
- `EnemyState` base provides a consistent lifecycle:
  - `Enter()`, `Update()`, `Exit()`
  - `AnimationTrigger()` for animation event sync
  - `AbilityTrigger()` for ability timing hooks

Melee states included:
- **Idle**: waits `idleTime`, then switches to Move
- **Move**: patrol navigation between points
- **Recovery**: short “decision hub” after actions; chooses next action based on range and cooldowns
- **Chase**: pursues player using NavMeshAgent
- **Attack**: animation-driven attacks with manual movement/rotation when required
- **Ability**: special actions (Axe throw) synchronized by animation events
- **Dead**: disables agent + animator, enables ragdoll + dissolve, then disables interactions later

This gives a clean “AI loop” that reads like behavior design instead of spaghetti conditionals.

---

### 🧬 Enemy Types & Special Behaviors
Created four melee variants:
- **Regular**: standard melee behavior
- **Shield**: shield absorbs bullets until durability breaks; enemy becomes “tankier” until broken
- **Dodge**: can roll to evade, with cooldown and situational checks
- **AxeThrow**: can throw a projectile from range, controlled via cooldown and animation timing

Also introduced **attack types**:
- **Close**
- **Charge**

This allowed the attack system to choose different animations and movement behavior depending on distance.

---

### 🎞 Animator Structure & Blend Trees
A large part of this milestone was building a scalable enemy Animator setup:
- Parameters for states (Idle/Move/Chase/Attack/Recovery)
- Additional parameters for animation selection:
  - Attack indices and recovery indices
  - Chase variants (ex: shielded chase)
  - Slash attack selection via randomized index
- Blend Trees for:
  - Chase variants (regular vs shielded)
  - Recovery variants
  - Attack variants (charge spin vs close slash set)

This created variety without exploding state count.

![Enemy Animator](/devlog-assets/09_4.png)

---

### 🎬 Animation Events as Gameplay Hooks
Implemented `EnemyAnimationEvents` on the animated model to call back into the enemy.
It triggers:
- `AnimationTrigger()` to progress state logic
- Manual movement toggles:
  - Start/stop manual movement
  - Start/stop manual rotation
- `AbilityEvent()` which calls the current state’s `AbilityTrigger()`

This is the key that makes the enemy feel intentional:
- Movement during attacks is controlled by animations, not by guesswork.
- Abilities happen at the exact frame they should.

---

### 🪓 Axe Throw Ability (Projectile + Pooling)
Implemented `EnemyAxe`:
- Rotates visually during flight
- Tracks player briefly for an aim window (`axeAimTimer`)
- Uses rigidbody velocity for motion
- On hit with player or bullet:
  - plays impact FX (pooled)
  - returns axe and FX to pool

This keeps projectiles performant and visually responsive.

---

### 💀 Death Pipeline: Ragdoll → Dissolve
Built a death sequence that combines physics + visuals:

1. **Dead state enters**
   - Animator disabled
   - NavMeshAgent disabled
   - Ragdoll enabled (rigidbodies non-kinematic)
2. **Death impact**
   - Bullet passes impact force through to ragdoll rigidbody at hit point
3. **Dissolve shader**
   - Materials swap to dissolve variants at runtime (only on death)
   - Dissolve value animates using MaterialPropertyBlock (no allocations per frame)
4. **Post-death cleanup**
   - After short delay, ragdoll is frozen and colliders disabled to stop excessive simulation

This sequence gives a high-quality “kill feedback loop” while remaining pool-safe through `ResetForReuse()`.

![Gameplay Death Demo](/devlog-assets/09_3.mp4)

---

### 🧩 Bullet & PlayerWeaponController Integration
To connect the combat loop, two key integrations were added:

**Bullet collision now supports:**
- Shield durability reduction when hitting an `EnemyShield`
- Enemy hit logic (`GetHit`) when hitting the enemy itself
- Death impact force application using bullet velocity direction

**PlayerWeaponController now supports enemy dodge reaction:**
- A raycast along the bullet’s path checks if a Dodge-type enemy is in-line
- If so, it triggers the enemy dodge roll (cooldown gated)
- This is called before spread is applied to keep behavior consistent with the fired shot direction

Result: enemies feel reactive and more “alive”, without needing heavy perception systems yet.

---

## ⚠ Problems Encountered
- Getting NavMeshAgent movement to cooperate with attack animations (root motion vs manual movement).
- Avoiding “sliding” or “drifting” animations during attacks and abilities.
- Managing lots of animation variety without creating an unmaintainable animator graph.
- Keeping death effects performant (material swaps and dissolve) and pool-safe.
- Fast combat interactions (dodge + shield + hit impacts) required careful ordering.
- Some imported animations caused unwanted drifting (root motion translation), making attacks slide across the ground.

---

## ✅ Solutions
- Used animation events to explicitly control manual movement and rotation at the correct frames.
- Added a recovery state as a stable “decision point” between chase/attack/ability.
- Used blend trees + indexed parameters to scale animation variety without state explosion.
- Implemented ragdoll/dissolve as a controlled pipeline, with runtime material swapping only when needed.
- Added pool-safe reset logic so enemies can be reused without broken visuals or disabled components.
- Adjusted imported animations by clearing root transform X/Z to keep attacks in-place when needed.
- Fixed drifting by removing root transform translation (clearing root T X/Z) so animations stay in place when needed.

---

## 🚀 Result
- Enemy Melee AI patrols, chases, attacks, recovers, and dies correctly.
- Multiple enemy variants (Shield/Dodge/AxeThrow) work within the same architecture.
- Attacks include variety (close slashes vs charge spins) with controlled movement/rotation.
- Enemy death feels impactful: ragdoll physics + force + dissolve feedback.
- Bullet → enemy interactions support shield durability, dodge reactions, and hit impacts.
- The system is structured to scale into more enemies and abilities cleanly.

![Animations](/devlog-assets/09_1.mp4)
![Close Attacks Variation](/devlog-assets/09_2.mp4)

---

## 📈 Engineering Takeaways
- Animation events are a powerful “contract” between visuals and gameplay timing.
- A recovery/decision state reduces messy conditional logic and stabilizes AI flow.
- Variant design should extend shared systems rather than fork new enemies per behavior.
- Death pipelines need to be engineered like a feature: physics, visuals, cleanup, and pooling must all align.
- Keeping animations in-place (root transform cleanup) prevents a lot of downstream movement bugs.

---

## ➡ Next Steps – Enemy Variant Polish & Expansion

- Expand enemy variants
- Update enemy FBX model
- Update avatar and refine animation set
- Implement random visual variations (random look system)
- Assign random weapons per enemy instance
- Add random corruption variants (model variation system)
- Introduce Animator Override Controllers per variant
- Implement enemy weapon trail effects
- Refine and rebalance attack data system