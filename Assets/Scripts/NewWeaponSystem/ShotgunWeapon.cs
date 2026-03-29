using ProjectZ.Weapon;
using UnityEngine;

/// <summary>
/// Shotgun sınıfı: Judge, Bucky
/// Her atışta çoklu pellet, geniş spread.
/// </summary>
public class ShotgunWeapon : BaseWeapon
{
    [Header("Shotgun Specific")]
    public bool isFullAuto = false;         // Judge = true, Bucky = false
    public float adsSpreadMultiplier = 0.4f; // ADS'de spread azalır

    private bool isADS = false;
    private bool triggerHeld = false;

    void Update()
    {
        if (isFullAuto && triggerHeld)
            PrimaryAttack();
    }

    public void SetTrigger(bool held) => triggerHeld = held;

    public override void SecondaryAttack()
    {
        // Bucky'nin alternatif ateşi: tek büyük mermi
        if (data.weaponName == WeaponName.Bucky)
            AlternativeFire();
        else
        {
            isADS = !isADS;
            weaponAnimator?.SetBool(AnimADS, isADS);
        }
    }

    protected override void Fire()
    {
        SpawnMuzzleFlash();
        SpawnShellEject();
        weaponAnimator?.SetTrigger(AnimShoot);
        PlaySound(data.shootSound);

        Camera cam = Camera.main;
        float spread = isADS ? data.pelletSpread * adsSpreadMultiplier : data.pelletSpread;

        // Her pellet için ayrı raycast
        for (int i = 0; i < data.pelletsPerShot; i++)
        {
            Vector3 spreadDir = new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f
            );

            Ray ray = new Ray(cam.transform.position, cam.transform.forward + spreadDir);

            if (Physics.Raycast(ray, out RaycastHit hit, data.range))
            {
                SpawnImpact(hit.point, hit.normal);
                if (hit.collider.TryGetComponent<IDamageable>(out var target))
                {
                    // Mesafeye göre hasar düşürme (shotgun için kritik)
                    float distanceFactor = Mathf.Clamp01(1f - (hit.distance / (data.range * 0.5f)));
                    float pelletDamage = (data.damage / data.pelletsPerShot) * distanceFactor;
                    target.TakeDamage(pelletDamage, hit.point, hit.normal);
                }
            }
        }
    }

    // Bucky'nin alt ateş modu: tek büyük mermi, daha az spread
    private void AlternativeFire()
    {
        if (isReloading || currentAmmo <= 0) return;
        currentAmmo--;

        SpawnMuzzleFlash();
        weaponAnimator?.SetTrigger(AnimShoot);

        Camera cam = Camera.main;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, data.range * 1.5f))
        {
            SpawnImpact(hit.point, hit.normal);
            if (hit.collider.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(data.damage * 0.8f, hit.point, hit.normal);
        }
    }
}
