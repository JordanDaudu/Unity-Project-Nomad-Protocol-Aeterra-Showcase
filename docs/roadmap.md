# Project Roadmap

This project is a long-term solo software engineering portfolio project built in Unity (C#). Its primary purpose is to demonstrate system architecture, modular design, scalability, and documented engineering decisions.

## Phase 0 – Project Bootstrap

* Create Unity project
* Set up folder structure
* Initialize GitHub repository
* Create documentation folders and placeholders
* Devlog 00

## Phase 1 – Input & Character Controller

* Design input system architecture
* Implement character movement and rotation
* Integrate basic animations (idle, walk, run, fire)
* Devlog 01
* System doc: input-system.md

## Phase 2 – Camera & Aim

* Setup camera and aim decomposition
* Smooth character rotation (lerp/slerp)
* Bullet creation and targeting
* Devlog 02
* System doc: camera-system.md

## Phase 3 – Weapon System

* Modular weapon class design
* Weapon slots, pickup, equip, and reload
* Fire modes: single, auto, burst
* Object pooling for bullets
* Devlog 03
* System doc: weapon-system.md

## Phase 4 – Interaction System

* Closest interactable detection
* Weapon pickup/drop
* Ammo box pickup
* Vehicle entry/exit integration
* Devlog 04
* System doc: interaction-system.md

## Phase 5 – Enemy AI

* Melee enemy FSM (idle, move, chase, attack, dead)
* Ranged enemy FSM (cover, shoot, grenade, cooldown)
* Boss enemy behaviors and abilities
* Devlog 05
* System doc: ai-system.md

## Phase 6 – Damage System

* Interfaces and hitboxes
* Health controller and friendly fire
* Weapon balancing
* Devlog 06
* System doc: damage-system.md

## Phase 7 – Procedural Level Generation

* System design for automated levels
* Zone limits, object placement, rotation rules
* Devlog 07
* System doc: procedural-generation.md

## Phase 8 – Mission / Quest System

* Mission manager and timed missions
* Hunt missions, deliveries, drop system
* Event-driven progress tracking
* Devlog 08
* System doc: mission-system.md

## Phase 9 – UI System

* Player health, weapon ammo, mission goals
* Main menu, pause menu, and in-game UI
* Event-driven updates
* Devlog 09
* System doc: ui-system.md

## Phase 10 – Vehicles

* Car controls: acceleration, braking, drift
* Vehicle health and camera integration
* Devlog 10
* System doc: vehicle-system.md

## Phase 11 – Audio System

* Background music and SFX
* Centralized audio service
* Event-driven playback
* Devlog 11
* System doc: audio-system.md

## Phase 12 – Polish & Extras

* Enemy variants (random look, corruption, animator overrides)
* Weapon visual polish
* UI polish and animations
* Devlog 12

## Phase 13 – Portfolio & LinkedIn

* Screenshots, GIFs, or short clips for posts
* LinkedIn posts after visible system milestones
* Documentation review for portfolio presentation
