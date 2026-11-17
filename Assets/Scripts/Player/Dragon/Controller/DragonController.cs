using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using KindFall.Player.Princess;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderha karakterinin ana kontrolcüsü.
    /// Ejderha oyun boyunca yere temas etmeden havada süzülür/uçar;
    /// FSM durum makinesini, stamina tabanlı uçuş/süzülme fiziğini (Unity 6 linearVelocity),
    /// küçük ateş topu atışını ve Prenses'i havada taşıma mekaniğini yönetir.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class DragonController : MonoBehaviour
    {
        #region Components
        [Header("Bileşen Referansları")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D mainCollider;
        [SerializeField] private Animator anim;
        [SerializeField] private SpriteRenderer spriteRenderer;
        #endregion

        #region Flight & Glide Parameters
        [Header("Havada Süzülme & Uçuş Fiziği")]
        [Tooltip("Normal havada süzülme (Glide) hızı")]
        [SerializeField] private float glideSpeed = 6.2f;

        [Tooltip("Right Shift basılıyken hızlı uçuş (Fly Boost) hızı")]
        [SerializeField] private float flyBoostSpeed = 10.0f;

        [Tooltip("Stamina tükendiğinde (Exhausted) uygulanacak hız çarpanı")]
        [SerializeField] private float exhaustedSpeedMultiplier = 0.55f;

        [Tooltip("Yön tuşları bırakıldığında havada durma hassasiyeti")]
        [SerializeField] private float airDamping = 120f;
        #endregion

        #region Ground Clearance (Never Touch Ground)
        [Header("Zemin Mesafesi & Ekran Sınırı")]
        [Tooltip("Ejderhanın yerden koruyacağı minimum yükseklik boşluğu")]
        [SerializeField] private float minGroundClearance = 0.15f;

        [Tooltip("Zemin tarama kutusu boyutu")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);

        [SerializeField] private LayerMask groundLayer;

        [Tooltip("True ise Ejderha ana kamera çerçevesinin dışına çıkamaz")]
        [SerializeField] private bool clampToCameraBounds = true;

        [Tooltip("Ekran kenarlarından bırakılacak iç boşluk (birim)")]
        [SerializeField] private float screenBoundaryPadding = 0.55f;
        #endregion

        #region Stamina Parameters
        [Header("Dayanıklılık (Stamina) Sistemi")]
        [SerializeField] private float maxStamina = 100f;

        [Tooltip("Hızlı uçuşta (Fly) saniyede harcanan stamina")]
        [SerializeField] private float flyStaminaCost = 25f;

        [Tooltip("Prenses taşırken süzülmede (Glide) saniyede harcanan stamina")]
        [SerializeField] private float glideCarryStaminaCost = 10f;

        [Tooltip("Prenses taşırken hızlı uçuşa uygulanan ekstra stamina çarpanı")]
        [SerializeField] private float carryStaminaMultiplier = 1.5f;

        [Tooltip("Havada dinlenirken saniyede yenilenen stamina")]
        [SerializeField] private float staminaRegenRate = 28f;

        [Tooltip("Stamina harcandıktan sonra yenilenmenin başlaması için bekleme süresi (sn)")]
        [SerializeField] private float staminaRegenDelay = 0.45f;

        [Tooltip("Stamina tamamen tükendikten sonra tekrar hızlı uçuş/taşıma için gereken minimum stamina")]
        [SerializeField] private float exhaustedRecoveryThreshold = 20f;
        #endregion

        #region Fireball Parameters
        [Header("Ateş Topu (Fireball) Saldırısı")]
        [SerializeField] private DragonFireball fireballPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.35f;
        [SerializeField] private float attackDuration = 0.20f;
        [SerializeField] private float fireballStaminaCost = 8f;
        #endregion

        #region Princess Carry Parameters
        [Header("Prenses Taşıma Mekaniği")]
        [Tooltip("Prenses taşınırken tutulacağı alt nokta (boşsa ejderhanın altı kullanılır)")]
        [SerializeField] private Transform carryAnchor;

        [Tooltip("Prenses'i yakalayabilmek için gereken algılama yarıçapı")]
        [SerializeField] private float carryDetectRadius = 2.0f;

        [Tooltip("Prenses katmanı (boş bırakılırsa sahnedeki PrincessController üzerinden kontrol edilir)")]
        [SerializeField] private LayerMask princessLayer;

        [Tooltip("Prenses taşırken ateş topu atılabilsin mi?")]
        [SerializeField] private bool allowFireballWhileCarrying = true;
        #endregion

        #region Control Settings
        [Header("Kontrol Durumu")]
        [SerializeField] private bool isControlActive = true;
        #endregion

        #region Events (Observer Pattern)
        /// <summary>
        /// Stamina değiştiğinde tetiklenir (Mevcut Stamina, Maksimum Stamina).
        /// </summary>
        public event Action<float, float> OnStaminaChanged;

        /// <summary>
        /// Yorgunluk (Exhausted) durumu değiştiğinde tetiklenir.
        /// </summary>
        public event Action<bool> OnExhaustedChanged;

        /// <summary>
        /// Ateş topu fırlatıldığında tetiklenir.
        /// </summary>
        public event Action OnFireballFired;

        /// <summary>
        /// Prenses taşınmaya başlandığında veya bırakıldığında tetiklenir.
        /// </summary>
        public event Action<bool, PrincessController> OnPrincessCarryChanged;
        #endregion

        #region Properties & States
        public Rigidbody2D Rb => rb;
        public Animator Anim => anim;
        public Collider2D MainCollider => mainCollider;
        public DragonStateMachine StateMachine { get; private set; }

        public bool IsFacingRight { get; private set; } = true;
        public bool IsControlActive => isControlActive;
        public float CurrentStamina { get; private set; }
        public float MaxStamina => maxStamina;
        public bool IsExhausted { get; private set; }
        public bool IsCarryingPrincess => carriedPrincess != null;
        public PrincessController CarriedPrincess => carriedPrincess;

        public Vector2 MoveInput { get; private set; }
        public bool FlyInputHeld { get; private set; }
        public bool AttackInputDown { get; private set; }
        public bool CarryInputDown { get; private set; }

        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

        // Parametre Getter'ları
        public float FlyBoostSpeed => flyBoostSpeed;
        public float FlyStaminaCost => flyStaminaCost;
        public float GlideCarryStaminaCost => glideCarryStaminaCost;
        public float AttackDuration => attackDuration;

        // FSM Durumları
        public DragonIdleState IdleState { get; private set; }
        public DragonGlideState GlideState { get; private set; }
        public DragonFlyState FlyState { get; private set; }
        public DragonAttackState AttackState { get; private set; }
        #endregion

        private float lastStaminaConsumeTime = -10f;
        private float nextFireTime;
        private PrincessController carriedPrincess;

        #region Unity Lifecycle
        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (mainCollider == null) mainCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 0f;

            CurrentStamina = maxStamina;
            InitializeStateMachine();
        }

        private void Start()
        {
            IgnorePrincessCollisions();
            StateMachine.Initialize(IdleState);
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }

        private void Update()
        {
            UpdateInput();
            HandleCarryInput();
            RegenerateStaminaInAir();
            StateMachine.CurrentState?.LogicUpdate();
        }

        private void FixedUpdate()
        {
            StateMachine.CurrentState?.PhysicsUpdate();
            EnforceGroundClearance();
            EnforceCameraBounds();
            UpdateCarryPosition();
        }

        private void OnDisable()
        {
            if (IsCarryingPrincess)
            {
                StopCarry();
            }
        }
        #endregion

        #region State Machine Initialization
        private void InitializeStateMachine()
        {
            StateMachine = new DragonStateMachine();

            IdleState = new DragonIdleState(this, StateMachine, "Idle");
            GlideState = new DragonGlideState(this, StateMachine, "Glide");
            FlyState = new DragonFlyState(this, StateMachine, "Fly");
            AttackState = new DragonAttackState(this, StateMachine, "Attack");
        }
        #endregion

        #region Input Handling (New Input System)
        /// <summary>
        /// Ejderha kontrolünün aktif olup olmadığını ayarlar (karakter geçişleri için).
        /// </summary>
        public void SetControlActive(bool active)
        {
            isControlActive = active;
            if (!active)
            {
                MoveInput = Vector2.zero;
                FlyInputHeld = false;
                AttackInputDown = false;
                CarryInputDown = false;
            }
        }

        private void UpdateInput()
        {
            if (!isControlActive)
            {
                MoveInput = Vector2.zero;
                FlyInputHeld = false;
                AttackInputDown = false;
                CarryInputDown = false;
                return;
            }

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            float moveX = 0f;
            float moveY = 0f;

            FlyInputHeld = false;
            AttackInputDown = false;
            CarryInputDown = false;

            if (keyboard != null)
            {
                // 2. Oyuncu (Yavru Ejderha) - Klavye sağ tarafı (Prenses ile çakışmaz)
                if (keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.rightArrowKey.isPressed) moveX += 1f;
                if (keyboard.upArrowKey.isPressed) moveY += 1f;
                if (keyboard.downArrowKey.isPressed) moveY -= 1f;

                FlyInputHeld = keyboard.rightShiftKey.isPressed;
                AttackInputDown = keyboard.enterKey.wasPressedThisFrame;
                CarryInputDown = keyboard.lKey.wasPressedThisFrame;
            }

            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.1f) moveX = stick.x;
                if (Mathf.Abs(stick.y) > 0.1f) moveY = stick.y;

                if (gamepad.buttonSouth.isPressed) FlyInputHeld = true;
                if (gamepad.buttonWest.wasPressedThisFrame) AttackInputDown = true;
                if (gamepad.buttonNorth.wasPressedThisFrame) CarryInputDown = true;
            }

            MoveInput = new Vector2(moveX, moveY);
#else
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            FlyInputHeld = Input.GetKey(KeyCode.RightShift);
            AttackInputDown = Input.GetKeyDown(KeyCode.Return);
            CarryInputDown = Input.GetKeyDown(KeyCode.L);
#endif
        }
        #endregion

        #region Stamina Management
        /// <summary>
        /// Ejderhanın hızlı uçuş (Fly Boost) yapabilecek staminaya sahip olup olmadığını döner.
        /// </summary>
        public bool CanFlyBoost()
        {
            return !IsExhausted && CurrentStamina > 0.1f;
        }

        /// <summary>
        /// Mevcut yorgunluk durumuna göre süzülme hızını döner.
        /// </summary>
        public float GetCurrentGlideSpeed()
        {
            return IsExhausted ? glideSpeed * exhaustedSpeedMultiplier : glideSpeed;
        }

        /// <summary>
        /// Belirtilen miktarda stamina tüketir (Prenses taşınıyorsa çarpan uygular).
        /// </summary>
        public void ConsumeStamina(float amount)
        {
            if (amount <= 0f) return;

            float multiplier = IsCarryingPrincess ? carryStaminaMultiplier : 1f;
            float previousStamina = CurrentStamina;
            CurrentStamina = Mathf.Max(0f, CurrentStamina - (amount * multiplier));
            lastStaminaConsumeTime = Time.time;

            if (!Mathf.Approximately(previousStamina, CurrentStamina))
            {
                OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            }

            if (CurrentStamina <= 0f && !IsExhausted)
            {
                SetExhausted(true);
            }
        }

        private void RegenerateStaminaInAir()
        {
            if (CurrentStamina >= maxStamina) return;
            if (IsCarryingPrincess) return;
            if (Time.time - lastStaminaConsumeTime < staminaRegenDelay) return;

            float previousStamina = CurrentStamina;
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + (staminaRegenRate * Time.deltaTime));

            if (!Mathf.Approximately(previousStamina, CurrentStamina))
            {
                OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            }

            if (IsExhausted && CurrentStamina >= exhaustedRecoveryThreshold)
            {
                SetExhausted(false);
            }
        }

        private void SetExhausted(bool exhausted)
        {
            if (IsExhausted == exhausted) return;
            IsExhausted = exhausted;
            OnExhaustedChanged?.Invoke(exhausted);

            // Stamina bittiyse taşınan Prenses'i güvenli şekilde bırak
            if (exhausted && IsCarryingPrincess)
            {
                StopCarry();
            }
        }
        #endregion

        #region Unity 6 LinearVelocity Aerial Physics
        public void SetZeroGravity()
        {
            rb.gravityScale = 0f;
        }

        public void SetVelocity(Vector2 velocity)
        {
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Tuş bırakıldığında kaymayı önlemek için hızı anında sıfırlar.
        /// </summary>
        public void StopVelocity()
        {
            rb.linearVelocity = Vector2.zero;
        }

        public void ApplyAirDamping()
        {
            if (rb.linearVelocity.sqrMagnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, airDamping * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Ejderhanın oyun boyunca yere temas etmemesi için zeminle minimum boşluğu korur.
        /// </summary>
        public bool IsNearGround()
        {
            if (groundLayer.value == 0) return false;
            Vector2 origin = mainCollider != null
                ? new Vector2(mainCollider.bounds.center.x, mainCollider.bounds.min.y)
                : (Vector2)transform.position;

            RaycastHit2D hit = Physics2D.BoxCast(origin, groundCheckSize, 0f, Vector2.down, minGroundClearance, groundLayer);
            return hit.collider != null;
        }

        private void EnforceGroundClearance()
        {
            if (rb.linearVelocityY < 0f && IsNearGround())
            {
                rb.linearVelocityY = 0f;
            }
        }

        private void EnforceCameraBounds()
        {
            if (!clampToCameraBounds) return;
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float halfH = Mathf.Max(0.5f, cam.orthographicSize - screenBoundaryPadding);
            float halfW = Mathf.Max(0.5f, (cam.orthographicSize * cam.aspect) - screenBoundaryPadding);

            Vector3 camPos = cam.transform.position;
            float minX = camPos.x - halfW;
            float maxX = camPos.x + halfW;
            float minY = camPos.y - halfH;
            float maxY = camPos.y + halfH;

            Vector2 pos = rb.position;
            Vector2 vel = rb.linearVelocity;
            bool clamped = false;

            if (pos.x < minX) { pos.x = minX; if (vel.x < 0f) vel.x = 0f; clamped = true; }
            else if (pos.x > maxX) { pos.x = maxX; if (vel.x > 0f) vel.x = 0f; clamped = true; }

            if (pos.y < minY) { pos.y = minY; if (vel.y < 0f) vel.y = 0f; clamped = true; }
            else if (pos.y > maxY) { pos.y = maxY; if (vel.y > 0f) vel.y = 0f; clamped = true; }

            if (clamped)
            {
                rb.position = pos;
                rb.linearVelocity = vel;
            }
        }
        #endregion

        #region Fireball Combat
        /// <summary>
        /// Ateş topu atışının bekleme süresi, stamina ve taşıma kurallarına göre mümkün olup olmadığını döner.
        /// </summary>
        public bool CanFireball()
        {
            if (Time.time < nextFireTime) return false;
            if (!allowFireballWhileCarrying && IsCarryingPrincess) return false;
            if (fireballStaminaCost > 0f && CurrentStamina < fireballStaminaCost) return false;
            return true;
        }

        /// <summary>
        /// Ateş topu prefab'ını fırlatır ve Observer olayını tetikler.
        /// </summary>
        public void FireFireball()
        {
            nextFireTime = Time.time + fireCooldown;

            if (fireballStaminaCost > 0f)
            {
                ConsumeStamina(fireballStaminaCost);
            }

            if (fireballPrefab != null)
            {
                Vector3 spawnPos = firePoint != null
                    ? firePoint.position
                    : transform.position + new Vector3(IsFacingRight ? 0.8f : -0.8f, 0.05f, 0f);

                DragonFireball fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
                Vector2 dir = IsFacingRight ? Vector2.right : Vector2.left;
                fireball.Launch(dir, gameObject);
            }
            else
            {
                Debug.LogWarning("[DragonController] fireballPrefab atanmadığı için ateş topu oluşturulamadı.");
            }

            OnFireballFired?.Invoke();
        }
        #endregion

        #region Princess Carry Mechanics
        private void HandleCarryInput()
        {
            if (!CarryInputDown) return;

            if (IsCarryingPrincess)
            {
                StopCarry();
            }
            else
            {
                TryStartCarry();
            }
        }

        /// <summary>
        /// Yakındaki Prenses karakterini algılayıp havada taşımaya çalışır.
        /// </summary>
        /// <returns>Taşıma başlatıldıysa true döner.</returns>
        public bool TryStartCarry()
        {
            if (IsCarryingPrincess || IsExhausted) return false;

            PrincessController targetPrincess = FindNearbyPrincess();
            if (targetPrincess == null) return false;

            carriedPrincess = targetPrincess;

            if (mainCollider != null && carriedPrincess.MainCollider != null)
            {
                Physics2D.IgnoreCollision(mainCollider, carriedPrincess.MainCollider, true);
            }

            carriedPrincess.enabled = false;

            if (carriedPrincess.Rb != null)
            {
                carriedPrincess.Rb.linearVelocity = Vector2.zero;
                carriedPrincess.Rb.gravityScale = 0f;
                carriedPrincess.Rb.bodyType = RigidbodyType2D.Kinematic;
            }

            carriedPrincess.transform.position = GetCarryAnchorPosition();
            if (anim != null) anim.SetBool("IsCarrying", true);
            OnPrincessCarryChanged?.Invoke(true, carriedPrincess);
            return true;
        }

        /// <summary>
        /// Taşınan Prenses karakterini güvenli şekilde serbest bırakır.
        /// </summary>
        public void StopCarry()
        {
            if (carriedPrincess == null) return;

            PrincessController released = carriedPrincess;
            carriedPrincess = null;

            if (released.Rb != null)
            {
                released.Rb.bodyType = RigidbodyType2D.Dynamic;
                released.ResetGravityScale();
                released.Rb.linearVelocity = new Vector2(rb.linearVelocityX * 0.5f, Mathf.Max(rb.linearVelocityY, 1.5f));
            }

            released.enabled = true;

            if (released.StateMachine != null && released.FallState != null)
            {
                released.StateMachine.ChangeState(released.FallState);
            }

            if (anim != null) anim.SetBool("IsCarrying", false);
            OnPrincessCarryChanged?.Invoke(false, released);
        }

        private void UpdateCarryPosition()
        {
            if (carriedPrincess == null) return;

            Vector3 targetPos = GetCarryAnchorPosition();
            if (carriedPrincess.Rb != null)
            {
                carriedPrincess.Rb.MovePosition(targetPos);
            }
            else
            {
                carriedPrincess.transform.position = targetPos;
            }
        }

        private Vector3 GetCarryAnchorPosition()
        {
            return carryAnchor != null
                ? carryAnchor.position
                : transform.position + new Vector3(0f, -0.95f, 0f);
        }

        private PrincessController FindNearbyPrincess()
        {
            if (princessLayer.value != 0)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, carryDetectRadius, princessLayer);
                foreach (Collider2D hit in hits)
                {
                    if (hit.TryGetComponent(out PrincessController princess))
                    {
                        return princess;
                    }
                }
            }

            PrincessController[] allPrincesses = FindObjectsByType<PrincessController>(FindObjectsSortMode.None);
            float bestDist = carryDetectRadius;
            PrincessController closest = null;

            foreach (PrincessController p in allPrincesses)
            {
                float dist = Vector2.Distance(transform.position, p.transform.position);
                if (dist <= bestDist)
                {
                    bestDist = dist;
                    closest = p;
                }
            }

            return closest;
        }

        private void IgnorePrincessCollisions()
        {
            if (mainCollider == null) return;

            PrincessController[] allPrincesses = FindObjectsByType<PrincessController>(FindObjectsSortMode.None);
            foreach (PrincessController p in allPrincesses)
            {
                if (p.MainCollider != null)
                {
                    Physics2D.IgnoreCollision(mainCollider, p.MainCollider, true);
                }
            }
        }
        #endregion

        #region Flip & Gizmos
        public void CheckFlip(float moveX)
        {
            if (moveX > 0.05f && !IsFacingRight)
            {
                Flip();
            }
            else if (moveX < -0.05f && IsFacingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            IsFacingRight = !IsFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            // Zemin mesafe koruma taraması
            Gizmos.color = IsNearGround() ? Color.yellow : Color.green;
            Vector2 origin = mainCollider != null
                ? new Vector2(mainCollider.bounds.center.x, mainCollider.bounds.min.y)
                : (Vector2)transform.position;
            Gizmos.DrawWireCube(origin + (Vector2.down * minGroundClearance), groundCheckSize);

            // Prenses yakalama yarıçapı
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, carryDetectRadius);

            // Taşıma noktası
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetCarryAnchorPosition(), 0.1f);

            // Ateş topu çıkış noktası
            if (firePoint != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawSphere(firePoint.position, 0.1f);
            }
        }
        #endregion
    }
}
