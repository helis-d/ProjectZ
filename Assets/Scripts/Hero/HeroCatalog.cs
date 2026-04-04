using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Hero
{
    /// <summary>
    /// Runtime catalog for hero definitions.
    /// If no authored Resources asset exists, the project falls back to a canonical
    /// in-memory roster so profile sync and hero selection still work in prototype scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroCatalog", menuName = "ProjectZ/Hero Catalog")]
    public class HeroCatalog : ScriptableObject
    {
        private static HeroCatalog _instance;

        public static HeroCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<HeroCatalog>("HeroCatalog");
                    if (_instance == null)
                        _instance = CreateRuntimeFallbackCatalog();
                }

                return _instance;
            }
        }

        [SerializeField] private HeroData[] _heroes;

        private Dictionary<string, HeroData> _lookup;

        public HeroData GetById(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
                return null;

            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(heroId.Trim().ToLowerInvariant(), out HeroData hero);
            return hero;
        }

        public HeroData[] GetAll() => _heroes ?? Array.Empty<HeroData>();

        public void InitializeRuntimeHeroes(HeroData[] heroes)
        {
            _heroes = heroes ?? Array.Empty<HeroData>();
            _lookup = null;
            BuildLookup();
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, HeroData>(StringComparer.OrdinalIgnoreCase);
            HeroData[] heroes = GetAll();
            for (int i = 0; i < heroes.Length; i++)
            {
                HeroData hero = heroes[i];
                if (hero != null && !string.IsNullOrWhiteSpace(hero.heroId))
                    _lookup[hero.heroId.Trim().ToLowerInvariant()] = hero;
            }
        }

        private static HeroCatalog CreateRuntimeFallbackCatalog()
        {
            HeroCatalog catalog = CreateInstance<HeroCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.InitializeRuntimeHeroes(new[]
            {
                CreateHero("volt", "Volt", "The Disruptor", HeroRole.Disruptor, UltimateAbilityId.SystemFailure, "System Failure"),
                CreateHero("jacob", "Jacob", "The Anchor", HeroRole.Anchor, UltimateAbilityId.SiegeBreaker, "Siege Breaker"),
                CreateHero("silvia", "Silvia", "The Buffer", HeroRole.Support, UltimateAbilityId.OverdriveCore, "Overdrive Core"),
                CreateHero("sai", "Sai", "The Duelist", HeroRole.Duelist, UltimateAbilityId.BladeDance, "Blade Dance"),
                CreateHero("helix", "Helix", "The Intel", HeroRole.Intel, UltimateAbilityId.OneWayMirror, "One-Way Mirror"),
                CreateHero("lagrange", "Lagrange", "The Flanker", HeroRole.Stalker, UltimateAbilityId.QuantumRewind, "Quantum Rewind"),
                CreateHero("sentinel", "Sentinel", "The Support", HeroRole.Support, UltimateAbilityId.Panopticon, "Panopticon"),
                CreateHero("sector", "Sector", "The Controller", HeroRole.Controller, UltimateAbilityId.DoomsdayCharge, "Doomsday Charge"),
                CreateHero("samuel", "Samuel", "The Gambler", HeroRole.Disruptor, UltimateAbilityId.BloodPact, "Blood Pact"),
                CreateHero("jielda", "Jielda", "The Hunter", HeroRole.Hunter, UltimateAbilityId.SpiritWolves, "Spirit Wolves"),
                CreateHero("zauhll", "Zauhll", "The Stalker", HeroRole.Stalker, UltimateAbilityId.VoidWalk, "Void Walk"),
                CreateHero("kant", "Kant", "The Thief", HeroRole.Thief, UltimateAbilityId.Echo, "Echo"),
                CreateHero("marcus20", "Marcus 2.0", "The Acrobat", HeroRole.Acrobat, UltimateAbilityId.GrappleStrike, "Grapple Strike")
            });
            return catalog;
        }

        private static HeroData CreateHero(
            string heroId,
            string heroName,
            string heroTitle,
            HeroRole role,
            UltimateAbilityId ultimateId,
            string ultimateName)
        {
            HeroData hero = CreateInstance<HeroData>();
            hero.hideFlags = HideFlags.HideAndDontSave;
            hero.heroId = heroId;
            hero.heroName = heroName;
            hero.heroTitle = heroTitle;
            hero.role = role;
            hero.gameplayRole = role.ToString();
            hero.ultimateId = ultimateId;
            hero.ultimateName = ultimateName;
            hero.ultimateChargePerKill = 15f;
            hero.ultimateChargePerAssist = 10f;
            return hero;
        }
    }
}
