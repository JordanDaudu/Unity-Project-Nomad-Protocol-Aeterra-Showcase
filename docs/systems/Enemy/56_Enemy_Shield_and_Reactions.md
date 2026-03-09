---
title: "Enemy Shield & Reactions"
summary: "Shield durability reduction, hit reactions, and bullet-driven enemy feedback hooks (shield first, then enemy)."
order: 56
status: "In Development"
tags: ["Enemy", "Combat", "Shield"]
last_updated: "2026-03-05"
---

## 🧭 Overview
Some melee variants include defensive behavior via `EnemyShield`.
Bullet collision resolves:
1) Shield (if hit collider is shield)
2) Else enemy (parent lookup)

This ensures shield durability absorbs bullets before health is reduced.

## 🎯 Purpose
Support melee variants that feel tankier and create readable combat feedback:
- bullets chip shield durability
- shield breaking changes threat profile (gameplay tuning extension point)

## 🧠 Design Philosophy
- Shield is a separate component, often on a child collider.
- Bullet hit logic stays simple and order-dependent.

## 📦 Core Responsibilities
**Does**
- `EnemyShield` exposes durability reduction API (`ReduceDurability()`)
- `Bullet` prioritizes `EnemyShield` on the hit collider
- `Enemy.GetHit()` handles base health decrement and battle-mode entry

**Does NOT**
- Implement damage numbers/health UI (not in scope yet)

## 🧱 Key Components
Classes
- `EnemyShield` (`code/Enemy/EnemyShield.cs`)
- `Bullet` (`code/Bullet.cs`) collision logic
- `Enemy` (`code/Enemy/Enemy.cs`) base hit behavior

## 🔄 Execution Flow
1. Bullet collides
2. Bullet spawns impact FX and returns itself to pool
3. Bullet checks `collision.gameObject.GetComponent<EnemyShield>()`
   - if found: `ReduceDurability()` and return
4. Else bullet finds `Enemy` via `GetComponentInParent<Enemy>()`
   - calls `GetHit()`
   - calls `DeathImpact()` with travel-direction impulse

## 🔗 Dependencies
**Depends On**
- Bullet collision setup (shield collider must receive collision)

**Used By**
- Shield melee variants

## ⚠ Constraints & Assumptions
- Shield must be on the collider that is actually hit (bullet checks the direct hit object for `EnemyShield`).

## 📈 Scalability & Extensibility
- Add shield break states (visual cracks, disable collider, swap chase speed) without changing bullet logic.

## ✅ Development Status
In Development
