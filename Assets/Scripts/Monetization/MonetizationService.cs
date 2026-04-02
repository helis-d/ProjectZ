using System;
using System.Collections.Generic;
using ProjectZ.Network;
using UnityEngine;

namespace ProjectZ.Monetization
{
    public enum MonetizationPurchaseStatus
    {
        Success = 0,
        InvalidProfile = 1,
        InvalidOffer = 2,
        AlreadyOwned = 3,
        InsufficientFunds = 4,
        OfferNotActive = 5,
        UnknownHero = 6
    }

    [Serializable]
    public struct MonetizationPurchaseResult
    {
        public MonetizationPurchaseStatus Status { get; }
        public string OfferId { get; }
        public string ContentId { get; }
        public string Message { get; }
        public MonetizationCurrencyType CurrencyType { get; }
        public int AmountSpent { get; }

        public bool Succeeded => Status == MonetizationPurchaseStatus.Success;

        public MonetizationPurchaseResult(
            MonetizationPurchaseStatus status,
            string offerId,
            string contentId,
            string message,
            MonetizationCurrencyType currencyType = MonetizationCurrencyType.None,
            int amountSpent = 0)
        {
            Status = status;
            OfferId = offerId;
            ContentId = contentId;
            Message = message;
            CurrencyType = currencyType;
            AmountSpent = amountSpent;
        }
    }

    /// <summary>
    /// Fair-play monetization rules.
    /// This layer keeps competitive access readable and prevents premium shortcuts from
    /// turning into power or rank advantages.
    /// </summary>
    public static class MonetizationService
    {
        public const string CommandCreditsDisplayName = "Komuta Kredisi";
        public const string PremiumCurrencyDisplayName = "Z-Core";
        public const int StartingCommandCredits = 1000;
        public const int StartingZCore = 0;
        public const int RankedRequiredOwnedHeroes = 5;
        public const int DefaultHeroUnlockPrice = 600;

        private static readonly string[] _starterHeroIds =
        {
            "volt",
            "jacob",
            "silvia",
            "sai",
            "helix"
        };

        private static readonly string[] _allHeroIds =
        {
            "volt",
            "jacob",
            "silvia",
            "sai",
            "helix",
            "lagrange",
            "sentinel",
            "sector",
            "samuel",
            "jielda",
            "zauhll",
            "kant",
            "marcus20"
        };

        private static readonly Dictionary<string, string> _heroDisplayNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["volt"] = "Volt",
            ["jacob"] = "Jacob",
            ["silvia"] = "Silvia",
            ["sai"] = "Sai",
            ["helix"] = "Helix",
            ["lagrange"] = "Lagrange",
            ["sentinel"] = "Sentinel",
            ["sector"] = "Sector",
            ["samuel"] = "Samuel",
            ["jielda"] = "Jielda",
            ["zauhll"] = "Zauhll",
            ["kant"] = "Kant",
            ["marcus20"] = "Marcus 20"
        };

        public static IReadOnlyList<string> GetStarterHeroIds() => _starterHeroIds;
        public static IReadOnlyList<string> GetAllHeroIds() => _allHeroIds;
        public static int TotalHeroCount => _allHeroIds.Length;

        public static bool IsStarterHero(string heroId)
        {
            return ContainsValue(_starterHeroIds, NormalizeId(heroId));
        }

        public static bool IsKnownHeroId(string heroId)
        {
            return ContainsValue(_allHeroIds, NormalizeId(heroId));
        }

        public static string GetHeroDisplayName(string heroId)
        {
            string normalizedHeroId = NormalizeId(heroId);
            if (string.IsNullOrEmpty(normalizedHeroId))
                return "Unknown Hero";

            if (_heroDisplayNames.TryGetValue(normalizedHeroId, out string heroName))
                return heroName;

            return normalizedHeroId;
        }

        public static void NormalizeProfile(PlayerProfileData profile)
        {
            if (profile == null)
                return;

            if (profile.commandCredits <= 0 && profile.currency > 0)
                profile.commandCredits = profile.currency;

            profile.commandCredits = Mathf.Max(0, profile.commandCredits);
            profile.zCore = Mathf.Max(0, profile.zCore);
            profile.currency = profile.commandCredits;

            profile.ownedHeroIds ??= new List<string>();
            profile.ownedCosmeticIds ??= new List<string>();
            profile.ownedOfferIds ??= new List<string>();

            NormalizeOwnedIds(profile.ownedHeroIds);
            NormalizeOwnedIds(profile.ownedCosmeticIds);
            NormalizeOwnedIds(profile.ownedOfferIds);

            for (int i = 0; i < _starterHeroIds.Length; i++)
            {
                string starterHero = _starterHeroIds[i];
                if (!profile.ownedHeroIds.Contains(starterHero))
                    profile.ownedHeroIds.Add(starterHero);
            }

            string normalizedSelectedHero = NormalizeId(profile.selectedHero);
            if (IsKnownHeroId(normalizedSelectedHero))
            {
                if (!profile.ownedHeroIds.Contains(normalizedSelectedHero))
                    profile.ownedHeroIds.Add(normalizedSelectedHero);

                profile.selectedHero = normalizedSelectedHero;
            }
            else
            {
                profile.selectedHero = _starterHeroIds[0];
            }
        }

        public static bool OwnsHero(PlayerProfileData profile, string heroId)
        {
            if (profile == null)
                return false;

            NormalizeProfile(profile);
            return profile.ownedHeroIds.Contains(NormalizeId(heroId));
        }

        public static int CountOwnedHeroes(PlayerProfileData profile)
        {
            if (profile == null)
                return 0;

            NormalizeProfile(profile);
            return profile.ownedHeroIds.Count;
        }

        public static bool CanEnterRanked(PlayerProfileData profile)
        {
            return CountOwnedHeroes(profile) >= RankedRequiredOwnedHeroes;
        }

        public static MonetizationPurchaseResult TryUnlockHero(PlayerProfileData profile, string heroId)
        {
            string normalizedHeroId = NormalizeId(heroId);
            if (profile == null)
                return new MonetizationPurchaseResult(MonetizationPurchaseStatus.InvalidProfile, null, normalizedHeroId, "Gecerli bir profil bulunamadi.");

            NormalizeProfile(profile);

            if (!IsKnownHeroId(normalizedHeroId))
            {
                return new MonetizationPurchaseResult(
                    MonetizationPurchaseStatus.UnknownHero,
                    $"hero_unlock_{normalizedHeroId}",
                    normalizedHeroId,
                    "Tanimlanmamis hero unlock talebi.");
            }

            if (profile.ownedHeroIds.Contains(normalizedHeroId))
            {
                return new MonetizationPurchaseResult(
                    MonetizationPurchaseStatus.AlreadyOwned,
                    $"hero_unlock_{normalizedHeroId}",
                    normalizedHeroId,
                    "Bu hero zaten sende acik.");
            }

            MonetizationCatalogOffer heroOffer = MonetizationCatalog.Instance.GetHeroUnlockOffer(normalizedHeroId);
            int price = heroOffer != null ? heroOffer.price : DefaultHeroUnlockPrice;

            if (!TrySpendCurrency(profile, MonetizationCurrencyType.CommandCredits, price))
            {
                return new MonetizationPurchaseResult(
                    MonetizationPurchaseStatus.InsufficientFunds,
                    heroOffer != null ? heroOffer.offerId : $"hero_unlock_{normalizedHeroId}",
                    normalizedHeroId,
                    "Hero acmak icin yeterli Komuta Kredisi yok.",
                    MonetizationCurrencyType.CommandCredits);
            }

            profile.ownedHeroIds.Add(normalizedHeroId);
            NormalizeOwnedIds(profile.ownedHeroIds);

            return new MonetizationPurchaseResult(
                MonetizationPurchaseStatus.Success,
                heroOffer != null ? heroOffer.offerId : $"hero_unlock_{normalizedHeroId}",
                normalizedHeroId,
                $"{GetHeroDisplayName(normalizedHeroId)} kalici olarak acildi.",
                MonetizationCurrencyType.CommandCredits,
                price);
        }

        public static MonetizationPurchaseResult TryPurchaseOffer(
            PlayerProfileData profile,
            MonetizationCatalogOffer offer,
            bool alphaEntitlementsEnabled = false,
            bool season2Enabled = false,
            bool eventContentEnabled = false)
        {
            if (profile == null)
                return new MonetizationPurchaseResult(MonetizationPurchaseStatus.InvalidProfile, null, null, "Gecerli bir profil bulunamadi.");

            if (offer == null)
                return new MonetizationPurchaseResult(MonetizationPurchaseStatus.InvalidOffer, null, null, "Gecerli bir teklif bulunamadi.");

            NormalizeProfile(profile);

            if (!IsOfferActive(offer, alphaEntitlementsEnabled, season2Enabled, eventContentEnabled))
            {
                return new MonetizationPurchaseResult(
                    MonetizationPurchaseStatus.OfferNotActive,
                    offer.offerId,
                    offer.contentId,
                    "Bu teklif su an aktif degil.");
            }

            if (offer.offerType == MonetizationOfferType.HeroUnlock)
                return TryUnlockHero(profile, offer.contentId);

            if (OwnsOffer(profile, offer))
            {
                return new MonetizationPurchaseResult(
                    MonetizationPurchaseStatus.AlreadyOwned,
                    offer.offerId,
                    offer.contentId,
                    "Bu teklif zaten satin alinmis.");
            }

            if (!TrySpendCurrency(profile, offer.priceCurrency, offer.price))
            {
                return new MonetizationPurchaseResult(
                    MonetizationPurchaseStatus.InsufficientFunds,
                    offer.offerId,
                    offer.contentId,
                    "Bu satin alim icin yeterli bakiye yok.",
                    offer.priceCurrency);
            }

            GrantOffer(profile, offer);
            return new MonetizationPurchaseResult(
                MonetizationPurchaseStatus.Success,
                offer.offerId,
                offer.contentId,
                $"{offer.displayName} basariyla satin alindi.",
                offer.priceCurrency,
                offer.price);
        }

        public static bool OwnsOffer(PlayerProfileData profile, MonetizationCatalogOffer offer)
        {
            if (profile == null || offer == null)
                return false;

            NormalizeProfile(profile);
            string normalizedContentId = NormalizeId(offer.contentId);

            return offer.offerType switch
            {
                MonetizationOfferType.WeaponSkin or
                MonetizationOfferType.PlayerCard or
                MonetizationOfferType.Spray or
                MonetizationOfferType.Charm => profile.ownedCosmeticIds.Contains(normalizedContentId),
                MonetizationOfferType.HeroUnlock => profile.ownedHeroIds.Contains(normalizedContentId),
                _ => profile.ownedOfferIds.Contains(NormalizeId(offer.offerId))
            };
        }

        public static bool IsOfferActive(
            MonetizationCatalogOffer offer,
            bool alphaEntitlementsEnabled = false,
            bool season2Enabled = false,
            bool eventContentEnabled = false)
        {
            if (offer == null)
                return false;

            return offer.availability switch
            {
                MonetizationOfferAvailability.Launch => true,
                MonetizationOfferAvailability.AlphaOnly => alphaEntitlementsEnabled,
                MonetizationOfferAvailability.Season2 => season2Enabled,
                MonetizationOfferAvailability.Event => eventContentEnabled,
                _ => false
            };
        }

        private static void GrantOffer(PlayerProfileData profile, MonetizationCatalogOffer offer)
        {
            string normalizedOfferId = NormalizeId(offer.offerId);
            string normalizedContentId = NormalizeId(offer.contentId);

            switch (offer.offerType)
            {
                case MonetizationOfferType.WeaponSkin:
                case MonetizationOfferType.PlayerCard:
                case MonetizationOfferType.Spray:
                case MonetizationOfferType.Charm:
                    profile.ownedCosmeticIds.Add(normalizedContentId);
                    NormalizeOwnedIds(profile.ownedCosmeticIds);
                    break;
                case MonetizationOfferType.Bundle:
                case MonetizationOfferType.AlphaFounderPack:
                case MonetizationOfferType.BattlePass:
                    profile.ownedOfferIds.Add(normalizedOfferId);
                    NormalizeOwnedIds(profile.ownedOfferIds);
                    break;
            }
        }

        private static bool TrySpendCurrency(PlayerProfileData profile, MonetizationCurrencyType currencyType, int amount)
        {
            if (profile == null || amount < 0)
                return false;

            switch (currencyType)
            {
                case MonetizationCurrencyType.CommandCredits:
                    if (profile.commandCredits < amount)
                        return false;

                    profile.commandCredits -= amount;
                    profile.currency = profile.commandCredits;
                    return true;
                case MonetizationCurrencyType.ZCore:
                    if (profile.zCore < amount)
                        return false;

                    profile.zCore -= amount;
                    return true;
                default:
                    return false;
            }
        }

        private static void NormalizeOwnedIds(List<string> ids)
        {
            if (ids == null)
                return;

            HashSet<string> uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = ids.Count - 1; i >= 0; i--)
            {
                string normalizedId = NormalizeId(ids[i]);
                if (string.IsNullOrEmpty(normalizedId) || !uniqueIds.Add(normalizedId))
                    ids.RemoveAt(i);
                else
                    ids[i] = normalizedId;
            }
        }

        private static bool ContainsValue(string[] source, string value)
        {
            if (source == null || string.IsNullOrEmpty(value))
                return false;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.Equals(source[i], value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeId(string rawValue)
        {
            return string.IsNullOrWhiteSpace(rawValue) ? string.Empty : rawValue.Trim().ToLowerInvariant();
        }
    }
}
