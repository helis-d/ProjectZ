using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Monetization
{
    public enum MonetizationCurrencyType
    {
        None = 0,
        CommandCredits = 1,
        ZCore = 2
    }

    public enum MonetizationOfferType
    {
        HeroUnlock = 0,
        WeaponSkin = 1,
        PlayerCard = 2,
        Spray = 3,
        Charm = 4,
        Bundle = 5,
        AlphaFounderPack = 6,
        BattlePass = 7
    }

    public enum MonetizationOfferAvailability
    {
        Launch = 0,
        AlphaOnly = 1,
        Season2 = 2,
        Event = 3
    }

    [Serializable]
    public class MonetizationCatalogOffer
    {
        public string offerId;
        public string displayName;
        [TextArea] public string description;
        public MonetizationOfferType offerType;
        public MonetizationCurrencyType priceCurrency;
        public int price;
        public string contentId;
        public bool directPurchase = true;
        public bool featured;
        public MonetizationOfferAvailability availability = MonetizationOfferAvailability.Launch;
        public int sortOrder;
    }

    /// <summary>
    /// Live-service catalog for the cosmetic store and hero unlock economy.
    /// If no authored asset exists in Resources, the game falls back to a curated
    /// runtime catalog so prototype and CI builds still have stable monetization data.
    /// </summary>
    [CreateAssetMenu(fileName = "MonetizationCatalog", menuName = "ProjectZ/Monetization Catalog")]
    public class MonetizationCatalog : ScriptableObject
    {
        private static MonetizationCatalog _instance;

        public static MonetizationCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MonetizationCatalog>("MonetizationCatalog");
                    if (_instance == null)
                        _instance = CreateRuntimeFallbackCatalog();
                }

                return _instance;
            }
        }

        [Header("Store Offers")]
        [SerializeField] private MonetizationCatalogOffer[] _offers;

        private Dictionary<string, MonetizationCatalogOffer> _lookup;

        public MonetizationCatalogOffer GetById(string offerId)
        {
            if (string.IsNullOrWhiteSpace(offerId))
                return null;

            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(offerId, out MonetizationCatalogOffer offer);
            return offer;
        }

        public MonetizationCatalogOffer GetHeroUnlockOffer(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
                return null;

            MonetizationCatalogOffer[] offers = GetAllOffers();
            for (int i = 0; i < offers.Length; i++)
            {
                MonetizationCatalogOffer offer = offers[i];
                if (offer != null &&
                    offer.offerType == MonetizationOfferType.HeroUnlock &&
                    string.Equals(offer.contentId, heroId, StringComparison.OrdinalIgnoreCase))
                {
                    return offer;
                }
            }

            return null;
        }

        public MonetizationCatalogOffer[] GetAllOffers() => _offers ?? Array.Empty<MonetizationCatalogOffer>();

        public void InitializeRuntimeOffers(MonetizationCatalogOffer[] offers)
        {
            _offers = offers ?? Array.Empty<MonetizationCatalogOffer>();
            _lookup = null;
            BuildLookup();
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, MonetizationCatalogOffer>(StringComparer.OrdinalIgnoreCase);

            MonetizationCatalogOffer[] offers = GetAllOffers();
            for (int i = 0; i < offers.Length; i++)
            {
                MonetizationCatalogOffer offer = offers[i];
                if (offer != null && !string.IsNullOrWhiteSpace(offer.offerId))
                    _lookup[offer.offerId] = offer;
            }
        }

        private static MonetizationCatalog CreateRuntimeFallbackCatalog()
        {
            MonetizationCatalog catalog = CreateInstance<MonetizationCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;

            List<MonetizationCatalogOffer> offers = new List<MonetizationCatalogOffer>();
            int nextSortOrder = 0;

            IReadOnlyList<string> heroIds = MonetizationService.GetAllHeroIds();
            for (int i = 0; i < heroIds.Count; i++)
            {
                string heroId = heroIds[i];
                if (MonetizationService.IsStarterHero(heroId))
                    continue;

                string heroName = MonetizationService.GetHeroDisplayName(heroId);
                offers.Add(new MonetizationCatalogOffer
                {
                    offerId = $"hero_unlock_{heroId}",
                    displayName = $"{heroName} Hero Unlock",
                    description = $"{heroName} ajanini Komuta Kredisi ile kalici olarak acar.",
                    offerType = MonetizationOfferType.HeroUnlock,
                    priceCurrency = MonetizationCurrencyType.CommandCredits,
                    price = MonetizationService.DefaultHeroUnlockPrice,
                    contentId = heroId,
                    directPurchase = true,
                    featured = false,
                    availability = MonetizationOfferAvailability.Launch,
                    sortOrder = nextSortOrder++
                });
            }

            offers.Add(CreateLaunchOffer(
                "launch_weaponskin_vandal_firstlight",
                "Firstlight Vandal",
                "Launch sezonuna ozel, temiz ve oyuncu dostu bir rifle skin.",
                MonetizationOfferType.WeaponSkin,
                MonetizationCurrencyType.ZCore,
                900,
                "weaponskin_vandal_firstlight",
                nextSortOrder++,
                true));

            offers.Add(CreateLaunchOffer(
                "launch_playercard_founders_signal",
                "Founder's Signal Card",
                "Project Z erken donem kimligini tasiyan oyuncu karti.",
                MonetizationOfferType.PlayerCard,
                MonetizationCurrencyType.ZCore,
                250,
                "playercard_founders_signal",
                nextSortOrder++));

            offers.Add(CreateLaunchOffer(
                "launch_spray_hold_the_site",
                "Hold The Site Spray",
                "Objective savunmasini kutlayan launch spreyi.",
                MonetizationOfferType.Spray,
                MonetizationCurrencyType.ZCore,
                175,
                "spray_hold_the_site",
                nextSortOrder++));

            offers.Add(CreateLaunchOffer(
                "launch_charm_quantum_key",
                "Quantum Key Charm",
                "Silahlara takilan hafif premium launch charm.",
                MonetizationOfferType.Charm,
                MonetizationCurrencyType.ZCore,
                225,
                "charm_quantum_key",
                nextSortOrder++));

            offers.Add(new MonetizationCatalogOffer
            {
                offerId = "alpha_founder_pack",
                displayName = "Alpha Founder Pack",
                description = "Kapali alpha donemine ozel destek paketi. Sadece kozmetik ve prestij icerir.",
                offerType = MonetizationOfferType.AlphaFounderPack,
                priceCurrency = MonetizationCurrencyType.ZCore,
                price = 1800,
                contentId = "alpha_founder_pack",
                directPurchase = true,
                featured = true,
                availability = MonetizationOfferAvailability.AlphaOnly,
                sortOrder = nextSortOrder++
            });

            offers.Add(new MonetizationCatalogOffer
            {
                offerId = "season2_battlepass_premium",
                displayName = "Season 2 Battle Pass",
                description = "Sadece kozmetik odakli premium sezon bileti.",
                offerType = MonetizationOfferType.BattlePass,
                priceCurrency = MonetizationCurrencyType.ZCore,
                price = 1000,
                contentId = "season2_battlepass_premium",
                directPurchase = true,
                featured = true,
                availability = MonetizationOfferAvailability.Season2,
                sortOrder = nextSortOrder++
            });

            catalog.InitializeRuntimeOffers(offers.ToArray());
            return catalog;
        }

        private static MonetizationCatalogOffer CreateLaunchOffer(
            string offerId,
            string displayName,
            string description,
            MonetizationOfferType offerType,
            MonetizationCurrencyType priceCurrency,
            int price,
            string contentId,
            int sortOrder,
            bool featured = false)
        {
            return new MonetizationCatalogOffer
            {
                offerId = offerId,
                displayName = displayName,
                description = description,
                offerType = offerType,
                priceCurrency = priceCurrency,
                price = price,
                contentId = contentId,
                directPurchase = true,
                featured = featured,
                availability = MonetizationOfferAvailability.Launch,
                sortOrder = sortOrder
            };
        }
    }
}
