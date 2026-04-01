using FishNet.Object;
using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing; // NATIVE FISHNET TIMELINE
using ProjectZ.Combat;
using ProjectZ.UI;
using ProjectZ.Weapon;
using ProjectZ.Core.Interfaces;
using UnityEngine;

namespace ProjectZ.Player
{
    /// <summary>
    /// Bridges input to the server-authoritative combat pipeline:
    /// TryFire -> ServerRpc -> Lag Compensation (Native) -> HitscanShooter -> DamageProcessor.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerInventory))]
    [RequireComponent(typeof(HitscanShooter))]
    [RequireComponent(typeof(DamageProcessor))]
    public class PlayerCombatController : NetworkBehaviour
    {
        private IPlayerInput _input;
        private PlayerInventory _inventory;
        private PlayerHealth _health;

        private HitscanShooter _hitscanShooter;
        private DamageProcessor _damageProcessor;
        private WeaponManager _weaponManager; 
        private CrosshairUI _crosshair;

        private RollbackManager _rollbackManager; 
        private BaseWeapon _currentBoundWeapon; 

        // ─── ANTI-CHEAT SUNUCU DENETLEYİCİLERİ ───────────────────────────
        private uint _lastServerFireTick;
        private System.Collections.Generic.Dictionary<int, int> _serverAmmoTracker = new();
        private System.Collections.Generic.HashSet<int> _reloadingWeapons = new();

        // SERVER SİSTEMİ İÇİN (BloodPact vb. yetenekler dinleyecek)
        public event System.Action OnServerFired;

        private void Awake()
        {
            _input = GetComponent<IPlayerInput>();
            _inventory = GetComponent<PlayerInventory>();
            _health = GetComponent<PlayerHealth>();

            _hitscanShooter = GetComponent<HitscanShooter>();
            _damageProcessor = GetComponent<DamageProcessor>();
            _weaponManager = GetComponent<WeaponManager>(); 
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
                enabled = false;

            _crosshair = FindFirstObjectByType<CrosshairUI>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // NetworkManager üzerindeki native RollbackManager'ı bul
            if (NetworkManager != null)
                _rollbackManager = NetworkManager.GetComponent<RollbackManager>();
                
            if (_rollbackManager == null)
                Debug.LogWarning("[PlayerCombatController] RollbackManager not found on NetworkManager. Hit detection will not be lag compensated.");
        }

        private void Update()
        {
            if (!IsOwner || _health != null && _health.IsDead.Value || _weaponManager == null)
                return;

            BaseWeapon activeWeapon = _weaponManager.GetActiveWeapon();
            if (activeWeapon == null || activeWeapon.data == null)
                return;

            // Silah değiştiyse olay dinleyicisini yeni silaha taşı
            if (_currentBoundWeapon != activeWeapon)
            {
                if (_currentBoundWeapon != null)
                    _currentBoundWeapon.OnWeaponFired -= HandleLocalWeaponFired;

                _currentBoundWeapon = activeWeapon;
                _currentBoundWeapon.OnWeaponFired += HandleLocalWeaponFired;
            }

            // GİRDİLER (Slot Değiştirme ve Ateş)
            if (_input.ReloadPressed)
            {
                activeWeapon.StartReload(); 
                CmdStartReload(activeWeapon.data.weaponId); // A16Z Anti-Cheat: Sunucuya Rulo Değişimini Bildir
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1)) _weaponManager.SwitchToSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _weaponManager.SwitchToSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _weaponManager.SwitchToSlot(2);

            if (_input.FireHeld)
                activeWeapon.PrimaryAttack();
        }

        // Yerel BaseWeapon başarılı bir ateşleme/animasyon yaptığında tetiklenir
        private void HandleLocalWeaponFired()
        {
            if (_currentBoundWeapon == null || _currentBoundWeapon.data == null) return;
            
            _crosshair?.AddFireRecoil(1f);
            
            Vector3 origin = _currentBoundWeapon.muzzlePoint ? _currentBoundWeapon.muzzlePoint.position : transform.position;
            Vector3 direction = _currentBoundWeapon.muzzlePoint ? _currentBoundWeapon.muzzlePoint.forward : transform.forward;
            
            // a16z STANDARDI: Merminin ateşlendiği MILISANIYEYI (PreciseTick) sunucuya bildiriyoruz.
            PreciseTick pt = new PreciseTick(TimeManager.Tick);
            CmdFire(origin, direction, _currentBoundWeapon.data.weaponId, pt);
        }

        [ServerRpc]
        private void CmdStartReload(string weaponId)
        {
            if (_weaponManager == null) return;
            BaseWeapon activeWeapon = _weaponManager.GetActiveWeapon();
            if (activeWeapon == null || activeWeapon.data == null || activeWeapon.data.weaponId != weaponId) return;

            int instanceId = activeWeapon.GetInstanceID();

            if (_reloadingWeapons.Contains(instanceId)) return;
            
            _reloadingWeapons.Add(instanceId);
            StartCoroutine(ServerReloadRoutine(activeWeapon));
        }

        private System.Collections.IEnumerator ServerReloadRoutine(BaseWeapon weapon)
        {
            int instanceId = weapon.GetInstanceID();
            
            // İstemcinin hile yapıp anında reload yollamasını engelle (Server-Authoritative Reload)
            yield return new WaitForSeconds(weapon.data.reloadTime);
            
            // A16Z Anti-Cheat: GHOST RELOAD FIX (Oyuncu silah değiştirdiyse mermiyi fulleme, hileyi iptal et)
            if (_weaponManager != null && _weaponManager.GetActiveWeapon() == weapon)
            {
                _serverAmmoTracker[instanceId] = weapon.data.magazineSize;
            }
            else
            {
                Debug.LogWarning($"[Anti-Cheat] Player {OwnerId} tried to swap weapon during reload to exploit Ghost Reload. Reload Cancelled!");
            }
            
            _reloadingWeapons.Remove(instanceId);
        }

        [ServerRpc]
        private void CmdFire(Vector3 clientOrigin, Vector3 clientDirection, string weaponId, PreciseTick clientTick)
        {
            if (_weaponManager == null) return;

            BaseWeapon activeWeapon = _weaponManager.GetActiveWeapon();
            if (activeWeapon == null || activeWeapon.data == null || activeWeapon.data.weaponId != weaponId) return;

            int instanceId = activeWeapon.GetInstanceID();

            // 1. A16Z ANTI-CHEAT: MAKRO & HIZLI ATEŞ (RAPID FIRE) ALGORİTMASI
            // Ateşler arası geçen süre silahın limitinden hızlıysa isteği çöpe at! (-0.05d pingleme toleransı)
            uint requiredTicks = TimeManager.TimeToTicks(activeWeapon.data.fireRate - 0.05f);
            if (TimeManager.Tick < _lastServerFireTick + requiredTicks)
            {
                Debug.LogWarning($"[Anti-Cheat] Player {OwnerId} tried to Rapid/Macro Fire.");
                return; 
            }

            // 2. A16Z ANTI-CHEAT: SONSUZ MERMİ HİLESİ (INFINITE AMMO) ALGORİTMASI ve Silah Kimliği Çatışması
            if (!_serverAmmoTracker.ContainsKey(instanceId))
                _serverAmmoTracker[instanceId] = activeWeapon.data.magazineSize;

            if (_serverAmmoTracker[instanceId] <= 0 || _reloadingWeapons.Contains(instanceId))
            {
                Debug.LogWarning($"[Anti-Cheat] Player {OwnerId} tried to fire without ammo or during reload.");
                return;
            }

            // Doğrulandı: Mermiyi azalt ve süreyi kaydet
            _serverAmmoTracker[instanceId]--;
            _lastServerFireTick = TimeManager.Tick;

            if (_hitscanShooter == null || _damageProcessor == null) return;

            Vector3 direction = clientDirection.sqrMagnitude > 0.0001f ? clientDirection.normalized : transform.forward;
            Vector3 serverOrigin = activeWeapon.muzzlePoint ? activeWeapon.muzzlePoint.position : transform.position;
            Vector3 origin = Vector3.Distance(clientOrigin, serverOrigin) <= 3f ? clientOrigin : serverOrigin;

            // a16z STANDARDI: NATIVE SERVER-SIDE LAG COMPENSATION
            // Tüm ağdaki hitboxları oyuncunun sıktığı "geçmiş tick" anına ışınla!
            if (_rollbackManager != null)
                _rollbackManager.Rollback(clientTick, RollbackPhysicsType.Physics);

            // Klasik Raycastmizi yapıyoruz (ama artık herkes oyuncunun gördüğü yerde!)
            var hitResult = _hitscanShooter.FireRay(origin, direction, activeWeapon.data.penetrationPower);

            // Vurup vurmadığımıza karar verdikten sonra zamanlamayı (hitbox pozisyonlarını) günümüze geri getiriyoruz
            if (_rollbackManager != null)
                _rollbackManager.Return();

            // Hasarı uygula
            if (hitResult.DidHitPlayer && hitResult.TargetObject != null)
            {
                _damageProcessor.ProcessDamage(OwnerId, activeWeapon.data, hitResult, hitResult.TargetObject);
            }

            // Server-side çalışan yetenekler (örn: BloodPact ultisi) için olayı tetikle
            OnServerFired?.Invoke();
        }
    }
}
