# Nomad Protocol: AETERRA — Showcase Repository (Unity / C#)

🌐 **Project site / devlogs:** https://nomad-protocol-aeterra.onrender.com/  
💼 **LinkedIn:** https://www.linkedin.com/in/jordan-daudu-cpp-python-java/  
✉️ **Email:** jordandaudu@gmail.com  

---

This repository is a **public showcase version** of my Unity project **Nomad Protocol: AETERRA**.

It intentionally contains **only curated, safe-to-share content** (primarily **C# scripts** and **documentation/devlogs**) and does **not** include the full Unity project, art/audio assets, scenes, prefabs, or other files that would allow cloning and running the complete game.

**Status:** Actively in development 🚧

---

## Why a Showcase Repo?

I’m building a commercial-quality Unity project and want recruiters/peers to be able to:
- review my **engineering approach and code quality**
- understand the **systems architecture**
- see **devlogs + implementation notes**
…without publishing the full asset pipeline or the entire playable project.

---

## What’s Included

- **C# gameplay systems** (selected scripts)
- **Docs / devlogs** (design notes, system write-ups)
- **Media used in documentation** (images/GIFs/videos used in devlogs)

---

## What’s Not Included (by design)

To keep this repo lightweight and non-exploitable, it does **not** include:

- Unity **Assets** (models, textures, audio, VFX, animations, etc.)
- **Scenes**, **prefabs**, materials, ScriptableObject data, etc.
- Unity **.meta** files and other project metadata
- Any proprietary content required to build/run the full game

> As a result, this repo is **not expected to open as a playable Unity project** on its own.

---

## Repository Structure

- `code/` — Curated C# scripts (gameplay systems, AI, combat, utilities)
- `docs/` — Devlogs + technical documentation
- Root `README.md` — Overview and navigation

---

## Notable Systems (High-Level)

Depending on the current snapshot of the showcase, you may find work related to:
- Player combat and weapons (pickup/inventory/reload/shoot flow)
- Pooled projectiles and impact FX
- Enemy AI (melee + ranged archetypes), navigation, combat timing via animation events
- Visual variety/polish systems (randomized enemy looks/weapons, effects, etc.)

---

## Keeping the Showcase in Sync

The public repo is maintained via a **sync script** from the private project that:
- copies only approved folders/files (scripts/docs/media)
- blocks risky Unity file types (assets, scenes, prefabs, `.meta`, etc.)
- can optionally mirror the destination to avoid stale files

This ensures the showcase stays up-to-date without exposing non-public content.

---

## License

Code in this repository is provided for review/portfolio purposes.  
If you want to reuse anything, please contact me first.
