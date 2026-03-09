---
title: "Camera Manager (Cinemachine Distance)"
summary: "A persistent singleton that smoothly changes Cinemachine camera distance based on weapon configuration."
order: 23
status: "In Development"
tags: ["Player", "Camera", "Cinemachine"]
last_updated: "2026-02-18"
---

## 🧭 Overview
`CameraManager` is a `DontDestroyOnLoad` singleton that manages **Cinemachine camera distance** using `CinemachinePositionComposer.CameraDistance`. It exposes a single method `ChangeCameraDistance(float)` which other systems call.

## 🎯 Purpose
Let weapons affect tactical visibility by changing camera distance per weapon:
- Rifles can increase distance (more awareness).
- Close-range weapons can decrease distance (tighter view).

## 🧠 Design Philosophy
- Centralize camera distance changes in one place (avoid multiple systems directly editing Cinemachine components).
- Smooth transitions using `Lerp` in `Update()`.

Trade-off: singleton for simplicity during early iteration.

## 📦 Core Responsibilities
**Does**
- Cache references to `CinemachineCamera` and `CinemachinePositionComposer`.
- Interpolate `CameraDistance` toward `targetCameraDistance` when enabled.
- Persist across scenes.

**Does NOT**
- Handle camera look-ahead (that is `PlayerAim.cameraTarget` logic).
- Handle camera rotation rules, shake, or other camera effects.

## 🧱 Key Components
Classes
- `CameraManager` (`Scripts/CameraManager.cs`)

Unity components (required)
- `CinemachineCamera` (child)
- `CinemachinePositionComposer` (on the Cinemachine camera)

## 🔄 Execution Flow
1. `Awake()`
   - Singleton setup + `DontDestroyOnLoad`
   - Cache Cinemachine components
2. `Update()`
   - If `canChangeCameraDistance` false → no-op
   - If difference from target exceeds threshold → lerp current distance toward target
3. External call: `ChangeCameraDistance(distance)` sets the target

## 🔗 Dependencies
**Depends On**
- Cinemachine packages (`Unity.Cinemachine` namespace).
- Scene setup: a `CinemachineCamera` child with a `CinemachinePositionComposer`.

**Used By**
- `PlayerWeaponController.EquipWeapon()` calls `CameraManager.Instance.ChangeCameraDistance(currentWeapon.cameraDistance)`.

## ⚠ Constraints & Assumptions
- `canChangeCameraDistance` must be enabled in Inspector for changes to apply.
- `targetCameraDistance` is not initialized from current camera distance (so first change depends on scene defaults).
- Uses a small threshold to prevent micro-adjustment jitter.

## 📈 Scalability & Extensibility
- Add methods for other camera properties (FOV, damping) only when needed.
- If you introduce multiple camera rigs, you’ll need a routing strategy (not implemented yet).

## ✅ Development Status
In Development

## 📝 Notes
Related devlog:
- Devlog 06 – Camera integration (weapon-driven distance)
