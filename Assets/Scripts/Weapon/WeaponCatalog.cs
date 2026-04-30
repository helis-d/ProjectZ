using System.Collections.Generic;
using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// Runtime weapon data catalog.
    /// Indexes WeaponData by weaponId. If no authored catalog exists in Resources,
    /// the project falls back to a minimal in-memory catalog so prototype scenes still run.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "ProjectZ/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject
    {
        private static WeaponCatalog _instance;

        public static WeaponData Resolve(string weaponId)
        {
            return Instance != null ? Instance.GetById(weaponId) : null;
        }

        public static WeaponCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<WeaponCatalog>("WeaponCatalog");
                    if (_instance == null)
                        _instance = CreateRuntimeFallbackCatalog();
                }

                return _instance;
            }
        }

        [Header("All Weapons")]
        [SerializeField] private WeaponData[] _weapons;

        private Dictionary<string, WeaponData> _lookup;

        public WeaponData GetById(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return null;

            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(weaponId, out WeaponData data);
            return data;
        }

        public WeaponData[] GetAll() => _weapons;

        public void InitializeRuntimeWeapons(WeaponData[] weapons)
        {
            _weapons = weapons;
            _lookup = null;
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, WeaponData>();
            if (_weapons == null)
                return;

            foreach (WeaponData weapon in _weapons)
            {
                if (weapon != null && !string.IsNullOrEmpty(weapon.weaponId))
                    _lookup[weapon.weaponId] = weapon;
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        private static WeaponCatalog CreateRuntimeFallbackCatalog()
        {
            WeaponCatalog catalog = CreateInstance<WeaponCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.InitializeRuntimeWeapons(new[]
            {
                CreateFallbackWeapon("vandal", WeaponName.Vandal, WeaponType.Rifle, 2900, 40f, 0.11f, 2.5f, 30, 90, 120f, 0.008f),
                CreateFallbackWeapon("pistol_classic", WeaponName.Classic, WeaponType.Pistol, 0, 26f, 0.18f, 1.6f, 12, 36, 80f, 0.01f),
                CreateFallbackKnife()
            });
            return catalog;
        }

        private static WeaponData CreateFallbackWeapon(
            string weaponId,
            WeaponName weaponName,
            WeaponType weaponType,
            int price,
            float damage,
            float fireRate,
            float reloadTime,
            int magazineSize,
            int reserveAmmo,
            float range,
            float spread)
        {
            WeaponData weapon = CreateInstance<WeaponData>();
            weapon.hideFlags = HideFlags.HideAndDontSave;
            weapon.weaponId = weaponId;
            weapon.weaponName = weaponName;
            weapon.weaponType = weaponType;
            weapon.price = price;
            weapon.damage = damage;
            weapon.fireRate = fireRate;
            weapon.reloadTime = reloadTime;
            weapon.magazineSize = magazineSize;
            weapon.maxReserveAmmo = reserveAmmo;
            weapon.range = range;
            weapon.bulletSpread = spread;
            weapon.penetrationPower = 100f;
            weapon.rightHandPositionOffset = new Vector3(0.18f, -0.18f, 0.42f);
            weapon.rightHandRotationOffset = Vector3.zero;
            weapon.leftHandPositionOffset = new Vector3(-0.08f, -0.04f, 0.2f);
            weapon.leftHandRotationOffset = Vector3.zero;
            weapon.adsPositionOffset = new Vector3(0f, -0.14f, 0.15f);
            weapon.adsRotationOffset = Vector3.zero;
            weapon.adsFOV = weaponType == WeaponType.Rifle ? 50f : 60f;
            weapon.adsSpeed = 0.12f;
            return weapon;
        }

        private static WeaponData CreateFallbackKnife()
        {
            WeaponData weapon = CreateInstance<WeaponData>();
            weapon.hideFlags = HideFlags.HideAndDontSave;
            weapon.weaponId = "knife_tactical";
            weapon.weaponName = WeaponName.KnifeDefault;
            weapon.weaponType = WeaponType.Knife;
            weapon.price = 0;
            weapon.damage = 50f;
            weapon.fireRate = 0.5f;
            weapon.reloadTime = 0f;
            weapon.magazineSize = 1;
            weapon.maxReserveAmmo = 1;
            weapon.range = 2.5f;
            weapon.primaryAttackRange = 1.5f;
            weapon.secondaryAttackRange = 2.5f;
            weapon.primaryAttackDamage = 50f;
            weapon.secondaryAttackDamage = 150f;
            weapon.rightHandPositionOffset = new Vector3(0.08f, -0.16f, 0.24f);
            weapon.rightHandRotationOffset = Vector3.zero;
            return weapon;
        }
    }
}
