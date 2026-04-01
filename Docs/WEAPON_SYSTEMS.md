# Weapon systems (GDD-aligned split)

Project Z uses two code areas that work together; this avoids duplicate “source of truth” confusion.

## `Assets/Scripts/NewWeaponSystem/`

- **Role:** Runtime combat for the equipped weapon: firing, reload, `WeaponData` ScriptableObjects, `WeaponManager`, `BaseWeapon` subclasses (rifle, pistol, etc.).
- **Authoritative for:** Damage application path tied to the active weapon instance.

## `Assets/Scripts/Weapon/`

- **Role:** Meta-progression and catalog: `WeaponMasteryManager`, `WeaponClassBuffConfig`, `WeaponCatalog`, mastery XP tables.
- **Authoritative for:** Per-weapon mastery XP and handling multipliers (GDD Section 2), applied through `BaseWeapon.ApplyBuffMultipliers`.

## Rule

When changing **how a gun shoots**, edit **NewWeaponSystem** and `WeaponData`. When changing **mastery curves or class buffs**, edit **Weapon** configs and `WeaponMasteryManager`. Competitive scaling of mastery handling uses `masteryHandlingStrength` (see `COMPETITIVE_INTEGRITY_PASS.md`).
