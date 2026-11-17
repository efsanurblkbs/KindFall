using System;
using System.Collections;
using UnityEngine;
using KindFall.Core.Interfaces;

namespace KindFall.Mechanics.Puzzle
{
    /// <summary>
    /// Prenses'in kılıç vuruşlarıyla (IDamageable) hasar alıp kırılan asma kilitli kurtarma kafesi.
    /// Hasar aldığında görsel tepki verir; canı sıfırlandığında kırılır ve Observer olayını tetikler.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CageController : MonoBehaviour, IDamageable
    {
        [Header("Kafes Dayanıklılık Ayarları")]
        [Tooltip("Kafesin kırılması için gereken toplam can (Prenses vuruşu varsayılan 25 hasardır).")]
        [SerializeField] private float maxHealth = 50f;

        [Tooltip("Kırıldığında kafesin engelleyici collider'ı kapatılsın mı?")]
        [SerializeField] private bool disableColliderOnBreak = true;

        [Header("Görsel Geri Bildirim (Opsiyonel)")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite intactSprite;
        [SerializeField] private Sprite brokenSprite;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float hitFlashDuration = 0.1f;

        /// <summary>
        /// Kafesin güncel can değeri.
        /// </summary>
        public float CurrentHealth { get; private set; }

        /// <summary>
        /// Kafes kırıldı mı?
        /// </summary>
        public bool IsBroken { get; private set; }

        /// <summary>
        /// Kafes hasar aldığında tetiklenir (Güncel Can, Maksimum Can).
        /// </summary>
        public event Action<float, float> OnCageDamaged;

        /// <summary>
        /// Kafes tamamen kırıldığında tetiklenen Observer olayı.
        /// </summary>
        public event Action OnCageBroken;

        private Collider2D cageCollider;
        private Color originalColor = Color.white;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            cageCollider = GetComponent<Collider2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                if (intactSprite != null)
                {
                    spriteRenderer.sprite = intactSprite;
                }
            }

            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// IDamageable uygulaması: Kafese hasar uygular ve canı biterse kafesi kırar.
        /// </summary>
        /// <param name="damage">Gelen hasar miktarı.</param>
        /// <param name="hitDirection">Darbe yönü.</param>
        public void TakeDamage(float damage, Vector2 hitDirection)
        {
            if (IsBroken || damage <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            OnCageDamaged?.Invoke(CurrentHealth, maxHealth);

            TriggerHitFlash();

            if (CurrentHealth <= 0f)
            {
                BreakCage();
            }
        }

        private void BreakCage()
        {
            IsBroken = true;

            if (disableColliderOnBreak && cageCollider != null)
            {
                cageCollider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
                if (brokenSprite != null)
                {
                    spriteRenderer.sprite = brokenSprite;
                }
            }

            OnCageBroken?.Invoke();
        }

        private void TriggerHitFlash()
        {
            if (spriteRenderer == null) return;

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);

            if (!IsBroken && spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            flashCoroutine = null;
        }
    }
}
