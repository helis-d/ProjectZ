# Hero and ultimate runtime pipeline

## How ultimates run today

1. Each player selects a [`HeroData`](../Assets/Scripts/Hero/HeroData.asset) (`PlayerHeroController._selectedHero`).
2. `HeroData.ultimateId` maps to an [`UltimateAbilityId`](../Assets/Scripts/Hero/HeroData.cs) enum value.
3. At runtime, [`PlayerHeroController.EquipUltimate`](../Assets/Scripts/Player/PlayerHeroController.cs) resolves the ability via **`GetComponent<T>()` on the player prefab** — see `ResolveAttachedUltimate`. The matching `UltimateAbility` component **must exist on the spawned player** (typically [`Assets/Player.prefab`](../Assets/Player.prefab)).

So: **the authoritative chain is `ultimateId` → component on `Player`, not `HeroData.ultimateAbilityPrefab`.**

## `HeroData.ultimateAbilityPrefab`

The `ultimateAbilityPrefab` field is **optional**. It is useful for:

- Documentation and future spawn-based setups.
- Designer tooling (validate that the correct prefab variant exists).

If it is left empty (`None`), ultimates still work **when** the player prefab includes the correct script for that hero’s `ultimateId`.

## Authoring checklist (per hero)

- [ ] `HeroData` has correct `ultimateId`, name, and charge values.
- [ ] `Player` prefab (or skin variant) includes the matching `UltimateAbility` subclass component.
- [ ] Network behaviours are registered on the same `NetworkObject` as `PlayerHeroController`.
- [ ] (Optional) Assign `ultimateAbilityPrefab` to a reference prefab for documentation parity.

## Shipping recommendation

For a vertical slice, fully verify **3–4 heroes** end-to-end (data + prefab components + VFX/SFX) before expanding the roster.
