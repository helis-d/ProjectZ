using UnityEngine;
using FishNet.Object;

namespace ProjectZ.VFX
{
    /// <summary>
    /// Koşarken kamera FOV'unu genişleterek hız hissi verir.
    /// PlayerMovement.CurrentSpeed ve PlayerInputHandler.IsSprinting izler.
    /// 
    /// KURULUM: Player prefab'ına ekle. Otomatik çalışır.
    /// </summary>
    public class SprintFOVEffect : NetworkBehaviour
    {
        [Header("FOV Settings")]
        [SerializeField] private float _normalFOV = 60f;
        [SerializeField] private float _sprintFOV = 72f;
        [SerializeField] private float _transitionSpeed = 8f;

        private Camera _cam;
        private Player.PlayerInputHandler _input;
        private Player.PlayerMovement _movement;
        private float _targetFOV;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _input = GetComponent<Player.PlayerInputHandler>();
            _movement = GetComponent<Player.PlayerMovement>();
            _cam = Camera.main;

            if (_cam != null)
                _normalFOV = _cam.fieldOfView;

            _targetFOV = _normalFOV;
        }

        private void Update()
        {
            if (_cam == null || _input == null) return;

            bool isSprinting = _input.IsSprinting && _input.MoveInput.magnitude > 0.3f;

            _targetFOV = isSprinting ? _sprintFOV : _normalFOV;

            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetFOV, Time.deltaTime * _transitionSpeed);
        }

        /// <summary>
        /// ADS (nişan alma) modunda FOV'u daraltmak için dışarıdan çağrılır.
        /// </summary>
        public void SetADSFOV(float adsFOV)
        {
            _targetFOV = adsFOV;
        }

        /// <summary>
        /// ADS'den çıkınca normal FOV'a döndür.
        /// </summary>
        public void ResetFOV()
        {
            _targetFOV = _normalFOV;
        }
    }
}
