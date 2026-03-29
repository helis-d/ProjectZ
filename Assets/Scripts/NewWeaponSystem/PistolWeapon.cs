using ProjectZ.Weapon;
using UnityEngine;

/// <summary>
/// Pistol sınıfı: Classic, Ghost, Sheriff, Frenzy, Shorty
/// Hafif, hızlı çekim. Classic = tam oto sağ tık.
/// </summary>
public class PistolWeapon : BaseWeapon
{
    [Header("Pistol Specific")]
    public bool hasAltFire = false;         // Classic = true (burst)
    public int altBurstCount = 3;
    public float altBurstDelay = 0.05f;
    public bool isFullAuto = false;         // Frenzy = true

    private bool triggerHeld = false;

    void Update()
    {
        if (isFullAuto && triggerHeld)
            PrimaryAttack();
    }

    public void SetTrigger(bool held) => triggerHeld = held;

    public override void SecondaryAttack()
    {
        if (hasAltFire)
            StartCoroutine(BurstFire()); // Classic burst
        else
        {
            // Diğer tabancalar için ADS
            weaponAnimator?.SetTrigger(AnimADS);
        }
    }

    protected override void Fire()
    {
        SpawnMuzzleFlash();
        SpawnShellEject();
        weaponAnimator?.SetTrigger(AnimShoot);
        PlaySound(data.shootSound);

        Camera cam = Camera.main;
        Vector3 spread = new Vector3(
            Random.Range(-data.bulletSpread, data.bulletSpread),
            Random.Range(-data.bulletSpread, data.bulletSpread),
            0f
        );

        Ray ray = new Ray(cam.transform.position, cam.transform.forward + spread);

        if (Physics.Raycast(ray, out RaycastHit hit, data.range))
        {
            SpawnImpact(hit.point, hit.normal);
            if (hit.collider.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(data.damage, hit.point, hit.normal);
        }
    }

    private System.Collections.IEnumerator BurstFire()
    {
        for (int i = 0; i < altBurstCount; i++)
        {
            if (currentAmmo <= 0) break;
            Fire();
            currentAmmo--;
            yield return new WaitForSeconds(altBurstDelay);
        }
    }
}
