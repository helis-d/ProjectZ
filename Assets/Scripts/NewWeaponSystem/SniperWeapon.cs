using ProjectZ.Weapon;
using UnityEngine;

/// <summary>
/// Sniper sınıfı: Operator, Marshal, Outlaw
/// Tek atış, yüksek hasar, ADS zoom zorunlu.
/// </summary>
public class SniperWeapon : BaseWeapon
{
    [Header("Sniper Specific")]
    public float scopedFOV = 15f;           // tam zoom FOV
    public float scopeInTime = 0.3f;
    public bool requireScopeToFire = false; // Operator = true, Marshal = false
    public float moveSpeedPenalty = 0.4f;   // scope'dayken hareket yavaşlama

    [Header("Scope UI")]
    public GameObject scopeOverlay;         // scope ekran UI'ı

    private bool isScoped = false;
    private Camera mainCam;
    private float defaultFOV;

    protected override void Awake()
    {
        base.Awake();
        mainCam = Camera.main;
        defaultFOV = mainCam != null ? mainCam.fieldOfView : 60f;
    }

    public override void PrimaryAttack()
    {
        if (requireScopeToFire && !isScoped) return;
        base.PrimaryAttack();
    }

    public override void SecondaryAttack()
    {
        // Sağ tık = scope toggle
        if (isScoped) ExitScope();
        else EnterScope();
    }

    public override void Holster()
    {
        base.Holster();
        if (isScoped) ExitScope();
    }

    protected override void Fire()
    {
        SpawnMuzzleFlash();
        SpawnShellEject();
        weaponAnimator?.SetTrigger(AnimShoot);
        PlaySound(data.shootSound);

        // Scope'dan çık (bolt-action geri alımı simülasyonu)
        if (isScoped) ExitScope();

        if (!TryBuildFireRay(Vector3.zero, out Ray ray))
            return;

        // Sniper penetrasyon: birden fazla hedefi deler (Operator gibi)
        RaycastHit[] hits = Physics.RaycastAll(ray, data.range);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int penetrationCount = 0;
        int maxPenetration = data.weaponName == WeaponName.Operator ? 2 : 1;

        foreach (var hit in hits)
        {
            if (penetrationCount >= maxPenetration) break;
            SpawnImpact(hit.point, hit.normal);
            if (hit.collider.TryGetComponent<IDamageable>(out var target))
            {
                // Mesafeye göre hasar düşürme
                float distanceFactor = Mathf.Clamp01(1f - (hit.distance / data.range));
                float finalDamage = data.damage * distanceFactor * (penetrationCount == 0 ? 1f : 0.5f);
                if (!UsesAuthoritativeCombatPipeline())
                    target.TakeDamage(finalDamage, hit.point, hit.normal);
                penetrationCount++;
            }
        }
    }

    private void EnterScope()
    {
        isScoped = true;
        weaponAnimator?.SetBool(AnimADS, true);
        if (scopeOverlay) scopeOverlay.SetActive(true);
        StartCoroutine(ZoomFOV(scopedFOV));
    }

    private void ExitScope()
    {
        isScoped = false;
        weaponAnimator?.SetBool(AnimADS, false);
        if (scopeOverlay) scopeOverlay.SetActive(false);
        StartCoroutine(ZoomFOV(defaultFOV));
    }

    private System.Collections.IEnumerator ZoomFOV(float targetFOV)
    {
        if (mainCam == null)
            yield break;

        float startFOV = mainCam.fieldOfView;
        float t = 0f;
        while (t < scopeInTime)
        {
            t += Time.deltaTime;
            mainCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t / scopeInTime);
            yield return null;
        }
        mainCam.fieldOfView = targetFOV;
    }

    public bool IsScoped => isScoped;
}
