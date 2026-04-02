using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ProjectZ.Tests
{
    public class CompetitiveRankSystemTests
    {
        [Test]
        public void MinimumRating_MapsToBaslangicOne()
        {
            Type rankSystemType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankSystem");
            Type rankBandType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankBand");
            object rankInfo = InvokeStatic(rankSystemType, "GetRankInfo", 1000);

            object band = GetPropertyValue(rankInfo, "Band");
            int division = (int)GetPropertyValue(rankInfo, "Division");
            string displayName = (string)GetPropertyValue(rankInfo, "DisplayName");

            Assert.AreEqual(Enum.Parse(rankBandType, "Baslangic"), band);
            Assert.AreEqual(1, division);
            Assert.AreEqual("\u0042a\u015flang\u0131\u00e7 I", displayName);
        }

        [Test]
        public void Baron_UsesFourthDivision()
        {
            Type rankSystemType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankSystem");
            Type rankBandType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankBand");
            object baron = Enum.Parse(rankBandType, "Baron");

            int baronFourFloor = (int)InvokeStatic(rankSystemType, "GetFloorRating", baron, 4);
            object rankInfo = InvokeStatic(rankSystemType, "GetRankInfo", baronFourFloor);

            Assert.AreEqual(baron, GetPropertyValue(rankInfo, "Band"));
            Assert.AreEqual(4, GetPropertyValue(rankInfo, "Division"));
            Assert.AreEqual("IV", GetPropertyValue(rankInfo, "DivisionDisplayName"));
        }

        [Test]
        public void Prestij_TopDivision_RemainsPrestijFour()
        {
            Type rankSystemType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankSystem");
            Type rankBandType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankBand");
            object prestij = Enum.Parse(rankBandType, "Prestij");

            int floor = (int)InvokeStatic(rankSystemType, "GetFloorRating", prestij, 4);
            object rankInfo = InvokeStatic(rankSystemType, "GetRankInfo", floor + 275);

            Assert.AreEqual(prestij, GetPropertyValue(rankInfo, "Band"));
            Assert.AreEqual(4, GetPropertyValue(rankInfo, "Division"));
        }

        [Test]
        public void WinAgainstStrongerOpponent_GainsMoreRating()
        {
            Type rankSystemType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankSystem");
            Type performanceType = GetGameplayType("ProjectZ.GameMode.RankedMatchPerformance");

            object evenMatch = Activator.CreateInstance(performanceType, 1600, 1600, true, 16, 10, 5, 13, 10, false, 24);
            object strongerOpponent = Activator.CreateInstance(performanceType, 1600, 2000, true, 16, 10, 5, 13, 10, false, 24);

            int evenDelta = (int)InvokeStatic(rankSystemType, "CalculateRatingDelta", evenMatch);
            int strongerDelta = (int)InvokeStatic(rankSystemType, "CalculateRatingDelta", strongerOpponent);

            Assert.Greater(strongerDelta, evenDelta);
        }

        [Test]
        public void ApplyRatingDelta_NeverDropsBelowMinimum()
        {
            Type rankSystemType = GetGameplayType("ProjectZ.GameMode.CompetitiveRankSystem");
            int minimumRating = (int)GetFieldValue(rankSystemType, "MinimumRating");

            int newRating = (int)InvokeStatic(rankSystemType, "ApplyRatingDelta", minimumRating, -999);

            Assert.AreEqual(minimumRating, newRating);
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

        private static object GetPropertyValue(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property, $"Property '{propertyName}' could not be resolved.");
            return property.GetValue(instance);
        }

        private static object GetFieldValue(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field, $"Field '{fieldName}' could not be resolved on {type.FullName}.");
            return field.GetValue(null);
        }
    }
}
