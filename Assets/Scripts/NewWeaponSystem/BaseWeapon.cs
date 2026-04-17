using ProjectZ.Audio;
using ProjectZ.Weapon;
using System.Collections;
using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    [Header("Data")]
    public WeaponData data;

    [Header("References")]
    public Transform muzzlePoint;           // namlu ucu
    public Transform shellEjectPoint;       // kovan çıkış noktası
    public Animator weaponAnimator;

    protected int currentAmmo;
    protected bool isReloading = false;
    protected bool isFiring = false;
    protected float nextFireTime = 0f;

    // [FIX] BUG-14: cached once in Awake — Camera.main is FindObjectOfType internally
    private Camera _firingCamera;
    // [FIX] BUG-15: stored ref so we stop only this coroutine, not all
    private Coroutine _reloadCoroutine;
    // [FIX] BUG-13: cached audio source for priority routing
    private AudioSource _cachedAudio;

    // Animator hash'leri (performans için)
    protected static readonly int AnimShoot = Animator.StringToHash("Shoot");
    protected static readonly int AnimReload = Animator.StringToHash("Reload");
    protected static readonly int AnimDraw = Animator.StringToHash("Draw");
    protected static readonly int AnimHolster = Animator.StringToHash("Holster");
    protected static readonly int AnimADS = Animator.StringToHash("ADS");

    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;

    // AĞ SİSTEMİ İÇİN (PlayerCombatController dinleyecek)
    public event System.Action OnWeaponFired;

    protected virtual void Awake()
    {
        currentAmmo = data != null ? data.magazineSize : 0;
        // [FIX] BUG-14: cache camera once — avoids FindObjectOfType every shot
        _firingCamera = Camera.main ?? GetComponentInParent<Camera>();
        // [FIX] BUG-13: cache AudioSource for priority-routed playback
        _cachedAudio = GetComponent<AudioSource>();
    }

    // ─── Temel API ───────────────────────────────────────────────
    public virtual void PrimaryAttack()
    {
        if (data == null)
            return;

        if (isReloading || Time.time < nextFireTime || currentAmmo <= 0)
        {
            if (currentAmmo <= 0) PlaySound(data.emptySound);
            return;
        }

        nextFireTime = Time.time + data.fireRate;
        currentAmmo--;
        Fire();
        
        // Ağ (Multiplayer) sistemine haber ver
        OnWeaponFired?.Invoke();
    }

    public virtual void SecondaryAttack() { }   // ADS veya bıçak ikincil saldırı

    // Ustalık (Mastery) sistemi uyumluluğu için
    public virtual void ApplyBuffMultipliers(LevelMultipliers multipliers) { }
    // Hero ultileri (Adrenaline, Overdrive) için gecici buff özelliği
    public virtual void ApplyTemporaryFireRateBuff(float multiplier, float duration) { }

    public virtual void StartReload()
    {
        if (data == null) return;
        if (isReloading || currentAmmo == data.magazineSize) return;
        _reloadCoroutine = StartCoroutine(ReloadCoroutine()); // [FIX] BUG-15: store ref
    }

    public virtual void Draw()
    {
        gameObject.SetActive(true);
        weaponAnimator?.SetTrigger(AnimDraw);
        if (data != null)
            PlaySound(data.drawSound);
    }

    public virtual void Holster()
    {
        // [FIX] BUG-15: stop only the reload coroutine, not ALL coroutines
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
        isReloading = false;
        weaponAnimator?.SetTrigger(AnimHolster);
    }

    // ─── Soyut metodlar (her silah türü kendisi doldurur) ────────
    protected abstract void Fire();

    // ─── Yardımcı metodlar ───────────────────────────────────────
    protected IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        weaponAnimator?.SetTrigger(AnimReload);
        PlaySound(data.reloadSound);
        yield return new WaitForSeconds(data.reloadTime);
        currentAmmo = data.magazineSize;
        isReloading = false;
    }

    protected void SpawnMuzzleFlash()
    {
        if (data.muzzleFlashPrefab && muzzlePoint)
        {
            if (ProjectZ.Core.VFXPoolManager.Instance != null)
            {
                GameObject obj = ProjectZ.Core.VFXPoolManager.Instance.Spawn(data.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
                ProjectZ.Core.VFXPoolManager.Instance.Release(obj, data.muzzleFlashPrefab, 0.05f);
            }
            else
            {
                Destroy(Instantiate(data.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation), 0.05f);
            }
        }
    }

    protected void SpawnShellEject()
    {
        if (data.shellEjectPrefab && shellEjectPoint)
        {
            GameObject shell;
            if (ProjectZ.Core.VFXPoolManager.Instance != null)
            {
                shell = ProjectZ.Core.VFXPoolManager.Instance.Spawn(data.shellEjectPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
                ProjectZ.Core.VFXPoolManager.Instance.Release(shell, data.shellEjectPrefab, 3f);
            }
            else
            {
                shell = Instantiate(data.shellEjectPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
                Destroy(shell, 3f);
            }

            if (shell.TryGetComponent<Rigidbody>(out var rb))
            {
                // Havuzdan gelen objenin momentumunu sıfırla ki üst üste binmesin
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(shellEjectPoint.right * 2f + shellEjectPoint.up * 1f, ForceMode.Impulse);
            }
        }
    }

    protected void SpawnImpact(Vector3 point, Vector3 normal)
    {
        if (data.bulletImpactPrefab)
        {
            if (ProjectZ.Core.VFXPoolManager.Instance != null)
            {
                GameObject obj = ProjectZ.Core.VFXPoolManager.Instance.Spawn(data.bulletImpactPrefab, point, Quaternion.LookRotation(normal));
                ProjectZ.Core.VFXPoolManager.Instance.Release(obj, data.bulletImpactPrefab, 2f);
            }
            else
            {
                Destroy(Instantiate(data.bulletImpactPrefab, point, Quaternion.LookRotation(normal)), 2f);
            }
        }
    }

    protected bool TryBuildFireRay(Vector3 spread, out Ray ray)
    {
        // [FIX] BUG-14: use cached camera — no FindObjectOfType per shot
        Camera firingCamera = _firingCamera;
        if (firingCamera == null) firingCamera = Camera.main; // runtime fallback only

        if (firingCamera != null)
        {
            Vector3 direction = firingCamera.transform.forward + spread;
            ray = new Ray(firingCamera.transform.position, direction.normalized);
            return true;
        }

        Transform originTransform = muzzlePoint != null ? muzzlePoint : transform;
        Vector3 fallbackDirection = originTransform.forward + spread;
        if (fallbackDirection.sqrMagnitude <= 0.0001f)
            fallbackDirection = transform.forward;

        ray = new Ray(originTransform.position, fallbackDirection.normalized);
        return true;
    }

    protected bool UsesAuthoritativeCombatPipeline()
    {
        return GetComponentInParent<ProjectZ.Player.PlayerCombatController>() != null;
    }

    protected void TryApplyDirectDamage(RaycastHit hit, float damage)
    {
        if (damage <= 0f || UsesAuthoritativeCombatPipeline())
            return;

        if (hit.collider.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage, hit.point, hit.normal);
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip == null || _cachedAudio == null) return;
        // [FIX] BUG-13: route through AudioPriorityManager, never call AudioSource.Play directly
        if (AudioPriorityManager.Instance != null)
            AudioPriorityManager.Instance.PlaySound(_cachedAudio, clip, AudioPriorityManager.AudioChannel.Combat);
        else
            _cachedAudio.PlayOneShot(clip); // editor/offline fallback only
    }

    public virtual void InitializeRuntimeData(WeaponData runtimeData)
    {
        data = runtimeData;
        currentAmmo = runtimeData != null ? runtimeData.magazineSize : 0;
    }
}
