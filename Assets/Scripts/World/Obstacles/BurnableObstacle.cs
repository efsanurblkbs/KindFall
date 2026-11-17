using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using KindFall.Core.Interfaces;
using KindFall.Player.Princess;

namespace KindFall.World.Obstacles
{
    /// <summary>
    /// Ejderhanın ateş topuyla yakılan dikenli sarmaşık veya eritilen buz bariyerlerini yöneten modüler bileşen.
    /// IBurnable arayüzünü uygular; tutuştuğunda alev kareleri, 2D ışık parlaması ve kömürleşme efekti üretir.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BurnableObstacle : MonoBehaviour, IBurnable
    {
        public enum ObstacleType
        {
            Vine,
            Ice
        }

        [Header("Engel Ayarları")]
        [Tooltip("Engelin türü (Dikenli Sarmaşık veya Buz Bariyeri)")]
        [SerializeField] private ObstacleType obstacleType = ObstacleType.Vine;

        [Tooltip("Engelin tamamen yok olması için gereken dayanıklılık")]
        [SerializeField] private float durability = 15f;

        [Tooltip("Tutuşma ve yanma süresi (sn)")]
        [SerializeField] private float burnDuration = 0.85f;

        [Tooltip("True ise yanma/erime bitince obje sahneden silinir; false ise kömürleşip zararsız hale gelir (geçiş açılır)")]
        [SerializeField] private bool destroyAfterBurn = false;

        [Header("Yanma & Alevlenme Efektleri")]
        [Tooltip("Yanma anında sarmaşık üzerinde belirecek animasyonlu alev kareleri")]
        [SerializeField] private Sprite[] flameSprites;

        [Tooltip("Buz bariyerinin kare kare erime animasyonu sprite'ları")]
        [SerializeField] private Sprite[] meltFrames;

        [Tooltip("Yanma sonrası dikenlerinden arınmış/solmuş sarmaşık görseli")]
        [SerializeField] private Sprite burnedSprite;

        [Tooltip("Tutuşma anında ilk parlama rengi")]
        [SerializeField] private Color igniteColor = new Color(1f, 0.45f, 0.12f, 1f);

        [Tooltip("Yanma sonrası solmuş/kavrulmuş bitki görünümü rengi (burnedSprite varsa beyaz/hafif saydam kalır)")]
        [SerializeField] private Color charColor = new Color(0.85f, 0.80f, 0.72f, 0.85f);

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator anim;

        /// <summary>
        /// Engel yanmaya/erimeye başladığında tetiklenir.
        /// </summary>
        public event Action<BurnableObstacle> OnBurnStarted;

        /// <summary>
        /// Engel tamamen yok olduğunda tetiklenir.
        /// </summary>
        public event Action<BurnableObstacle> OnBurnCompleted;

        public bool IsBurned { get; private set; }
        public ObstacleType Type => obstacleType;

        private Collider2D obstacleCollider;
        private float currentDurability;
        private bool isBurning;

        private void Awake()
        {
            obstacleCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            currentDurability = durability;
        }

        /// <summary>
        /// Ejderhanın ateş topu temas ettiğinde çağrılır.
        /// </summary>
        /// <param name="fireDamage">Uygulanan ateş hasarı.</param>
        public void Burn(float fireDamage)
        {
            if (IsBurned || isBurning) return;

            currentDurability -= fireDamage;

            if (currentDurability <= 0f)
            {
                StartCoroutine(BurnRoutine());
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, igniteColor, 0.35f);
            }
        }

        private IEnumerator BurnRoutine()
        {
            isBurning = true;
            OnBurnStarted?.Invoke(this);

            if (anim != null)
            {
                anim.SetTrigger("Burn");
            }

            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = false;
            }

            // Yanma anında sarmaşık üzerinde alevler ve sıcak 2D ışık oluştur
            List<SpriteRenderer> activeFlames = new List<SpriteRenderer>();
            Light2D fireLight = null;

            if (obstacleType == ObstacleType.Vine)
            {
                fireLight = SpawnFireLight();
                activeFlames = SpawnFlames();
            }

            float elapsed = 0f;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;
            float halfHeight = spriteRenderer != null ? spriteRenderer.bounds.extents.y : 0.75f;
            float groundBottomY = startPos.y - halfHeight;
            bool swappedBurnedSprite = false;

            int flameCount = activeFlames.Count;
            float[] flameBaseY = new float[flameCount];
            for (int i = 0; i < flameCount; i++)
            {
                flameBaseY[i] = activeFlames[i].transform.localPosition.y;
            }

            List<AshParticle> ashes = SpawnParticles(groundBottomY, obstacleType == ObstacleType.Ice);
            Sprite ashSprite = null;
            if (ashes.Count > 0)
            {
                ashSprite = ashes[0].Renderer.sprite;
            }

            while (elapsed < burnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / burnDuration);

                // 1. Alev karelerini titreştir ve yukarı doğru yükselt
                if (flameSprites != null && flameSprites.Length > 0)
                {
                    int frameIdx = Mathf.FloorToInt(elapsed * 14f) % flameSprites.Length;
                    for (int i = 0; i < flameCount; i++)
                    {
                        SpriteRenderer fl = activeFlames[i];
                        if (fl == null) continue;
                        fl.sprite = flameSprites[(frameIdx + i) % flameSprites.Length];

                        float rise = t * 0.35f;
                        Vector3 pos = fl.transform.localPosition;
                        pos.y = flameBaseY[i] + rise;
                        fl.transform.localPosition = pos;

                        float flameScale = t < 0.3f ? Mathf.Lerp(0.6f, 1.35f, t / 0.3f) : Mathf.Lerp(1.35f, 0.2f, (t - 0.3f) / 0.7f);
                        fl.transform.localScale = new Vector3(flameScale, flameScale, 1f);

                        Color fc = Color.white;
                        fc.a = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
                        fl.color = fc;
                    }
                }

                // 2. 2D Ateş Işığı parlayıp söner
                if (fireLight != null)
                {
                    fireLight.intensity = t < 0.25f
                        ? Mathf.Lerp(0.5f, 2.2f, t / 0.25f)
                        : Mathf.Lerp(2.2f, 0f, (t - 0.25f) / 0.75f);
                }

                // 3. Sarmaşık önce alev turuncusuna parlar, ortasında dikenlerinden arınmış solmuş sprite'a geçer
                //    Buz bariyerinde meltFrames varsa kare kare erime görsellerini oynatır
                if (spriteRenderer != null)
                {
                    if (obstacleType == ObstacleType.Ice && meltFrames != null && meltFrames.Length > 0)
                    {
                        int meltIdx = Mathf.Clamp(Mathf.FloorToInt(t * meltFrames.Length), 0, meltFrames.Length - 1);
                        spriteRenderer.sprite = meltFrames[meltIdx];

                        Color iceCol = Color.white;
                        if (t > 0.85f && destroyAfterBurn)
                        {
                            iceCol.a = 1f - ((t - 0.85f) / 0.15f);
                        }
                        spriteRenderer.color = iceCol;
                    }
                    else
                    {
                        if (!swappedBurnedSprite && burnedSprite != null && t >= 0.40f)
                        {
                            spriteRenderer.sprite = burnedSprite;
                            swappedBurnedSprite = true;
                        }

                        Color flashCol = obstacleType == ObstacleType.Ice
                            ? new Color(0.85f, 0.98f, 1f, 1f)
                            : igniteColor;
                        Color endCol = obstacleType == ObstacleType.Ice
                            ? new Color(0.75f, 0.95f, 1f, 0f)
                            : (burnedSprite != null ? new Color(1f, 1f, 1f, 0.85f) : charColor);

                        Color targetCol = t < 0.35f
                            ? Color.Lerp(startColor, flashCol, t / 0.35f)
                            : Color.Lerp(flashCol, endCol, (t - 0.35f) / 0.65f);

                        if (destroyAfterBurn)
                        {
                            targetCol.a = t < 0.45f ? 1f : 1f - ((t - 0.45f) / 0.55f);
                        }
                        spriteRenderer.color = targetCol;
                    }
                }

                if (obstacleType == ObstacleType.Ice && (meltFrames == null || meltFrames.Length == 0))
                {
                    float scaleY = Mathf.Lerp(startScale.y, 0.05f, t);
                    transform.localScale = new Vector3(startScale.x, scaleY, startScale.z);
                    transform.position = new Vector3(startPos.x, groundBottomY + halfHeight * (scaleY / startScale.y), startPos.z);
                }
                else if (obstacleType == ObstacleType.Vine && t > 0.25f)
                {
                    // Alt tabanı zemine sabit tutarak doğal şekilde büzüşür
                    float rawSag = (t - 0.25f) / 0.75f;
                    float sag = 1f - Mathf.Pow(1f - rawSag, 2f);
                    float scaleX = Mathf.Lerp(startScale.x, startScale.x * 0.86f, sag);
                    float scaleY = Mathf.Lerp(startScale.y, startScale.y * 0.48f, sag);
                    transform.localScale = new Vector3(scaleX, scaleY, startScale.z);
                    transform.position = new Vector3(startPos.x, groundBottomY + halfHeight * (scaleY / startScale.y), startPos.z);
                }

                // Kül parçacıklarını süzülerek zemine dök ve zemin hizasında durdur
                for (int i = 0; i < ashes.Count; i++)
                {
                    AshParticle ash = ashes[i];
                    if (ash.Renderer == null) continue;

                    Vector3 ashPos = ash.Renderer.transform.position;
                    if (ashPos.y > ash.FloorY)
                    {
                        ash.Velocity.y = Mathf.Max(ash.Velocity.y - 1.2f * Time.deltaTime, -1.35f);
                        float flutterX = Mathf.Sin(elapsed * 6f + i) * 0.22f;
                        ashPos.x += (ash.Velocity.x + flutterX) * Time.deltaTime;
                        ashPos.y = Mathf.Max(ash.FloorY, ashPos.y + ash.Velocity.y * Time.deltaTime);
                        ash.Renderer.transform.position = ashPos;
                    }

                    Color ac = ash.BaseColor;
                    ac.a = t < 0.65f ? 1f : Mathf.Clamp01(1f - (t - 0.65f) / 0.35f);
                    ash.Renderer.color = ac;
                }

                yield return null;
            }

            // Geçici alev, kül ve ışık objelerini temizle
            for (int i = 0; i < flameCount; i++)
            {
                if (activeFlames[i] != null) Destroy(activeFlames[i].gameObject);
            }
            for (int i = 0; i < ashes.Count; i++)
            {
                if (ashes[i].Renderer != null) Destroy(ashes[i].Renderer.gameObject);
            }
            if (ashSprite != null && ashSprite.texture != null)
            {
                Destroy(ashSprite.texture);
                Destroy(ashSprite);
            }
            if (fireLight != null) Destroy(fireLight.gameObject);

            IsBurned = true;
            OnBurnCompleted?.Invoke(this);

            if (destroyAfterBurn)
            {
                Destroy(gameObject);
            }
            else if (spriteRenderer != null)
            {
                // Karakterler zararsız kalıntının (solmuş sarmaşık veya su birikintisi) önünden geçebilsin
                if (obstacleType == ObstacleType.Ice && meltFrames != null && meltFrames.Length > 0)
                {
                    spriteRenderer.sprite = meltFrames[meltFrames.Length - 1];
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.85f);
                }
                else
                {
                    spriteRenderer.color = burnedSprite != null ? new Color(1f, 1f, 1f, 0.85f) : charColor;
                }
                spriteRenderer.sortingOrder = Mathf.Min(spriteRenderer.sortingOrder, 1);
            }
        }

        private List<SpriteRenderer> SpawnFlames()
        {
            List<SpriteRenderer> list = new List<SpriteRenderer>();
            if (flameSprites == null || flameSprites.Length == 0) return list;

            Vector3[] offsets =
            {
                new Vector3(0f, -0.42f, 0f),
                new Vector3(-0.14f, 0.0f, 0f),
                new Vector3(0.14f, 0.12f, 0f),
                new Vector3(0f, 0.44f, 0f)
            };

            int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 6;
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject flameObj = new GameObject($"BurnFlame_{i}");
                flameObj.transform.SetParent(transform, false);
                flameObj.transform.localPosition = offsets[i];

                SpriteRenderer fl = flameObj.AddComponent<SpriteRenderer>();
                fl.sprite = flameSprites[i % flameSprites.Length];
                fl.sortingOrder = order;
                list.Add(fl);
            }

            return list;
        }

        private Light2D SpawnFireLight()
        {
            GameObject lightObj = new GameObject("BurnFireLight");
            lightObj.transform.SetParent(transform, false);
            Light2D l = lightObj.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.52f, 0.12f, 1f);
            l.intensity = 1.2f;
            l.pointLightInnerRadius = 0.4f;
            l.pointLightOuterRadius = 2.6f;
            return l;
        }

        private class AshParticle
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public Color BaseColor;
            public float FloorY;
        }

        private List<AshParticle> SpawnParticles(float groundBottomY, bool isIce)
        {
            List<AshParticle> list = new List<AshParticle>();

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);

            int order = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 5;
            for (int i = 0; i < 16; i++)
            {
                GameObject ashObj = new GameObject(isIce ? $"SteamDrop_{i}" : $"Ash_{i}");
                ashObj.transform.position = transform.position + new Vector3(
                    UnityEngine.Random.Range(-0.32f, 0.32f),
                    UnityEngine.Random.Range(-0.25f, 0.55f),
                    0f);
                float size = UnityEngine.Random.Range(0.6f, 1.15f);
                ashObj.transform.localScale = new Vector3(size, size, 1f);

                SpriteRenderer sr = ashObj.AddComponent<SpriteRenderer>();
                sr.sprite = pixelSprite;
                sr.sortingOrder = order;

                Color col;
                Vector2 vel;
                if (isIce)
                {
                    col = (i % 2 == 0)
                        ? new Color(0.85f, 0.98f, 1f, 0.85f)  // beyaz buhar
                        : new Color(0.35f, 0.85f, 0.95f, 1f); // buz damlası
                    vel = (i % 2 == 0)
                        ? new Vector2(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(0.55f, 1.05f))
                        : new Vector2(UnityEngine.Random.Range(-0.35f, 0.35f), UnityEngine.Random.Range(-0.1f, 0.25f));
                }
                else
                {
                    col = (i % 3 == 0)
                        ? new Color(1f, 0.45f, 0.14f, 1f)   // sıcak kor
                        : new Color(0.38f, 0.34f, 0.30f, 1f); // soluk kül
                    vel = new Vector2(UnityEngine.Random.Range(-0.28f, 0.28f), UnityEngine.Random.Range(-0.15f, 0.2f));
                }
                sr.color = col;

                list.Add(new AshParticle
                {
                    Renderer = sr,
                    Velocity = vel,
                    BaseColor = col,
                    FloorY = groundBottomY + UnityEngine.Random.Range(0.02f, 0.08f)
                });
            }

            return list;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePrincessContact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePrincessContact(other);
        }

        private void HandlePrincessContact(Collider2D other)
        {
            if (IsBurned || isBurning || other == null) return;

            PrincessController princess = other.GetComponent<PrincessController>()
                                        ?? other.GetComponentInParent<PrincessController>();
            if (princess == null) return;

            if (obstacleType == ObstacleType.Vine)
            {
                princess.Die();
            }
            else if (obstacleType == ObstacleType.Ice)
            {
                princess.Freeze(5f);
            }
        }
    }
}
