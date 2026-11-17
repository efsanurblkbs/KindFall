using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using KindFall.Core.Interfaces;
using KindFall.Player.Princess;

namespace KindFall.World.Puzzles
{
    /// <summary>
    /// Yavru Ejderhanın havada ateş topuyla aktif ettiği asimetrik bulmaca kristali.
    /// Yakılmadan önce fiziksel engel olarak Ejderhanın geçişini engeller;
    /// yakıldığında geçiş açılır ve alttan Prenses'e hasar verebilen lav damlaları damlatır.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FireTriggerCrystal : MonoBehaviour, IBurnable
    {
        [Header("Kristal Ayarları")]
        [Tooltip("True ise bir kez yakıldıktan sonra aktif kalır; false ise süre dolunca söner")]
        [SerializeField] private bool stayLitPermanently = true;

        [Tooltip("Kalıcı değilse aktif kalma süresi (sn)")]
        [SerializeField] private float activeDuration = 5f;

        [Header("Havada Süzülme")]
        [SerializeField] private float hoverAmplitude = 0.06f;
        [SerializeField] private float hoverSpeed = 2.4f;

        [Header("Lav Damlama Tehlikesi")]
        [SerializeField] private bool dripLavaWhenLit = true;
        [SerializeField] private float lavaDripInterval = 0.28f;
        [SerializeField] private float lavaDamage = 10f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Görsel & Işık Geri Bildirimi")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite unlitSprite;
        [SerializeField] private Sprite litSprite;
        [SerializeField] private Light2D glowLight;
        [SerializeField] private Collider2D barrierCollider;
        [SerializeField] private SpriteRenderer barrierVisual;
        [SerializeField] private Collider2D lowerBarrierCollider;
        [SerializeField] private SpriteRenderer lowerBarrierVisual;
        [SerializeField] private Sprite[] barrierFrames;
        [SerializeField] private Color unlitColor = new Color(0.35f, 0.45f, 0.65f, 1f);
        [SerializeField] private Color litColor = new Color(1f, 0.55f, 0.15f, 1f);
        [SerializeField] private Animator anim;

        /// <summary>
        /// Kristal ateş topuyla yakıldığında tetiklenir.
        /// </summary>
        public event Action<FireTriggerCrystal> OnActivated;

        /// <summary>
        /// Süreli kristal söndüğünde tetiklenir.
        /// </summary>
        public event Action<FireTriggerCrystal> OnDeactivated;

        public bool IsBurned { get; private set; }
        public bool IsActivated => IsBurned;

        private Collider2D crystalCollider;
        private float extinguishTime;
        private float nextDripTime;
        private Vector3 startPos;
        private Texture2D dropTexture;
        private Sprite dropSprite;

        private void Awake()
        {
            crystalCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");

            startPos = transform.position;
            CreateLavaDropSprite();
            UpdateVisual(false);
            UpdateBarrierState(false);
        }

        private void Start()
        {
            IgnorePrincessBarrierCollisions();
        }

        private void OnDestroy()
        {
            if (dropSprite != null) Destroy(dropSprite);
            if (dropTexture != null) Destroy(dropTexture);
        }

        private void Update()
        {
            if (hoverAmplitude > 0f)
            {
                float offsetY = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
                transform.position = startPos + new Vector3(0f, offsetY, 0f);
            }

            if (!IsBurned && barrierFrames != null && barrierFrames.Length > 0)
            {
                int frameIdx = Mathf.FloorToInt(Time.time * 9f) % barrierFrames.Length;
                if (barrierVisual != null) barrierVisual.sprite = barrierFrames[frameIdx];
                if (lowerBarrierVisual != null) lowerBarrierVisual.sprite = barrierFrames[(frameIdx + 1) % barrierFrames.Length];
            }

            if (IsBurned)
            {
                if (dripLavaWhenLit && Time.time >= nextDripTime)
                {
                    nextDripTime = Time.time + lavaDripInterval;
                    StartCoroutine(LavaDropRoutine());
                }

                if (!stayLitPermanently && Time.time >= extinguishTime)
                {
                    Deactivate();
                }
            }
        }

        /// <summary>
        /// Ejderhanın ateş topu kristale çarptığında tetiklenir.
        /// </summary>
        /// <param name="fireDamage">Ateş topu etki değeri.</param>
        public void Burn(float fireDamage)
        {
            if (!IsBurned && barrierFrames != null && barrierFrames.Length > 0)
            {
                int frameIdx = Mathf.FloorToInt(Time.time * 9f) % barrierFrames.Length;
                if (barrierVisual != null) barrierVisual.sprite = barrierFrames[frameIdx];
                if (lowerBarrierVisual != null) lowerBarrierVisual.sprite = barrierFrames[(frameIdx + 1) % barrierFrames.Length];
            }

            if (IsBurned)
            {
                if (!stayLitPermanently)
                {
                    extinguishTime = Time.time + activeDuration;
                }
                return;
            }

            IsBurned = true;
            nextDripTime = Time.time + 0.05f;

            if (!stayLitPermanently)
            {
                extinguishTime = Time.time + activeDuration;
            }

            UpdateVisual(true);
            UpdateBarrierState(true);

            if (anim != null)
            {
                anim.SetBool("IsLit", true);
            }

            OnActivated?.Invoke(this);
        }

        private void Deactivate()
        {
            IsBurned = false;
            UpdateVisual(false);
            UpdateBarrierState(false);

            if (anim != null)
            {
                anim.SetBool("IsLit", false);
            }

            OnDeactivated?.Invoke(this);
        }

        private void UpdateBarrierState(bool lit)
        {
            if (crystalCollider != null)
            {
                // Sönükken katı engel (Ejderha geçemez); yakılınca trigger olur ve geçiş açılır
                crystalCollider.isTrigger = lit;
            }

            // Üst ve alt büyülü enerji bariyerlerini aç/kapat (Ejderha üstten de alttan da geçemesin)
            if (barrierCollider != null) barrierCollider.enabled = !lit;
            if (barrierVisual != null) barrierVisual.enabled = !lit;
            if (lowerBarrierCollider != null) lowerBarrierCollider.enabled = !lit;
            if (lowerBarrierVisual != null) lowerBarrierVisual.enabled = !lit;
        }

        private void IgnorePrincessBarrierCollisions()
        {
            // Kristal ve enerji bariyerleri Prenses'e asla engel olmaz; geçip Ejderhayı bekleyebilir
            PrincessController[] allPrincesses = FindObjectsByType<PrincessController>(FindObjectsSortMode.None);
            foreach (PrincessController p in allPrincesses)
            {
                if (p.MainCollider == null) continue;
                if (crystalCollider != null) Physics2D.IgnoreCollision(crystalCollider, p.MainCollider, true);
                if (lowerBarrierCollider != null) Physics2D.IgnoreCollision(lowerBarrierCollider, p.MainCollider, true);
                if (barrierCollider != null) Physics2D.IgnoreCollision(barrierCollider, p.MainCollider, true);
            }
        }

        private void UpdateVisual(bool lit)
        {
            if (spriteRenderer != null)
            {
                if (unlitSprite != null && litSprite != null)
                {
                    spriteRenderer.sprite = lit ? litSprite : unlitSprite;
                    spriteRenderer.color = Color.white;
                }
                else
                {
                    spriteRenderer.color = lit ? litColor : unlitColor;
                }
            }

            if (glowLight != null)
            {
                glowLight.intensity = lit ? 1.4f : 0.15f;
                glowLight.color = lit ? new Color(1f, 0.62f, 0.18f, 1f) : new Color(0.45f, 0.72f, 1f, 1f);
            }
        }

        private void CreateLavaDropSprite()
        {
            dropTexture = new Texture2D(2, 3, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            Color core = new Color(1f, 0.90f, 0.45f, 1f);
            Color rim = new Color(1f, 0.42f, 0.08f, 1f);
            dropTexture.SetPixels(new[]
            {
                rim, rim,
                core, core,
                rim, core
            });
            dropTexture.Apply();
            dropSprite = Sprite.Create(dropTexture, new Rect(0, 0, 2, 3), new Vector2(0.5f, 0.5f), 32f);
        }

        private IEnumerator LavaDropRoutine()
        {
            if (dropSprite == null) yield break;

            GameObject dropObj = new GameObject("LavaDrop");
            Vector3 spawnPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.10f, 0.10f), -0.44f, 0f);
            dropObj.transform.position = spawnPos;

            SpriteRenderer sr = dropObj.AddComponent<SpriteRenderer>();
            sr.sprite = dropSprite;
            sr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 6;

            float floorY = spawnPos.y - 4.5f;
            if (groundLayer.value != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, 12f, groundLayer);
                if (hit.collider != null)
                {
                    floorY = hit.point.y + 0.03f;
                }
            }

            float velY = -0.35f;
            bool hitPrincess = false;

            while (dropObj != null && dropObj.transform.position.y > floorY)
            {
                velY -= 6.5f * Time.deltaTime;
                Vector3 pos = dropObj.transform.position;
                pos.y = Mathf.Max(floorY, pos.y + velY * Time.deltaTime);
                dropObj.transform.position = pos;

                // Prenses teması kontrolü (Lav damlası değerse Prenses ölür)
                Collider2D[] overlaps = Physics2D.OverlapCircleAll(pos, 0.14f);
                for (int i = 0; i < overlaps.Length; i++)
                {
                    if (overlaps[i].TryGetComponent(out PrincessController princess))
                    {
                        princess.Die();
                        hitPrincess = true;
                        break;
                    }
                }

                if (hitPrincess) break;
                yield return null;
            }

            if (dropObj != null)
            {
                dropObj.transform.localScale = new Vector3(1.8f, 0.45f, 1f);
                float splashElapsed = 0f;
                const float splashDuration = 0.25f;
                while (splashElapsed < splashDuration && dropObj != null)
                {
                    splashElapsed += Time.deltaTime;
                    float a = 1f - (splashElapsed / splashDuration);
                    sr.color = new Color(1f, 0.55f, 0.15f, a);
                    yield return null;
                }

                if (dropObj != null) Destroy(dropObj);
            }
        }
    }
}
