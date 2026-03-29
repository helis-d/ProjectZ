using ProjectZ.Weapon;
using UnityEngine;

/// <summary>
/// Rifle sınıfı: Vandal, Phantom, Bulldog, Guardian
/// Tam otomatik veya burst ateşleme destekler.
/// </summary>
public class RifleWeapon : BaseWeapon
{
    [Header("Rifle Specific")]
    public bool isFullAuto = true;          // Vandal/Phantom = true, Bulldog burst = false
    public int burstCount = 3;              // burst mod için (Bulldog)
    public float burstDelay = 0.06f;

    private bool triggerHeld = false;
    private int currentBurst = 0;

    void Update()
    {
        // Tam otomatik ateşleme
        if (isFullAuto && triggerHeld)
            PrimaryAttack();
    }

    public void SetTrigger(bool held) => triggerHeld = held;

    protected override void Fire()
    {
        if (!isFullAuto)
        {
            // Burst mod (Bulldog)
            StartCoroutine(BurstFire());
            return;
        }

        SingleShot();
    }

    private void SingleShot()
    {
        SpawnMuzzleFlash();
        SpawnShellEject();
        weaponAnimator?.SetTrigger(AnimShoot);
        PlaySound(data.shootSound);

        // Raycast ile hasar
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
        for (int i = 0; i < burstCount; i++)
        {
            if (currentAmmo <= 0) break;
            SingleShot();
            currentAmmo--;
            yield return new WaitForSeconds(burstDelay);
        }
    }
}
