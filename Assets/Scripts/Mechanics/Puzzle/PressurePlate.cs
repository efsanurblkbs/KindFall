using System;
using System.Collections.Generic;
using UnityEngine;
using KindFall.Core.Interfaces;
using KindFall.Player.Princess;

namespace KindFall.Mechanics.Puzzle
{
    /// <summary>
    /// Prenses veya belirlenen ağır nesneler üzerine bastığında aktive olan ağırlık butonu.
    /// Observer Pattern (OnPressed, OnReleased, OnStateChanged) ile kapıları ve mekanizmaları tetikler.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour, IPuzzleTrigger
    {
        [Header("Algılama Ayarları")]
        [Tooltip("İşaretliyse yalnızca Prenses karakteri butonu tetikleyebilir; kapalıysa Heavy katmanındaki nesneler de basabilir.")]
        [SerializeField] private bool onlyPrincess = false;

        [Tooltip("Ağır nesneler için katman maskesi (onlyPrincess kapalıyken kullanılır)")]
        [SerializeField] private LayerMask heavyObjectLayer;

        [Tooltip("Buton bırakıldıktan sonra kapanmadan önce beklenecek kısa süre (sn)")]
        [SerializeField] private float releaseDelay = 0.05f;

        [Header("Görsel Geri Bildirim (Opsiyonel)")]
        [SerializeField] private Transform plateVisual;
        [SerializeField] private Vector3 pressedOffset = new Vector3(0f, -0.08f, 0f);
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color releasedColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(0.4f, 0.9f, 0.5f, 1f);

        /// <summary>
        /// Buton şu anda basılı mı?
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Butona ilk kez basıldığında tetiklenir.
        /// </summary>
        public event Action OnPressed;

        /// <summary>
        /// Buton üzerindeki tüm ağırlık kalktığında tetiklenir.
        /// </summary>
        public event Action OnReleased;

        /// <summary>
        /// Buton durumu değiştiğinde tetiklenir (true: Basılı, false: Serbest).
        /// </summary>
        public event Action<bool> OnStateChanged;

        private readonly HashSet<Collider2D> activeColliders = new HashSet<Collider2D>();
        private Collider2D triggerCollider;
        private Vector3 initialVisualPosition;
        private float releaseTimer;
        private bool pendingRelease;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;

            if (plateVisual != null)
            {
                initialVisualPosition = plateVisual.localPosition;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            UpdateVisuals(false);
        }

        private void Update()
        {
            CleanupDestroyedColliders();

            if (pendingRelease)
            {
                releaseTimer -= Time.deltaTime;
                if (releaseTimer <= 0f)
                {
                    pendingRelease = false;
                    if (activeColliders.Count == 0 && IsActive)
                    {
                        SetActiveState(false);
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;

            activeColliders.Add(other);
            pendingRelease = false;

            if (!IsActive)
            {
                SetActiveState(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!activeColliders.Remove(other)) return;

            if (activeColliders.Count == 0 && IsActive)
            {
                if (releaseDelay <= 0f)
                {
                    SetActiveState(false);
                }
                else
                {
                    pendingRelease = true;
                    releaseTimer = releaseDelay;
                }
            }
        }

        private bool IsValidActivator(Collider2D other)
        {
            if (other == null) return false;

            if (other.GetComponentInParent<PrincessController>() != null)
            {
                return true;
            }

            if (onlyPrincess)
            {
                return false;
            }

            return heavyObjectLayer.value != 0 && ((1 << other.gameObject.layer) & heavyObjectLayer.value) != 0;
        }

        private void CleanupDestroyedColliders()
        {
            if (activeColliders.Count == 0) return;

            activeColliders.RemoveWhere(col => col == null || !col.gameObject.activeInHierarchy);

            if (activeColliders.Count == 0 && IsActive && !pendingRelease)
            {
                SetActiveState(false);
            }
        }

        private void SetActiveState(bool active)
        {
            if (IsActive == active) return;

            IsActive = active;
            UpdateVisuals(active);

            if (active)
            {
                OnPressed?.Invoke();
            }
            else
            {
                OnReleased?.Invoke();
            }

            OnStateChanged?.Invoke(active);
        }

        private void UpdateVisuals(bool pressed)
        {
            if (plateVisual != null)
            {
                plateVisual.localPosition = pressed ? initialVisualPosition + pressedOffset : initialVisualPosition;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = pressed ? pressedColor : releasedColor;
            }
        }
    }
}
