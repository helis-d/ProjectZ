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
        currentAmmo = data.magazineSize;
    }

    // ─── Temel API ───────────────────────────────────────────────
    public virtual void PrimaryAttack()
    {
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
        if (isReloading || currentAmmo == data.magazineSize) return;
        StartCoroutine(ReloadCoroutine());
    }

    public virtual void Draw()
    {
        gameObject.SetActive(true);
        weaponAnimator?.SetTrigger(AnimDraw);
        PlaySound(data.drawSound);
    }

    public virtual void Holster()
    {
        StopAllCoroutines();
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
            Destroy(Instantiate(data.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation), 0.05f);
    }

    protected void SpawnShellEject()
    {
        if (data.shellEjectPrefab && shellEjectPoint)
        {
            GameObject shell = Instantiate(data.shellEjectPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
            if (shell.TryGetComponent<Rigidbody>(out var rb))
                rb.AddForce(shellEjectPoint.right * 2f + shellEjectPoint.up * 1f, ForceMode.Impulse);
            Destroy(shell, 3f);
        }
    }

    protected void SpawnImpact(Vector3 point, Vector3 normal)
    {
        if (data.bulletImpactPrefab)
            Destroy(Instantiate(data.bulletImpactPrefab, point, Quaternion.LookRotation(normal)), 2f);
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip && TryGetComponent<AudioSource>(out var src))
            src.PlayOneShot(clip);
    }
}
