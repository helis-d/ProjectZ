using System.Collections.Generic;
using ProjectZ.Weapon;
using UnityEngine;

/// <summary>
/// Weapon inventory and switching system for the active player.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    public WeaponAttachment attachment;
    public Transform weaponHolder;

    [Header("Weapon Slots")]
    public BaseWeapon primaryWeapon;
    public BaseWeapon secondaryWeapon;
    public BaseWeapon meleeWeapon;

    private BaseWeapon activeWeapon;
    private int currentSlot;
    private readonly List<BaseWeapon> weapons = new();

    private void Awake()
    {
        RebuildWeaponCache();
    }

    private void Start()
    {
        RebuildWeaponCache();

        foreach (BaseWeapon weapon in weapons)
            weapon.gameObject.SetActive(false);

        if (weapons.Count > 0)
            SwitchToSlot(0);
    }

    public void HandleSwitchInput(int slotIndex)
    {
        SwitchToSlot(slotIndex);
    }

    public void SwitchToSlot(int slot)
    {
        RebuildWeaponCache();
        if (slot < 0 || slot >= weapons.Count)
            return;

        if (slot == currentSlot && activeWeapon != null)
            return;

        currentSlot = slot;
        BaseWeapon targetWeapon = weapons[slot];
        if (targetWeapon == null)
            return;

        if (activeWeapon != null && activeWeapon != targetWeapon)
            activeWeapon.Holster();

        activeWeapon = targetWeapon;
        if (attachment != null)
            attachment.AttachWeapon(activeWeapon);
    }

    public BaseWeapon GetActiveWeapon() => activeWeapon;
    public int GetCurrentAmmo() => activeWeapon?.CurrentAmmo ?? 0;
    public int GetMaxAmmo() => activeWeapon?.data.magazineSize ?? 0;

    public void RebuildWeaponCache()
    {
        weapons.Clear();
        if (primaryWeapon != null) weapons.Add(primaryWeapon);
        if (secondaryWeapon != null) weapons.Add(secondaryWeapon);
        if (meleeWeapon != null) weapons.Add(meleeWeapon);
    }
}
