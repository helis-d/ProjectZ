using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ProjectZ.Tests
{
    /// <summary>
    /// Guards GDD-adjacent mastery math (no scene required).
    /// </summary>
    public class LevelMultipliersBlendTests
    {
        [Test]
        public void BlendTowardIdentity_StrengthZero_YieldsIdentity()
        {
            Type multipliersType = GetLevelMultipliersType();
            object m = CreateMultipliers(multipliersType, 0.9f, 0.85f, 1.05f, 1.15f, 0.95f);

            object r = BlendTowardIdentity(multipliersType, m, 0f);

            Assert.AreEqual(1f, GetFieldValue(r, "ads"), 0.0001f);
            Assert.AreEqual(1f, GetFieldValue(r, "reload"), 0.0001f);
            Assert.AreEqual(1f, GetFieldValue(r, "move"), 0.0001f);
            Assert.AreEqual(1f, GetFieldValue(r, "fireRate"), 0.0001f);
            Assert.AreEqual(1f, GetFieldValue(r, "draw"), 0.0001f);
        }

        [Test]
        public void BlendTowardIdentity_StrengthOne_PreservesInput()
        {
            Type multipliersType = GetLevelMultipliersType();
            object m = CreateMultipliers(multipliersType, 0.9f, 1f, 1.05f, 1.1f, 1f);

            object r = BlendTowardIdentity(multipliersType, m, 1f);

            Assert.AreEqual(0.9f, GetFieldValue(r, "ads"), 0.0001f);
            Assert.AreEqual(1.05f, GetFieldValue(r, "move"), 0.0001f);
            Assert.AreEqual(1.1f, GetFieldValue(r, "fireRate"), 0.0001f);
        }

        private static Type GetLevelMultipliersType()
        {
            Assembly gameplayAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            Assert.NotNull(gameplayAssembly, "Assembly-CSharp was not loaded.");

            Type multipliersType = gameplayAssembly.GetType("ProjectZ.Weapon.LevelMultipliers");
            Assert.NotNull(multipliersType, "ProjectZ.Weapon.LevelMultipliers could not be resolved.");
            return multipliersType;
        }

        private static object CreateMultipliers(
            Type multipliersType,
            float ads,
            float reload,
            float move,
            float fireRate,
            float draw)
        {
            object multipliers = Activator.CreateInstance(multipliersType);
            SetFieldValue(multipliersType, ref multipliers, "ads", ads);
            SetFieldValue(multipliersType, ref multipliers, "reload", reload);
            SetFieldValue(multipliersType, ref multipliers, "move", move);
            SetFieldValue(multipliersType, ref multipliers, "fireRate", fireRate);
            SetFieldValue(multipliersType, ref multipliers, "draw", draw);
            return multipliers;
        }

        private static object BlendTowardIdentity(Type multipliersType, object multipliers, float strength)
        {
            MethodInfo blendMethod = multipliersType.GetMethod(
                "BlendTowardIdentity",
                BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(blendMethod, "BlendTowardIdentity method could not be resolved.");
            return blendMethod.Invoke(null, new object[] { multipliers, strength });
        }

        private static void SetFieldValue(Type multipliersType, ref object target, string fieldName, float value)
        {
            FieldInfo field = multipliersType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' could not be resolved.");
            field.SetValue(target, value);
        }

        private static float GetFieldValue(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' could not be resolved.");
            return (float)field.GetValue(target);
        }
    }
}
