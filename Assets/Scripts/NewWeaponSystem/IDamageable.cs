using UnityEngine;

namespace ProjectZ.Weapon
{
    /// <summary>
    /// Hasar alabilecek her obje bu interface'i implemente eder.
    /// Player, AI, araçlar, barrel vb.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
        void Die();
    }
}
