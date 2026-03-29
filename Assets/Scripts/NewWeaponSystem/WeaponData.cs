using UnityEngine;

namespace ProjectZ.Weapon
{

public enum WeaponType
{
    Rifle,
    Sniper,
    Shotgun,
    Pistol,
    Knife
}

public enum WeaponName
{
    // Rifles
    Vandal,
    Phantom,
    Bulldog,
    Guardian,

    // Snipers
    Operator,
    Marshal,
    Outlaw,

    // Shotguns
    Judge,
    Bucky,

    // Pistols
    Classic,
    Shorty,
    Frenzy,
    Ghost,
    Sheriff,

    // Knives
    KnifeDefault,
    Karambit,
    Butterfly
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponId; // AĞ İÇİN (Network)
    public WeaponName weaponName;
    public WeaponType weaponType;

    [Header("Economy")]
    public int price = 2900;                  // Satın alma fiyatı (GDD Section 4)

    [Header("Stats")]
    public float damage = 30f;
    public float baseDamage => damage; // Eski sisteme (DamageProcessor) uyumluluk için
    public float headshotMultiplier = 2.5f;    // Kafa vuruşu çarpanı
    
    // Eski sisteme uyumluluk: WeaponMasteryManager WeaponType'ı weaponClass adıyla bekliyor
    public WeaponType weaponClass => weaponType;

    public float penetrationPower = 100f; // Duvar delme gücü (HitscanShooter için)
    public float fireRate = 0.1f;          // saniye cinsinden iki atış arası süre
    public float FireInterval => fireRate; // Eski sisteme (PlayerCombatController) uyumluluk için
    
    public float reloadTime = 2f;
    public float drawTime = 0.6f;
    public int magazineSize = 30;
    public int maxReserveAmmo = 90;
    public float range = 100f;
    public float bulletSpread = 0.01f;

    [Header("Attachment Offsets")]
    public Vector3 rightHandPositionOffset;
    public Vector3 rightHandRotationOffset;
    public Vector3 leftHandPositionOffset;   // IK için
    public Vector3 leftHandRotationOffset;

    [Header("ADS (Aim Down Sight)")]
    public Vector3 adsPositionOffset;
    public Vector3 adsRotationOffset;
    public float adsFOV = 40f;
    public float adsSpeed = 0.15f;

    [Header("Shotgun Extra")]
    public int pelletsPerShot = 8;          // sadece Shotgun için
    public float pelletSpread = 0.05f;

    [Header("Knife Extra")]
    public float primaryAttackRange = 1.5f;
    public float secondaryAttackRange = 2.5f;
    public float primaryAttackDamage = 50f;
    public float secondaryAttackDamage = 150f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip drawSound;
    public AudioClip emptySound;

    [Header("VFX")]
    public GameObject muzzleFlashPrefab;
    public GameObject bulletImpactPrefab;
    public GameObject shellEjectPrefab;     // kovan
}
}
