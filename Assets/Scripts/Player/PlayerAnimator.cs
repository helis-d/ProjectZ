using UnityEngine;
using FishNet.Object;

namespace ProjectZ.Player
{
    /// <summary>
    /// Drives the character's Animator based on movement input.
    /// Works with the AnimatorController created by PlayerAnimatorSetup.
    /// Attach this to the Player prefab.
    /// </summary>
    public class PlayerAnimator : NetworkBehaviour
    {
        [Header("References")]
        [Tooltip("Human-Custom modelinin üzerindeki Animator. Boş bırakırsan otomatik bulur.")]
        [SerializeField] private Animator _animator;

        private PlayerInputHandler _input;
        private PlayerMovement _movement;

        // Animator parameter hash'leri (performans için)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        public override void OnStartClient()
        {
            base.OnStartClient();

            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();

            // Inspector'dan yanlış atanmış olma ihtimaline karşı GÜÇLÜ KORUMA
            // SADECE 'Human-Custom' modelini bul ve kodla ata. Inspector'u hiçe say.
            Transform humanCustom = transform.Find("Human-Custom");
            if (humanCustom != null)
            {
                _animator = humanCustom.GetComponent<Animator>();
            }
            else
            {
                _animator = null; // Yanlış animator'ü sil
            }

            if (_animator == null)
            {
                Debug.LogError("[PlayerAnimator] Animator bulunamadı! 'Human-Custom' isimli alt obje yok veya Animator bileşeni eksik.");
            }
            else
            {
                // ─── T-POSE TEŞHİS SİSTEMİ ───
                if (_animator.runtimeAnimatorController == null)
                {
                    Debug.LogError("🚨 [T-POSE SEBEBİ BULUNDU!] Human-Custom objesindeki Animator'ün beyni (Controller) YOK! (None yazıyor). Lütfen Inspector'dan elinle PlayerAnimatorController'ı sürükle.");
                }
                else if (_animator.avatar == null)
                {
                    Debug.LogError("🚨 [T-POSE SEBEBİ BULUNDU!] Human-Custom objesindek Animator'ün 'Avatar' kısmı boş (None)! Lütfen o modelin Rig ayarlarından Avatar'ı oluşturduğuna emin ol.");
                }
                else
                {
                    Debug.Log("✅ [PlayerAnimator] Animator, Controller ve Avatar kusursuz çalışıyor!");
                }
            }
        }

        private void Update()
        {
            if (_animator == null || _input == null || !IsOwner) return;

            float speed = _input.MoveInput.magnitude;
            bool isMoving = speed > 0.1f;

            _animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
            _animator.SetBool(IsMovingHash, isMoving);

            if (_input.FirePressed)
            {
                _animator.SetTrigger("Fire");
            }
        }
    }
}
