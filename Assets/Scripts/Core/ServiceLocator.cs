using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Core
{
    /// <summary>
    /// Service Locator for Dependency Injection.
    /// Replaces Singletons and provides a safe, loosely coupled way to request Managers.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, MonoBehaviour> _services = new Dictionary<Type, MonoBehaviour>();

        /// <summary>
        /// Registers a service to the global state.
        /// Existing Singleton instances can call this in Awake() to transition safely.
        /// </summary>
        public static void Register<T>(T service) where T : MonoBehaviour
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                _services[typeof(T)] = service;
            }
            else
            {
                _services[typeof(T)] = service; // Allow overriding for test mock scenarios
            }
        }

        /// <summary>
        /// Gets an implicitly required service. Logs error and gracefully returns null if missing.
        /// </summary>
        public static T Get<T>() where T : MonoBehaviour
        {
            if (_services.TryGetValue(typeof(T), out MonoBehaviour service))
            {
                return (T)service;
            }
            
            // Zero Damage Fallback: Try to find it in the scene if not registered (lazy load via Unity Object cache)
            T fallback = UnityEngine.Object.FindFirstObjectByType<T>();
            if (fallback != null)
            {
                Register(fallback);
                return fallback;
            }

            Debug.LogWarning($"[ServiceLocator] Service {typeof(T).Name} NOT found. Dependency missing.");
            return null;
        }

        /// <summary>
        /// Clears all services (Useful on Scene Unload or Server Restart).
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
