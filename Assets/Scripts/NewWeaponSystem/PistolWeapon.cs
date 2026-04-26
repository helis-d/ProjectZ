using ProjectZ.Core;
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

        Vector3 spread = new Vector3(
            SecureRandom.Range(-data.bulletSpread, data.bulletSpread),
            SecureRandom.Range(-data.bulletSpread, data.bulletSpread),
            0f
        );

        if (!TryBuildFireRay(spread, out Ray ray))
            return;

        if (Physics.Raycast(ray, out RaycastHit hit, data.range))
        {
            SpawnImpact(hit.point, hit.normal);
            TryApplyDirectDamage(hit, data.damage);
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
