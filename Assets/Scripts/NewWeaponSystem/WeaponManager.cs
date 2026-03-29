using ProjectZ.Weapon;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Silah envanteri ve geçiş sistemi.
/// Bu scripti Player objesine ekle.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    public WeaponAttachment attachment;
    public Transform weaponHolder;          // sahne içinde silahların spawn noktası

    [Header("Weapon Slots")]
    public BaseWeapon primaryWeapon;        // Rifle veya Sniper
    public BaseWeapon secondaryWeapon;      // Pistol
    public BaseWeapon meleeWeapon;          // Knife

    // Anlık aktif silah
    private BaseWeapon activeWeapon;
    private int currentSlot = 0;            // 0=primary, 1=secondary, 2=melee

    // Tüm silah listesi (kolay iterasyon için)
    private List<BaseWeapon> weapons = new List<BaseWeapon>();

    void Start()
    {
        // Silahları listeye ekle
        if (primaryWeapon) weapons.Add(primaryWeapon);
        if (secondaryWeapon) weapons.Add(secondaryWeapon);
        if (meleeWeapon) weapons.Add(meleeWeapon);

        // Başlangıçta tüm silahları gizle
        foreach (var w in weapons)
            w.gameObject.SetActive(false);

        // Primary ile başla
        SwitchToSlot(0);
    }

    void Update()
    {
        // GİRDİ ARTIK PLAYER COMBAT CONTROLLER TARAFINDAN YÖNETİLİYOR (Multiplayer Uyumlu)
        // HandleInput() silindi.
    }

    public void HandleSwitchInput(int slotIndex)
    {
        SwitchToSlot(slotIndex);
    }

    public void SwitchToSlot(int slot)
    {
        if (slot < 0 || slot >= weapons.Count) return;
        if (slot == currentSlot && activeWeapon != null) return;

        currentSlot = slot;
        BaseWeapon targetWeapon = weapons[slot];

        // Aktif silahı holster et
        if (activeWeapon != null && activeWeapon != targetWeapon)
            activeWeapon.Holster();

        activeWeapon = targetWeapon;
        attachment.AttachWeapon(activeWeapon);
    }

    // Tam oto silahlar için trigger yönetimi
    private void SetTrigger(bool held)
    {
        if (activeWeapon is RifleWeapon rifle) rifle.SetTrigger(held);
        else if (activeWeapon is ShotgunWeapon shotgun) shotgun.SetTrigger(held);
        else if (activeWeapon is PistolWeapon pistol) pistol.SetTrigger(held);
    }

    // UI için
    public BaseWeapon GetActiveWeapon() => activeWeapon;
    public int GetCurrentAmmo() => activeWeapon?.CurrentAmmo ?? 0;
    public int GetMaxAmmo() => activeWeapon?.data.magazineSize ?? 0;
}
