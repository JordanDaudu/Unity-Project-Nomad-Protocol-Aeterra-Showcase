# Devlog 07 – Global Object Pooling & Data-Driven Weapons (ScriptableObjects)

Date: 2026-02-15

## 🎯 Goal
Refactor core combat support systems to be more scalable and production-ready by:
- Converting object pooling into a global, reusable system for any prefab
- Moving weapon configuration into ScriptableObjects for data-driven tuning
- Introducing a Weapon constructor that builds runtime weapon instances from WeaponData assets

---

## 🧠 Design Approach
This phase focused on separating **static configuration** from **runtime state** and removing hardcoded, weapon-specific logic.

Key principles:
- **Pool any prefab**, not just bullets, to support future VFX, pickups, enemy projectiles, and interactables.
- Treat weapon definitions as **data assets**, not code changes.
- Use a constructor-based approach so a weapon instance can be built cleanly from a WeaponData source.
- Keep the system extensible: adding a new weapon should become “create a new asset,” not “rewrite logic.”

---

## 🏗 Implementation

### ♻ Global Object Pool
Refactored the object pool into a generic pooling system:
- Supports pooling **any prefab**, not only bullets.
- Uses a dictionary mapping `prefab → queue of instances`.
- Automatically initializes a pool when a prefab is requested for the first time.
- Creates additional instances if a pool runs empty (with a warning to increase pool size when needed).
- Added a small delayed return option to avoid edge cases where an object is returned during the same frame it’s still being used.
- Each pooled instance tracks its original prefab via a lightweight component so it can always return to the correct pool.

This converts pooling from a “bullet optimization” into a reusable engine feature.

---

### 🧾 ScriptableObject Weapon Data
Introduced a `WeaponData` ScriptableObject as the authoritative source of weapon configuration.

WeaponData now contains the full weapon identity:
- Magazine and reserve ammo settings
- Fire mode configuration (semi/auto)
- Fire rate and bullets-per-shot
- Burst configuration (optional)
- Spread configuration
- Weapon specifics (type, reload/equip speed)
- Gun distance and camera distance tuning per weapon

This allows balancing and iteration directly in the Inspector without changing code.

---

### 🧩 Weapon Constructor (Runtime Instance from Data)
Implemented a constructor in the runtime `Weapon` class that initializes a weapon instance from a `WeaponData` asset.

This creates a clean separation:
- WeaponData = static tuning and defaults (design-time)
- Weapon = runtime state (ammo consumption, cooldown timing, current firing behavior)

This approach supports:
- Multiple instances of the same weapon type
- Future systems like upgrades/mods, rarity rolls, or attachments
- Cleaner initialization and less controller-side setup logic

---

### ✅ Weapon Defaults Created
Created default `WeaponData` assets for all current weapons:
- Pistol
- Revolver
- Auto Rifle
- Shotgun
- Rifle

This establishes a consistent baseline and makes future weapon expansion straightforward.

---

## ⚠ Problems Encountered
- Bullet-only pooling didn’t scale to VFX, pickups, and other gameplay objects.
- Weapon configuration was becoming code-heavy and hard to tune.
- Weapon initialization logic risked spreading across controllers and visuals.
- Needed a reliable way to connect pooled instances back to their source prefab.

---

## ✅ Solutions
- Refactored pooling into a prefab-agnostic system using a dictionary of queues.
- Added per-instance tracking of the original prefab to ensure correct returns.
- Moved weapon configuration into ScriptableObjects for data-driven iteration.
- Introduced a constructor to build runtime weapons from WeaponData assets.
- Created default WeaponData assets for all current weapon types.

---

## 🚀 Result
- A reusable global object pool capable of handling any prefab type.
- Weapons are now fully data-driven via ScriptableObjects.
- Weapon runtime instances initialize cleanly from WeaponData assets.
- All five current weapons have consistent, editable defaults.
- The project is now set up for faster iteration, balancing, and expansion.

---

## 📈 Engineering Takeaways
- Generic systems (like pooling) pay off immediately once the game grows beyond a single use case.
- ScriptableObjects are ideal for separating tuning from logic in Unity projects.
- Constructor-based initialization reduces controller complexity and enforces clean runtime state creation.
- Data-driven design dramatically improves iteration speed and scalability.

---

## ➡ Next Steps
### Interaction System Foundations
- Implement closest-interactable detection
- Create an interaction base structure (inheritance / interface-driven design)
- Convert weapon pickup into an interaction
- Drop current weapon as a pickup item
- Add ammo box pickup interaction
