using ProjectZ.Weapon;
using UnityEngine;

/// <summary>
/// Silahı karakter eline bağlar.
/// Bu scripti karakter root objesine ekle.
/// Her silah türü için offset WeaponData'dan okunur.
/// </summary>
public class WeaponAttachment : MonoBehaviour
{
    [Header("Hand Bones - Inspector'dan ata")]
    public Transform rightHandBone;         // örn: mixamorig:RightHand
    public Transform leftHandBone;          // örn: mixamorig:LeftHand

    [Header("Active Weapon")]
    public BaseWeapon currentWeapon;

    [Header("IK Settings")]
    public bool useLeftHandIK = true;       // sol el silahı tutma pozisyonu

    // Animator IK callback için
    private Animator characterAnimator;

    // Bıçak için ayrı attachment noktası (daha kısa)
    [Header("Knife Offset Override")]
    public Vector3 knifeExtraOffset = new Vector3(0f, 0f, 0.1f);

    void Awake()
    {
        characterAnimator = GetComponent<Animator>();
    }

    /// <summary>
    /// Silahı sağ ele bağla. WeaponManager'dan çağrılır.
    /// </summary>
    public void AttachWeapon(BaseWeapon weapon)
    {
        if (currentWeapon != null)
            currentWeapon.Holster();

        currentWeapon = weapon;

        if (weapon == null || rightHandBone == null) return;

        // Silahı sağ el kemiğinin child'ı yap
        weapon.transform.SetParent(rightHandBone, false);

        // WeaponData'dan offset uygula
        weapon.transform.localPosition = weapon.data.rightHandPositionOffset;
        weapon.transform.localRotation = Quaternion.Euler(weapon.data.rightHandRotationOffset);

        // Bıçak için ek offset
        if (weapon.data.weaponType == WeaponType.Knife)
            weapon.transform.localPosition += knifeExtraOffset;

        weapon.Draw();
    }

    /// <summary>
    /// Animator IK: sol el silahı tutma pozisyonu için
    /// </summary>
    void OnAnimatorIK(int layerIndex)
    {
        if (!useLeftHandIK || currentWeapon == null || characterAnimator == null) return;
        if (currentWeapon.data.weaponType == WeaponType.Knife) return; // bıçakta sol el IK yok

        // Sol el IK weight'i
        characterAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
        characterAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);

        // Sol el hedef pozisyonu: sağ elden offset
        Vector3 leftHandTarget = rightHandBone.position
            + rightHandBone.TransformDirection(currentWeapon.data.leftHandPositionOffset);
        Quaternion leftHandRot = rightHandBone.rotation
            * Quaternion.Euler(currentWeapon.data.leftHandRotationOffset);

        characterAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget);
        characterAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRot);
    }

    /// <summary>
    /// LateUpdate: animasyon sonrası silah pozisyonunu garanti et (T-pose bug fix)
    /// </summary>
    void LateUpdate()
    {
        if (currentWeapon == null || rightHandBone == null) return;

        // Silah zaten child olduğu için bu genelde gerekmez,
        // ama floating/glitch durumunda manuel override:
        if (currentWeapon.transform.parent != rightHandBone)
        {
            currentWeapon.transform.SetParent(rightHandBone, false);
            currentWeapon.transform.localPosition = currentWeapon.data.rightHandPositionOffset;
            currentWeapon.transform.localRotation = Quaternion.Euler(currentWeapon.data.rightHandRotationOffset);
        }
    }
}
