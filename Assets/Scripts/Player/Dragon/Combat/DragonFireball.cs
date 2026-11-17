using System;
using UnityEngine;
using KindFall.Core.Interfaces;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderha tarafından fırlatılan küçük ateş topu (Fireball) mermisi.
    /// Modüler Prefab yapısına uygun olarak Unity 6 linearVelocity ile hareket eder
    /// ve çarpışma anında Observer olayı fırlatır.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class DragonFireball : MonoBehaviour
    {
        [Header("Ateş Topu Parametreleri")]
        [Tooltip("Ateş topunun hareket hızı")]
        [SerializeField] private float speed = 12f;

        [Tooltip("Ateş topunun vereceği hasar")]
        [SerializeField] private float damage = 15f;

        [Tooltip("Çarpışma olmazsa merminin sahneden silinme süresi (sn)")]
        [SerializeField] private float lifeTime = 2.5f;

        [Tooltip("Ateş topunun etkileşime gireceği katmanlar (Düşman, Duvar, Zemin, Bulmaca vb.)")]
        [SerializeField] private LayerMask hitLayer;

        /// <summary>
        /// Ateş topu bir hedefe veya yüzeye çarptığında tetiklenen Observer olayı (Çarpılan Collider, Hasar).
        /// </summary>
        public event Action<Collider2D, float> OnFireballHit;

        private Rigidbody2D rb;
        private Collider2D col;
        private Vector2 moveDirection = Vector2.right;
        private GameObject ownerObject;
        private bool hasHit;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            col.isTrigger = true;
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        /// <summary>
        /// Ateş topunu fırlatma yönü ve sahibi ile başlatır.
        /// </summary>
        /// <param name="direction">Merminin ilerleyeceği yön vektörü.</param>
        /// <param name="owner">Mermiyi atan karakter (kendi collider'ına çarpmasını önlemek için).</param>
        public void Launch(Vector2 direction, GameObject owner = null)
        {
            ownerObject = owner;
            moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;

            if (rb != null)
            {
                rb.linearVelocity = moveDirection * speed;
            }

            if (moveDirection.x < 0f)
            {
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;
            if (ownerObject != null && (other.gameObject == ownerObject || other.transform.IsChildOf(ownerObject.transform)))
            {
                return;
            }

            if (hitLayer.value == 0 || ((1 << other.gameObject.layer) & hitLayer.value) != 0)
            {
                hasHit = true;

                if (other.TryGetComponent(out IBurnable burnable))
                {
                    burnable.Burn(damage);
                }

                OnFireballHit?.Invoke(other, damage);
                Destroy(gameObject);
            }
        }
    }
}

