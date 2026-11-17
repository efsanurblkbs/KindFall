using System;
using System.Collections.Generic;
using UnityEngine;
using KindFall.Core.Interfaces;

namespace KindFall.Mechanics.Puzzle
{
    /// <summary>
    /// Birden fazla bulmaca tetikleyicisini (PressurePlate, Ejderha ateş kristali vb.)
    /// Observer Pattern ile dinleyen ve tüm tetikleyiciler aktif olduğunda açılan senkronize kapı.
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [Header("Tetikleyici Bağlantıları")]
        [Tooltip("Kapının açılması için aktif olması gereken tüm tetikleyiciler (IPuzzleTrigger uygulayan bileşenler).")]
        [SerializeField] private List<MonoBehaviour> triggerSources = new List<MonoBehaviour>();

        [Tooltip("İşaretliyse kapı bir kez açıldığında tetikleyiciler bırakılsa bile açık kalır.")]
        [SerializeField] private bool stayOpenOnceOpened = false;

        [Header("Kapı Hareketi")]
        [Tooltip("Hareket edecek kapı görseli/objesi (boşsa bu GameObject'in kendi transform'u hareket eder).")]
        [SerializeField] private Transform doorTransform;

        [Tooltip("Kapı açıldığında kapalı konumuna göre gideceği yer değiştirme miktarı.")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);

        [Tooltip("Kapının açılma ve kapanma hızı.")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Kapı açıldığında geçit verecek olan engelleyici collider (opsiyonel).")]
        [SerializeField] private Collider2D doorCollider;

        [Tooltip("İşaretliyse demirler yukarı kalkarken dikey ölçeği küçülerek kemerin içine gizlenir.")]
        [SerializeField] private bool shrinkYOnOpen = false;

        [Header("Kemer İçi Parmaklık Çekme (Opsiyonel)")]
        [Tooltip("Demir parmaklıkların SpriteRenderer bileşeni.")]
        [SerializeField] private SpriteRenderer barsRenderer;

        [Tooltip("İşaretliyse demirler ezilmeden/katlanmadan üst kemerin içine doğru düz yukarı çekilir.")]
        [SerializeField] private bool clipBarsIntoArch = false;

        /// <summary>
        /// Kapının şu anda açık olup olmadığını döner.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Kapı açıldığında veya kapandığında tetiklenen Observer olayı (true: Açıldı, false: Kapandı).
        /// </summary>
        public event Action<bool> OnDoorStateChanged;

        private readonly List<IPuzzleTrigger> triggers = new List<IPuzzleTrigger>();
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private Vector3 closedScale;
        private float totalOffsetDistance;
        private bool hasInitializedPositions;
        private Sprite[] cachedBarSprites;
        private float openProgress;

        private void Awake()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }

            closedPosition = doorTransform.localPosition;
            openPosition = closedPosition + openOffset;
            closedScale = doorTransform.localScale;
            totalOffsetDistance = openOffset.magnitude;
            hasInitializedPositions = true;

            if (clipBarsIntoArch && barsRenderer != null && barsRenderer.sprite != null)
            {
                BuildClippedBarCache(barsRenderer.sprite);
            }

            CollectTriggers();
        }

        private void BuildClippedBarCache(Sprite sourceSprite)
        {
            Texture2D tex = sourceSprite.texture;
            Rect r = sourceSprite.rect;
            int totalH = Mathf.Max(1, Mathf.RoundToInt(r.height));
            float ppu = sourceSprite.pixelsPerUnit;
            Vector2 topPivot = new Vector2(0.5f, 1f);

            cachedBarSprites = new Sprite[totalH];
            for (int i = 0; i < totalH; i++)
            {
                int visibleH = totalH - i;
                Rect subRect = new Rect(r.x, r.y, r.width, visibleH);
                cachedBarSprites[i] = Sprite.Create(tex, subRect, topPivot, ppu, 0, SpriteMeshType.FullRect);
            }
        }

        private void OnEnable()
        {
            SubscribeToTriggers();
            EvaluateDoorState();
        }

        private void OnDisable()
        {
            UnsubscribeFromTriggers();
        }

        private void Update()
        {
            if (!hasInitializedPositions) return;

            if (clipBarsIntoArch && cachedBarSprites != null && cachedBarSprites.Length > 0 && barsRenderer != null)
            {
                float targetProgress = IsOpen ? 1f : 0f;
                openProgress = Mathf.MoveTowards(openProgress, targetProgress, moveSpeed * Time.deltaTime);

                if (openProgress >= 0.99f)
                {
                    barsRenderer.enabled = false;
                }
                else
                {
                    barsRenderer.enabled = true;
                    int idx = Mathf.Clamp(Mathf.RoundToInt(openProgress * (cachedBarSprites.Length - 1)), 0, cachedBarSprites.Length - 1);
                    barsRenderer.sprite = cachedBarSprites[idx];
                }
                return;
            }

            Vector3 targetPosition = IsOpen ? openPosition : closedPosition;
            if (Vector3.Distance(doorTransform.localPosition, targetPosition) > 0.001f)
            {
                doorTransform.localPosition = Vector3.MoveTowards(
                    doorTransform.localPosition,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );
            }
            else
            {
                doorTransform.localPosition = targetPosition;
            }

            if (shrinkYOnOpen && totalOffsetDistance > 0.001f)
            {
                float progress = Vector3.Distance(doorTransform.localPosition, closedPosition) / totalOffsetDistance;
                Vector3 currentScale = closedScale;
                currentScale.y = closedScale.y * Mathf.Clamp01(1f - progress);
                doorTransform.localScale = currentScale;
            }
        }

        private void CollectTriggers()
        {
            triggers.Clear();
            foreach (MonoBehaviour source in triggerSources)
            {
                if (source == null) continue;

                if (source is IPuzzleTrigger trigger)
                {
                    triggers.Add(trigger);
                }
                else
                {
                    Debug.LogWarning($"[DoorController] '{source.name}' IPuzzleTrigger arayüzünü uygulamıyor ve yoksayıldı.", this);
                }
            }
        }

        private void SubscribeToTriggers()
        {
            foreach (IPuzzleTrigger trigger in triggers)
            {
                trigger.OnStateChanged += HandleTriggerStateChanged;
            }
        }

        private void UnsubscribeFromTriggers()
        {
            foreach (IPuzzleTrigger trigger in triggers)
            {
                trigger.OnStateChanged -= HandleTriggerStateChanged;
            }
        }

        private void HandleTriggerStateChanged(bool _)
        {
            EvaluateDoorState();
        }

        /// <summary>
        /// Bağlı tüm tetikleyicilerin durumunu kontrol ederek kapının açık/kapalı durumunu günceller.
        /// </summary>
        public void EvaluateDoorState()
        {
            if (IsOpen && stayOpenOnceOpened) return;
            if (triggers.Count == 0) return;

            bool allActive = true;
            foreach (IPuzzleTrigger trigger in triggers)
            {
                if (!trigger.IsActive)
                {
                    allActive = false;
                    break;
                }
            }

            if (IsOpen != allActive)
            {
                IsOpen = allActive;
                if (doorCollider != null)
                {
                    doorCollider.enabled = !IsOpen;
                }
                OnDoorStateChanged?.Invoke(IsOpen);
            }
        }

        private void OnValidate()
        {
            for (int i = 0; i < triggerSources.Count; i++)
            {
                MonoBehaviour source = triggerSources[i];
                if (source != null && !(source is IPuzzleTrigger))
                {
                    // GameObject üzerinde IPuzzleTrigger uygulayan başka bir bileşen varsa onu otomatik bul
                    IPuzzleTrigger found = source.GetComponent<IPuzzleTrigger>();
                    if (found is MonoBehaviour foundMono)
                    {
                        triggerSources[i] = foundMono;
                    }
                    else
                    {
                        Debug.LogWarning($"[DoorController] '{source.name}' üzerinde IPuzzleTrigger uygulayan bir script bulunamadı.", this);
                    }
                }
            }
        }
    }
}
