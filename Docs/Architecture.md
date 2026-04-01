# ProjectZ Architecture Overview

> **A16Z Speedrun Technical Review Document** 
> *This document provides a high-level overview of the systems architecture and networking paradigms utilized in ProjectZ.*

## 1. Network Topology (Server-Authoritative)

At the core of ProjectZ is a strict **Server-Authoritative** model designed to handle competitive 5v5 FPS gameplay, completely neutralizing the vast majority of client-side hacks (speedhacks, unauthorized spawning, teleporting).

- **Framework:** FishNet v4
- **Host Model:** Dedicated Headless Linux Servers (Dockerized via Alpine linux)
- **Client Role:** Clients act purely as "dumb terminals" that capture user input (Mouse/Keyboard data) and render the state received from the server.
- **Server Role:** The server alone computes collision logic, economy, round progression, hit registration, and player health.

## 2. Hit Registration & Lag Compensation

To achieve "Valorant-grade" crispness in shooting, the project implements **Client-Side Prediction (CSP)** paired with **Server-Side Rollback (Rewind)** mechanisms.

1. **Prediction:** When a player clicks to fire, the client immediately plays the VFX (Muzzle Flash, Tracers) and Recoil animations so the gunplay feels responsive at 0ms delay.
2. **ServerRpc Dispatch:** The client dispatches a minimal packet (`ProcessFireRoutine`) containing the exact local timestamp of the shot alongside the directional vector.
3. **Rollback Verification:** Upon receiving the RPC, the Server consults its `LagCompensator`. It physically rewinds the hitboxes of all enemy players to exactly where they were at the *(Client Timestamp - Ping Offset)*. 
4. **Validation:** A server-side physics raycast is fired through these historically accurate hitboxes. If a hit is registered, the server applies damage and broadcasts an un-ignorable `ObserverRpc` that instantly updates the victim's health across all connected clients.

## 3. Modularity & Systems Design

ProjectZ avoids giant Monolithic behaviors by separating data from execution.

- **Weapon Data Structures:** Weapons are built using `ScriptableObjects` (e.g., `WeaponData.cs`), allowing designers to easily tweak Fire Rate, Recoil patterns, and Mastery XP buffs without ever opening a code editor.
- **BaseGameMode Base Class:** A polymorphic approach to game modes. `DuelChaosMode`, `RankedGameMode`, and `FastFightMode` all inherit from a highly optimized `BaseGameMode`. The `RoundManager` simply hooks into these inherited event delegates (`OnRoundStart`, `OnPlayerKilled`) rather than using hardcoded conditional spaghetti.

## 4. Current Refactoring Focus (SOLID)

While the project stands at ~10K Lines of Code, the primary area of ongoing technical debt repayment is the `Player` assembly:
- Breaking down large controller scripts (`PlayerController`) into modular components (`PlayerMotor`, `PlayerPredictor`, `PlayerCombat`).
- Preparing the integration of **Dependency Injection (DI)** for managers (Economy, UI, Match State) to increase unit-testability coverage.

**Design / product:** Mode shipping tiers, mastery tuning, and ultimate counterplay are documented in [`DESIGN_PILLARS.md`](DESIGN_PILLARS.md), [`HERO_ULTIMATE_PIPELINE.md`](HERO_ULTIMATE_PIPELINE.md), and [`COMPETITIVE_INTEGRITY_PASS.md`](COMPETITIVE_INTEGRITY_PASS.md). Code: `GameModeProductInfo`, `WeaponMasteryManager.masteryHandlingStrength`.

## 5. Deployment & CI/CD Pipeline

The project ships with inherent containerization:
- **CockroachDB + Nakama** handles authentication and Matchmaking tickets.
- **AWS GameLift / Custom Edge Scaling** spins up the `Dockerfile.server` headless instances dynamically upon successful match composition.

*This structure ensures that the game can horizontally scale from 100 players to 1,000,000 with minimum friction.*
