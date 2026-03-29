using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// Runtime weapon data catalog.
    /// Tüm WeaponData ScriptableObject'lerini weaponId ile indeksler.
    /// FishNet AudioClip serileştirme sorununu çözmek için SyncVar<WeaponData> yerine
    /// SyncVar<string> (weaponId) kullanılır ve bu katalogdan çözümlenir.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "ProjectZ/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject
    {
        private static WeaponCatalog _instance;
        public static WeaponCatalog Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<WeaponCatalog>("WeaponCatalog");
                return _instance;
            }
        }

        [Header("All Weapons")]
        [SerializeField] private WeaponData[] _weapons;

        private Dictionary<string, WeaponData> _lookup;

        /// <summary>
        /// weaponId'ye göre WeaponData döndürür. Bulunamazsa null.
        /// </summary>
        public WeaponData GetById(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return null;

            if (_lookup == null) BuildLookup();
            
            _lookup.TryGetValue(weaponId, out WeaponData data);
            return data;
        }

        /// <summary>
        /// Tüm kayıtlı silahları döndürür.
        /// </summary>
        public WeaponData[] GetAll() => _weapons;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, WeaponData>();
            if (_weapons == null) return;

            foreach (WeaponData w in _weapons)
            {
                if (w != null && !string.IsNullOrEmpty(w.weaponId))
                    _lookup[w.weaponId] = w;
            }
        }

        private void OnEnable()
        {
            _lookup = null; // Editor'da yeniden yüklendiğinde lookup'ı sıfırla
        }
    }
}
