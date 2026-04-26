using UnityEngine;
using FishNet.Object;
using ProjectZ.Player;
using ProjectZ.Weapon;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Tüm VFX sistemlerini PlayerCombatController ve BaseWeapon eventlerine bağlar.
    /// Bir köprü (bridge) görevi görür — her ateş anında:
    ///   1. MuzzleFlashSystem.Fire()
    ///   2. BulletTrailRenderer.ShowTrail()
    ///   3. CameraShakeSystem.FireShake()
    /// Her isabet anında:
    ///   4. HitImpactSystem.ShowWorldImpact() veya ShowPlayerImpact()
    /// 
    /// KURULUM: Player prefab'ına ekle. 
    /// Inspector'dan referansları atamana GEREK YOK, otomatik bulur.
    /// </summary>
    [RequireComponent(typeof(PlayerCombatController))]
    public class CombatVFXBridge : NetworkBehaviour
    {
        private MuzzleFlashSystem _muzzleFlash;
        private BulletTrailRenderer _bulletTrail;
        private HitImpactSystem _hitImpact;

        private BaseWeapon _boundWeapon;
        private WeaponManager _weaponManager;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            // Aynı prefab üzerindeki VFX sistemlerini bul veya oluştur
            _muzzleFlash = GetComponentInChildren<MuzzleFlashSystem>();
            if (_muzzleFlash == null)
            {
                GameObject obj = new GameObject("_MuzzleFlash");
                obj.transform.SetParent(transform);
                _muzzleFlash = obj.AddComponent<MuzzleFlashSystem>();
            }

            _bulletTrail = GetComponentInChildren<BulletTrailRenderer>();
            if (_bulletTrail == null)
            {
                GameObject obj = new GameObject("_BulletTrail");
                obj.transform.SetParent(transform);
                _bulletTrail = obj.AddComponent<BulletTrailRenderer>();
            }

            _hitImpact = GetComponentInChildren<HitImpactSystem>();
            if (_hitImpact == null)
            {
                GameObject obj = new GameObject("_HitImpact");
                obj.transform.SetParent(transform);
                _hitImpact = obj.AddComponent<HitImpactSystem>();
            }

            _weaponManager = WeaponRuntimeRigBuilder.EnsurePlayerRig(gameObject, GetComponent<WeaponManager>());
        }

        private void Update()
        {
            if (_weaponManager == null) return;

            BaseWeapon active = _weaponManager.GetActiveWeapon();
            if (active != _boundWeapon)
            {
                // Eski silahtan çık
                if (_boundWeapon != null)
                    _boundWeapon.OnWeaponFired -= OnLocalWeaponFired;

                _boundWeapon = active;

                // Yeni silaha bağlan
                if (_boundWeapon != null)
                    _boundWeapon.OnWeaponFired += OnLocalWeaponFired;
            }
        }

        private void OnLocalWeaponFired()
        {
            if (_boundWeapon == null) return;

            Transform muzzle = _boundWeapon.muzzlePoint;
            if (muzzle == null) muzzle = transform;

            // 1. Namlu Flaşı
            _muzzleFlash.Fire(muzzle);

            // 2. Kamera Sarsıntısı
            if (CameraShakeSystem.Instance != null)
                CameraShakeSystem.Instance.FireShake();

            // 3. Mermi İzi — basit raycast ile isabet noktası bul
            Vector3 origin = muzzle.position;
            Vector3 direction = muzzle.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 200f))
            {
                _bulletTrail.ShowTrail(origin, hit.point);

                // 4. İsabet Efekti
                var surface = hit.collider.GetComponent<Combat.SurfaceMaterial>();
                var hitbox = hit.collider.GetComponentInParent<Combat.HitboxManager>();

                if (hitbox != null)
                {
                    _hitImpact.ShowPlayerImpact(hit.point, hit.normal);
                }
                else
                {
                    Combat.SurfaceType surfType = surface != null ? surface.Type : Combat.SurfaceType.Stone;
                    _hitImpact.ShowWorldImpact(hit.point, hit.normal, surfType);
                }
            }
            else
            {
                // Bir şeye çarpmadı — menzil sonuna trail çiz
                _bulletTrail.ShowTrail(origin, origin + direction * 200f);
            }
        }

        private void OnDestroy()
        {
            if (_boundWeapon != null)
                _boundWeapon.OnWeaponFired -= OnLocalWeaponFired;
        }
    }
}
