---
title: "Object Pooling"
summary: "A global, prefab-agnostic pooling system for bullets, pickups, and VFX."
order: 10
status: "In Development"
tags: ["Core", "Performance", "Pooling"]
last_updated: "2026-02-18"
---

## 🧭 Overview
A global pooling service implemented as a `DontDestroyOnLoad` singleton. It manages pooled instances per prefab (`prefab → queue`) and is used by:
- `Bullet` (projectiles)
- `PickupWeapon` / `PickupAmmo` (world pickups)
- Bullet impact VFX (spawned in `Bullet.CreateImpactFx()`)

## 🎯 Purpose
Avoid frequent `Instantiate()` / `Destroy()` during gameplay (combat bursts, rapid pickup spawning) and provide a reusable performance foundation.

## 🧠 Design Philosophy
- **Prefab-agnostic**: any prefab can be pooled (not bullet-only).
- **Lazy creation**: a pool is created the first time a prefab is requested.
- **Simple return contract**: pooled instances must know which original prefab they belong to.

Trade-off: this is intentionally a simple singleton pool (no interfaces/DI yet) to keep iteration fast.

## 📦 Core Responsibilities
**Does**
- Maintain `Dictionary<GameObject, Queue<GameObject>>` per prefab.
- Prewarm pools for configured prefabs on `Start()`.
- Spawn additional instances when a pool runs empty (with a warning).
- Return objects to the correct pool via `PooledObject.originalPrefab`.
- Support delayed return (coroutine) to avoid same-frame edge cases.

**Does NOT**
- Reset object state beyond `SetActive(false)` / parent assignment.
- Validate `PooledObject` existence (current code assumes it exists).
- Handle scene-placed objects automatically (see constraints).

## 🧱 Key Components
Classes
- `ObjectPool` (`Scripts/Object Pool/ObjectPool.cs`)
  - Singleton pool manager and API (`GetObject()`, `ReturnObjectToPool()`).
- `PooledObject` (`Scripts/Object Pool/PooledObject.cs`)
  - Stores `originalPrefab` so the pool can enqueue correctly.

## 🔄 Execution Flow
1. `ObjectPool.Awake()` sets up singleton + `DontDestroyOnLoad`.
2. `ObjectPool.Start()` pre-initializes pools for:
   - `weaponPickup`
   - `ammoPickup`
3. Any system requests an instance via `GetObject(prefab)`:
   - If pool doesn’t exist → `initializeNewPool(prefab)`.
   - If pool empty → `CreateNewObject(prefab)` (warns).
   - Dequeue, activate, detach from pool parent.
4. Systems return an instance via `ReturnObjectToPool(go, delay)`:
   - Optional delay coroutine.
   - `ReturnToPool()` looks up `PooledObject.originalPrefab`.
   - Deactivates, parents under pool object, enqueues.

## 🔗 Dependencies
**Depends On**
- Unity: `MonoBehaviour`, coroutines.
- `PooledObject` component to route returns.

**Used By**
- `PlayerWeaponController` (bullets + weapon pickups)
- `Bullet` (impact FX)
- `PickupWeapon` / `PickupAmmo` (return pickup objects)

## ⚠ Constraints & Assumptions
- Any object returned **must** have `PooledObject` with `originalPrefab` set.
  - Pool-created instances are safe (pool adds `PooledObject`).
  - Scene-placed objects returned to pool can break if they don’t have `PooledObject`.
- `poolSize` is global for all prefabs (no per-prefab config yet).
- No null guards when returning: missing `PooledObject` will throw.

## 📈 Scalability & Extensibility
Safe extensions that match current architecture:
- Per-prefab pool size configuration (dictionary of settings).
- Optional “reset hook” interface (e.g., `IPoolable.OnGet/OnReturn`) **only if you actually add it later**.
- Editor tooling to prewarm pools based on scene references.

## ✅ Development Status
In Development

## 📝 Notes
Related devlogs:
- Devlog 06 – bullet pooling foundation
- Devlog 07 – refactor to global pool
- Devlog 08 – pooling enforced for pickups and VFX
