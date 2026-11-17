using UnityEngine;

namespace KindFall.Core.Interfaces
{
    /// <summary>
    /// Hasar alabilen veya kırılabilen tüm varlıkların (düşman, kurtarma kafesi vb.)
    /// uygulaması gereken ortak arayüz.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Nesneye belirtilen miktarda hasar uygular.
        /// </summary>
        /// <param name="damage">Uygulanacak hasar miktarı.</param>
        /// <param name="hitDirection">Saldırının geldiği yön vektörü (geri tepme veya efekt yönü için).</param>
        void TakeDamage(float damage, Vector2 hitDirection);
    }
}
