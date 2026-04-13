using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectZ.Tests
{
    public class CombatMathTests
    {
        private Type _playerHealthType;
        private Type _hitboxZoneType;
        private Type _hitboxResultType;
        
        [SetUp]
        public void Setup()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            _playerHealthType = assembly?.GetType("ProjectZ.Player.PlayerHealth");
            _hitboxZoneType = assembly?.GetType("ProjectZ.Combat.HitboxZone");
            _hitboxResultType = assembly?.GetType("ProjectZ.Combat.HitboxResult");
        }

        [Test]
        public void TakeDamage_NoArmor_ReducesHealth()
        {
            Assert.NotNull(_playerHealthType, "PlayerHealth type not found.");
            
            GameObject go = new GameObject("TestPlayer");
            var healthComponent = go.AddComponent(_playerHealthType);
            
            // ResetHealth()
            InvokeMethod(healthComponent, "ResetHealth");
            
            // Set armor to 0 explicitly just in case
            SetSyncVarValue(healthComponent, "CurrentArmor", 0f);
            
            // TakeDamage(25f, -1)
            InvokeMethod(healthComponent, "TakeDamage", 25f, -1);
            
            float currentHealth = GetSyncVarValue(healthComponent, "CurrentHealth");
            Assert.AreEqual(75f, currentHealth);
            
            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_WithArmor_AbsorbsDamageAndPreservesHealth()
        {
            GameObject go = new GameObject("TestPlayer");
            var healthComponent = go.AddComponent(_playerHealthType);
            
            InvokeMethod(healthComponent, "ResetHealth");
            SetSyncVarValue(healthComponent, "CurrentArmor", 50f);
            
            InvokeMethod(healthComponent, "TakeDamage", 30f, -1);
            
            float currentArmor = GetSyncVarValue(healthComponent, "CurrentArmor");
            float currentHealth = GetSyncVarValue(healthComponent, "CurrentHealth");
            
            // 30 damage should be eaten entirely by armor
            Assert.AreEqual(20f, currentArmor);
            Assert.AreEqual(100f, currentHealth);
            
            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_ExceedingArmor_BleedsToHealth()
        {
            GameObject go = new GameObject("TestPlayer");
            var healthComponent = go.AddComponent(_playerHealthType);
            
            InvokeMethod(healthComponent, "ResetHealth");
            SetSyncVarValue(healthComponent, "CurrentArmor", 25f);
            
            // Deal 60 damage -> 25 armor broken, 35 bleed to health (100 - 35 = 65)
            InvokeMethod(healthComponent, "TakeDamage", 60f, -1);
            
            float currentArmor = GetSyncVarValue(healthComponent, "CurrentArmor");
            float currentHealth = GetSyncVarValue(healthComponent, "CurrentHealth");
            bool isDead = GetSyncVarBool(healthComponent, "IsDead");
            
            Assert.AreEqual(0f, currentArmor);
            Assert.AreEqual(65f, currentHealth);
            Assert.IsFalse(isDead);
            
            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_Lethal_SetsIsDeadTrue()
        {
            GameObject go = new GameObject("TestPlayer");
            var healthComponent = go.AddComponent(_playerHealthType);
            
            InvokeMethod(healthComponent, "ResetHealth");
            SetSyncVarValue(healthComponent, "CurrentArmor", 50f);
            
            // Deal 200 damage -> 50 armor broken, 150 bleed to health (100 - 150 = -50) -> death
            InvokeMethod(healthComponent, "TakeDamage", 200f, -1);
            
            float currentHealth = GetSyncVarValue(healthComponent, "CurrentHealth");
            bool isDead = GetSyncVarBool(healthComponent, "IsDead");
            
            Assert.AreEqual(0f, currentHealth);
            Assert.IsTrue(isDead);
            
            GameObject.DestroyImmediate(go);
        }

        // --- Helper Reflection Methods ---

        private void InvokeMethod(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, $"Method '{methodName}' not found.");
            method.Invoke(instance, args);
        }

        private float GetSyncVarValue(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' not found.");
            object syncVar = field.GetValue(instance);
            
            PropertyInfo prop = syncVar.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(prop, $"Property 'Value' not found on SyncVar.");
            return (float)prop.GetValue(syncVar);
        }

        private bool GetSyncVarBool(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' not found.");
            object syncVar = field.GetValue(instance);
            
            PropertyInfo prop = syncVar.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(prop, $"Property 'Value' not found on SyncVar.");
            return (bool)prop.GetValue(syncVar);
        }

        private void SetSyncVarValue(object instance, string fieldName, float value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, $"Field '{fieldName}' not found.");
            object syncVar = field.GetValue(instance);
            
            PropertyInfo prop = syncVar.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(prop, $"Property 'Value' not found on SyncVar.");
            prop.SetValue(syncVar, value);
        }
    }
}
