---
title: "Systems Doc Conventions"
summary: "House rules for how we document and evolve systems docs."
order: 1
status: "Stable"
tags: ["Systems", "Docs", "Conventions"]
last_updated: "2026-02-18"
---

## 🧭 Overview
Every system doc follows the same template so you can scan quickly and avoid “where was that implemented?” moments.

## 🎯 Purpose
A system doc must be anchored to:
- **A concrete code surface** (scripts/assets)
- **A concrete runtime behavior** (what happens in play mode)

## 🧠 Design Philosophy
When describing design, keep it true to the current implementation:
- You can explain **why** something is built a certain way
- You must not claim systems exist if they are not in code yet

## 📦 Core Responsibilities
Always include:
- **Does** (what this system owns)
- **Does NOT** (explicit boundaries)

## 🧱 Key Components
List the real scripts and assets:
- `ClassName` — responsibilities
- `ScriptableObject` assets if relevant
- Generated code (e.g. `InputSystem_Actions`) may be referenced, but not treated as “custom code”

## 🔄 Execution Flow
Write the runtime flow in terms of Unity lifecycle and triggers:
- Awake / Start / Update
- Input events
- Collision events
- Animation events
- Pool get/return

## 🔗 Dependencies
Split into:
- **Unity components** required (Animator, CharacterController, Rig, etc.)
- **Other systems** in this project (e.g. Weapon Controller depends on ObjectPool)

## ⚠ Constraints & Assumptions
Document the sharp edges you currently rely on (Inspector setup, expected layers, etc.).

## 📈 Scalability & Extensibility
Only describe extension points that are clearly compatible with the current architecture.

## ✅ Development Status
Use one of:
- Stable
- In Development
- Prototype
- Needs Refactor

## 📝 Notes
Good place for:
- Inspector setup checklists
- Animator parameter names
- Layer/tag expectations
- Known issues / TODOs (only if based on real code behavior)
