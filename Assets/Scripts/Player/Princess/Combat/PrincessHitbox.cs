using System;
using UnityEngine;
using KindFall.Core.Interfaces;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin kılıç vuruş alanını (Hitbox) yöneten modüler bileşen.
    /// Düşmanlara veya bulmaca tetikleyicilerine temas ettiğinde Observer olayı fırlatır.
    /// RULES.md uyarınca tekil/modüler tasarım ve Observer Pattern prensiplerine uygundur.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PrincessHitbox : MonoBehaviour
    {
        [Tooltip("Vuruşun vereceği temel hasar")]
        [SerializeField] private float damage = 25f;

        [Tooltip("Vurulabilir nesne katmanları (Düşman, Bulmaca Tetikleyicisi, Kırılabilir Obje vb.)")]
        [SerializeField] private LayerMask hitLayer;

        [Tooltip("Vuruş ile nesneye uygulanacak hafif geri tepme kuvveti")]
        [SerializeField] private float knockbackForce = 4f;

        /// <summary>
        /// Bir hedefe (düşman veya bulmaca nesnesi) vurulduğunda tetiklenen Observer olayı.
        /// (Vurulan Collider, Hasar Miktarı, Geri Tepme Kuvveti).
        /// </summary>
        public event Action<Collider2D, float, float> OnHitTarget;

        private Collider2D hitboxCollider;

        private void Awake()
        {
            hitboxCollider = GetComponent<Collider2D>();
            hitboxCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Belirlenen katmandaki bir objeyle temas edildi mi?
            if (((1 << other.gameObject.layer) & hitLayer.value) == 0)
            {
                return;
            }

            Vector2 hitDirection = (other.transform.position - transform.position).normalized;
            if (hitDirection.sqrMagnitude < 0.001f)
            {
                hitDirection = transform.lossyScale.x >= 0f ? Vector2.right : Vector2.left;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage, hitDirection);

            OnHitTarget?.Invoke(other, damage, knockbackForce);
        }
    }
}

