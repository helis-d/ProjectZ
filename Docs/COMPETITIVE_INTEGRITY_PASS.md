# Competitive integrity pass — mastery and ultimates

Design-facing notes and an actionable change list. Implementation hooks: `WeaponMasteryManager.masteryHandlingStrength`, product tiers in [`DESIGN_PILLARS.md`](DESIGN_PILLARS.md).

## Problems (design)

1. **Mastery snowball** — In a low-TTK game, live buffs to ADS, reload, move, and fire rate make the winning duelist mechanically stronger in the same match.
2. **Unreadable losses** — Ultimates that remove HUD, comms, or global vision can feel like “the game broke” instead of “I was outplayed.”
3. **Long-lived cross-round effects** — Abilities that persist multiple rounds (e.g. wallbang zones) distort economy and planning unless isolated in their own balance budget.

## Counterplay principles (target state)

| Principle | Example |
|-----------|---------|
| Telegraph | Audio/visual cue before effect; duration visible to victim. |
| Counterplay | Positioning, utility, or agent-specific answers. |
| Information fairness | Avoid deleting **all** HUD; prefer partial or directional debuffs. |
| Scope limits | Time-box globals; avoid stacking with plant/defuse critical moments without review. |

## Change list (prioritized)

### Mastery (code + tuning)

- [x] **Handling strength slider** — `WeaponMasteryManager`: `masteryHandlingStrength` blends configured buffs toward identity (1.0 = full GDD, 0 = no handling buff from mastery).
- [ ] **Playtest default** — Try `0.35`–`0.5` for ranked-like modes; keep `1.0` for arcade or secondary modes if desired.
- [ ] **Separate modes** — Optionally drive strength from `BaseGameMode` or mode tier (`GameModeProductInfo`) in a follow-up.

### Ultimates (design + content)

- [ ] **Volt / System Failure** — Replace full HUD wipe with partial UI noise, directional blur, or shorter duration; keep comms if possible or add “mute immunity” item.
- [ ] **Jacob / Siege Breaker** — If multi-round duration stays, add visible zone limits, cooldown across rounds, or cap wallbang rounds.
- [ ] **Audit remaining globals** — Panopticon, Overdrive Core, etc.: ensure minimap/ally callouts remain readable.

### Process

- [ ] Add each change to patch notes with **player-facing** wording (“what you can do about it”).
- [ ] Re-run [`GAME_DESIGN_AUDIT.md`](GAME_DESIGN_AUDIT.md) after major balance passes.
