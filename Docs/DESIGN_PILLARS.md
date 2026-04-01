# Project Z — Design pillars and shipping focus

This document locks **product intent** for implementation and live-ops. Code references: [`GameModeProductInfo.cs`](../Assets/Scripts/GameMode/GameModeProductInfo.cs).

## Pillars (what the game optimizes for)

1. **Competitive integrity** — Server-authoritative rules, readable losses, minimal “I lost to the UI” moments.
2. **Tactical clarity** — Information and space matter; effects should have telegraph and counterplay where possible.
3. **High-skill gunplay** — Low TTK and precise shooting remain the baseline; hero kits **modify** engagements, they do not replace aim.
4. **Round-based stakes** — Economy and objective (Sphere) create structured decisions each round.

## Mastery and hero abilities (scope)

- **Mastery** is a live match progression system. For ranked integrity, handling bonuses can be **scaled down** via `WeaponMasteryManager` (see `masteryHandlingStrength`) without removing the feature.
- **Ultimates** are high-impact. Global denial (HUD, comms, vision) requires explicit **counterplay** and tuning; see [`COMPETITIVE_INTEGRITY_PASS.md`](COMPETITIVE_INTEGRITY_PASS.md).

## Game mode tiers

| Tier | Modes | Intent |
|------|--------|--------|
| **Primary** | `RankedGameMode` | Main competitive ruleset; balance and UX priority. |
| **Secondary** | `FastFightMode` | Shorter sessions; still economy + plant/defuse. |
| **Experimental** | `DuelChaosMode`, `SoloTournamentMode` | Sandbox / alternate fantasies; not the shipping competitive core until explicitly promoted. |

Menus and matchmaking should label **Experimental** modes clearly and may hide them behind a flag in production builds.

## Single source of truth

- **Mode numbers** (round counts, caps, win conditions): [`README.md`](../README.md) “Canonical mode rules” + code constants in `RankedGameMode`, `FastFightMode`.
- **Ultimate runtime wiring**: [`HERO_ULTIMATE_PIPELINE.md`](HERO_ULTIMATE_PIPELINE.md) and [`HeroData.cs`](../Assets/Scripts/Hero/HeroData.cs).
