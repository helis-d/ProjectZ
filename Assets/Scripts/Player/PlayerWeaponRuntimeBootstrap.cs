using ProjectZ.Weapon;
using UnityEngine;

namespace ProjectZ.Player
{
    internal static class PlayerWeaponRuntimeBootstrap
    {
        public static WeaponManager EnsureWeaponRig(GameObject playerRoot, WeaponManager existingManager)
        {
            if (playerRoot == null)
                return existingManager;

            WeaponManager weaponManager = existingManager ?? playerRoot.GetComponent<WeaponManager>();
            if (weaponManager == null)
                weaponManager = playerRoot.AddComponent<WeaponManager>();

            WeaponAttachment attachment = playerRoot.GetComponent<WeaponAttachment>();
            if (attachment == null)
                attachment = playerRoot.AddComponent<WeaponAttachment>();

            Transform anchor = EnsureAnchor(playerRoot.transform);
            attachment.rightHandBone = attachment.rightHandBone != null ? attachment.rightHandBone : anchor;
            attachment.leftHandBone = attachment.leftHandBone != null ? attachment.leftHandBone : anchor;

            weaponManager.attachment = attachment;
            weaponManager.weaponHolder = anchor;
            weaponManager.primaryWeapon = EnsureWeapon<RifleWeapon>(anchor, "Primary_Vandal", "vandal");
            weaponManager.secondaryWeapon = EnsureWeapon<PistolWeapon>(anchor, "Secondary_Classic", "pistol_classic");
            weaponManager.meleeWeapon = EnsureWeapon<KnifeWeapon>(anchor, "Melee_TacticalKnife", "knife_tactical");
            weaponManager.RebuildWeaponCache();
            return weaponManager;
        }

        public static WeaponData GetFallbackWeapon(string weaponId)
        {
            return WeaponCatalog.Instance?.GetById(weaponId);
        }

        private static Transform EnsureAnchor(Transform playerRoot)
        {
            Transform anchor = playerRoot.Find("RuntimeWeaponAnchor");
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject("RuntimeWeaponAnchor");
                anchor = anchorObject.transform;
                anchor.SetParent(playerRoot, false);
                anchor.localPosition = new Vector3(0.2f, 1.25f, 0.35f);
                anchor.localRotation = Quaternion.identity;
            }

            return anchor;
        }

        private static T EnsureWeapon<T>(Transform anchor, string objectName, string weaponId) where T : BaseWeapon
        {
            Transform weaponTransform = anchor.Find(objectName);
            GameObject weaponObject = weaponTransform != null ? weaponTransform.gameObject : new GameObject(objectName);
            if (weaponTransform == null)
            {
                weaponObject.transform.SetParent(anchor, false);
                weaponObject.transform.localPosition = Vector3.zero;
                weaponObject.transform.localRotation = Quaternion.identity;
                weaponObject.SetActive(false);
            }

            T weapon = weaponObject.GetComponent<T>();
            if (weapon == null)
                weapon = weaponObject.AddComponent<T>();

            if (weaponObject.GetComponent<AudioSource>() == null)
                weaponObject.AddComponent<AudioSource>();

            Transform muzzle = EnsureChildTransform(weaponObject.transform, "MuzzlePoint", new Vector3(0f, 0f, 0.55f));
            Transform eject = EnsureChildTransform(weaponObject.transform, "ShellEjectPoint", new Vector3(0.05f, 0.02f, 0.1f));

            weapon.muzzlePoint = muzzle;
            weapon.shellEjectPoint = eject;
            if (weapon.data == null || weapon.data.weaponId != weaponId)
                weapon.InitializeRuntimeData(GetFallbackWeapon(weaponId));

            return weapon;
        }

        private static Transform EnsureChildTransform(Transform parent, string childName, Vector3 localPosition)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                child = childObject.transform;
                child.SetParent(parent, false);
                child.localRotation = Quaternion.identity;
            }

            child.localPosition = localPosition;
            return child;
        }
    }
}
