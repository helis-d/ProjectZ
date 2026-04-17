using ProjectZ.Weapon;
using UnityEngine;
using System.Collections;

/// <summary>
/// Knife sınıfı: Default, Karambit, Butterfly
/// İki saldırı modu: hızlı sol tık, yavaş+güçlü sağ tık.
/// </summary>
public class KnifeWeapon : BaseWeapon
{
    [Header("Knife Specific")]
    public float primaryCooldown = 0.5f;
    public float secondaryCooldown = 1.2f;
    public float moveSpeedBonus = 1.3f;     // bıçakla daha hızlı koşulur

    // Animator hash'leri
    private static readonly int AnimPrimaryAttack = Animator.StringToHash("PrimaryAttack");
    private static readonly int AnimSecondaryAttack = Animator.StringToHash("SecondaryAttack");
    private static readonly int AnimRun = Animator.StringToHash("Run");

    private float nextPrimaryTime = 0f;
    private float nextSecondaryTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        // Bıçakların ammo'su yok
        currentAmmo = int.MaxValue;
    }

    // Sol tık: hızlı, düşük hasar
    public override void PrimaryAttack()
    {
        if (data == null) return;
        if (Time.time < nextPrimaryTime || isReloading) return;
        nextPrimaryTime = Time.time + primaryCooldown;
        StartCoroutine(MeleeAttack(
            data.primaryAttackRange,
            data.primaryAttackDamage,
            AnimPrimaryAttack
        ));
    }

    // Sağ tık: yavaş, yüksek hasar (one-shot potansiyeli)
    public override void SecondaryAttack()
    {
        if (data == null) return;
        if (Time.time < nextSecondaryTime || isReloading) return;
        nextSecondaryTime = Time.time + secondaryCooldown;
        StartCoroutine(MeleeAttack(
            data.secondaryAttackRange,
            data.secondaryAttackDamage,
            AnimSecondaryAttack
        ));
    }

    // Bıçakla reload/ammo yok
    public override void StartReload() { }

    public override void InitializeRuntimeData(WeaponData runtimeData)
    {
        base.InitializeRuntimeData(runtimeData);
        currentAmmo = int.MaxValue;
    }

    protected override void Fire() { }     // BaseWeapon zorunlu kıldığı için boş

    private IEnumerator MeleeAttack(float range, float damage, int animHash)
    {
        weaponAnimator?.SetTrigger(animHash);
        if (data != null)
            PlaySound(data.shootSound);     // bıçak çarpma sesi burada

        // Animasyonun vuruş frame'ini bekle
        yield return new WaitForSeconds(0.15f);

        Camera cam = Camera.main;
        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        Vector3 direction = cam != null ? cam.transform.forward : transform.forward;
        Ray ray = new Ray(origin, direction);

        // Bıçak için SphereCast: daha toleranslı isabet alanı
        if (Physics.SphereCast(ray, 0.3f, out RaycastHit hit, range))
        {
            SpawnImpact(hit.point, hit.normal);
            TryApplyDirectDamage(hit, damage);
        }
    }

    // Bıçak çekilince karakter hızlanır — CharacterController bu değeri okur
    public float GetMoveSpeedBonus() => moveSpeedBonus;
}
