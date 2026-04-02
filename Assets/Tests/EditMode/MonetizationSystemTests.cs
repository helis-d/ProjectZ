using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ProjectZ.Tests
{
    public class MonetizationSystemTests
    {
        [Test]
        public void DefaultProfile_StartsWithStarterRoster_AndRankedReady()
        {
            Type profileType = GetGameplayType("ProjectZ.Network.PlayerProfileData");
            Type monetizationServiceType = GetGameplayType("ProjectZ.Monetization.MonetizationService");

            object profile = InvokeStatic(profileType, "CreateDefault", "TestPilot");
            int ownedHeroCount = (int)InvokeStatic(monetizationServiceType, "CountOwnedHeroes", profile);
            bool rankedReady = (bool)InvokeStatic(monetizationServiceType, "CanEnterRanked", profile);
            int commandCredits = (int)GetFieldValue(profile, "commandCredits");

            Assert.AreEqual(5, ownedHeroCount);
            Assert.IsTrue(rankedReady);
            Assert.AreEqual(1000, commandCredits);
            Assert.IsTrue(ListContains((IEnumerable)GetFieldValue(profile, "ownedHeroIds"), "volt"));
            Assert.IsTrue(ListContains((IEnumerable)GetFieldValue(profile, "ownedHeroIds"), "helix"));
        }

        [Test]
        public void HeroUnlock_SpendsCommandCredits_AndGrantsOwnership()
        {
            Type profileType = GetGameplayType("ProjectZ.Network.PlayerProfileData");
            Type monetizationServiceType = GetGameplayType("ProjectZ.Monetization.MonetizationService");

            object profile = InvokeStatic(profileType, "CreateDefault", "Unlocker");
            SetFieldValue(profile, "commandCredits", 1000);
            SetFieldValue(profile, "currency", 1000);

            object result = InvokeStatic(monetizationServiceType, "TryUnlockHero", profile, "lagrange");

            Assert.IsTrue((bool)GetPropertyValue(result, "Succeeded"));
            Assert.AreEqual(400, GetFieldValue(profile, "commandCredits"));
            Assert.IsTrue(ListContains((IEnumerable)GetFieldValue(profile, "ownedHeroIds"), "lagrange"));
        }

        [Test]
        public void CosmeticPurchase_SpendsPremiumCurrency_AndAddsEntitlement()
        {
            Type profileType = GetGameplayType("ProjectZ.Network.PlayerProfileData");
            Type monetizationServiceType = GetGameplayType("ProjectZ.Monetization.MonetizationService");
            Type catalogType = GetGameplayType("ProjectZ.Monetization.MonetizationCatalog");

            object profile = InvokeStatic(profileType, "CreateDefault", "Collector");
            SetFieldValue(profile, "zCore", 1000);

            object catalog = GetStaticPropertyValue(catalogType, "Instance");
            object offer = InvokeInstance(catalog, "GetById", "launch_weaponskin_vandal_firstlight");
            object result = InvokeStatic(monetizationServiceType, "TryPurchaseOffer", profile, offer, false, false, false);

            Assert.IsTrue((bool)GetPropertyValue(result, "Succeeded"));
            Assert.AreEqual(100, GetFieldValue(profile, "zCore"));
            Assert.IsTrue(ListContains((IEnumerable)GetFieldValue(profile, "ownedCosmeticIds"), "weaponskin_vandal_firstlight"));
        }

        private static Type GetGameplayType(string fullName)
        {
            Assembly gameplayAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            Assert.NotNull(gameplayAssembly, "Assembly-CSharp was not loaded.");

            Type type = gameplayAssembly.GetType(fullName);
            Assert.NotNull(type, $"{fullName} could not be resolved.");
            return type;
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, $"Method '{methodName}' could not be resolved on {type.FullName}.");
            return method.Invoke(null, args);
        }

        private static object InvokeInstance(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method, $"Method '{methodName}' could not be resolved on {instance.GetType().FullName}.");
            return method.Invoke(instance, args);
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property, $"Property '{propertyName}' could not be resolved.");
            return property.GetValue(instance);
        }

        private static object GetStaticPropertyValue(Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(property, $"Property '{propertyName}' could not be resolved on {type.FullName}.");
            return property.GetValue(null);
        }

        private static object GetFieldValue(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' could not be resolved on {instance.GetType().FullName}.");
            return field.GetValue(instance);
        }

        private static void SetFieldValue(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' could not be resolved on {instance.GetType().FullName}.");
            field.SetValue(instance, value);
        }

        private static bool ListContains(IEnumerable values, string expected)
        {
            foreach (object value in values)
            {
                if (string.Equals(value as string, expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
