using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin ana kontrolcüsü.
    /// FSM durum makinesini, girdi dinlemeyi, Unity 6 fizik operasyonlarını (linearVelocity),
    /// BoxCast zemin ve Raycast duvar kontrollerini yönetir.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PrincessController : MonoBehaviour
    {
        #region Components
        [Header("Bileşen Referansları")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D mainCollider;
        [SerializeField] private Animator anim;
        [SerializeField] private SpriteRenderer spriteRenderer;
        #endregion

        #region Movement Parameters
        [Header("Yatay Hareket")]
        [Tooltip("Zeminde koşma hızı")]
        [SerializeField] private float moveSpeed = 8f;

        [Tooltip("Havadayken yatay hareket hızı")]
        [SerializeField] private float airMoveSpeed = 7f;

        [Tooltip("Tuş bırakıldığında kaymayı engellemek için zemin sürtünmesi")]
        [SerializeField] private float groundDeceleration = 50f;
        #endregion

        #region Jump & Fall Parameters
        [Header("Dikey Hareket & Yerçekimi")]
        [Tooltip("İlk zıplama dikey fırlatma hızı")]
        [SerializeField] private float jumpForce = 9.5f;

        [Tooltip("Düşerken uygulanan yerçekimi ölçeği")]
        [SerializeField] private float fallGravityScale = 3.5f;

        [Tooltip("Maksimum serbest düşüş hızı (Terminal Velocity)")]
        [SerializeField] private float maxFallSpeed = -18f;

        [Tooltip("Zıplama tuşu erken bırakıldığında uygulanan yerçekimi ölçeği (Variable Jump)")]
        [SerializeField] private float lowJumpGravityScale = 4.5f;

        [Tooltip("Normal yerçekimi ölçeği")]
        [SerializeField] private float defaultGravityScale = 2.5f;

        [Tooltip("Platformdan boşluğa adım atıldığında zıplamaya izin verilen süre (~0.12 sn)")]
        [SerializeField] private float coyoteTime = 0.12f;

        [Tooltip("Yere inmeden önce basılan zıplama komutunu hafızada tutma süresi (~0.10 sn)")]
        [SerializeField] private float jumpBufferTime = 0.10f;
        #endregion

        #region Wall Mechanics Parameters
        [Header("Duvar Mekanik")]
        [Tooltip("Duvardan kayarken izin verilen maksimum düşüş hızı (linearVelocityY)")]
        [SerializeField] private float wallSlideMaxFallSpeed = -2.5f;

        [Tooltip("Duvardan ters yöne zıplama kuvveti (X: Yatay itiş, Y: Dikey fırlama)")]
        [SerializeField] private Vector2 wallJumpDirection = new Vector2(9f, 13f);

        [Tooltip("Duvar zıplamasından sonra oyuncunun yön girdisinin kilitlendiği süre")]
        [SerializeField] private float wallJumpInputLockTime = 0.15f;
        #endregion

        #region Combat & Defense Parameters
        [Header("Savaş ve Savunma")]
        [Tooltip("Kılıç savrulduğunda öne atılma kuvveti")]
        [SerializeField] private float attackDashForce = 3f;

        [Tooltip("Hitbox'ın açık kalacağı süre (sn)")]
        [SerializeField] private float attackHitboxDuration = 0.10f;

        [Tooltip("Toplam saldırı animasyon/durum süresi")]
        [SerializeField] private float attackDuration = 0.35f;

        [Tooltip("Mükemmel savuşturma (Parry) penceresi süresi (0.18 sn)")]
        [SerializeField] private float parryWindowDuration = 0.18f;

        [Tooltip("Kalkan açıkken hareket hızı çarpanı (%50)")]
        [SerializeField] private float shieldSpeedMultiplier = 0.50f;

        [Tooltip("Kalkan açıkken hasar azaltma oranı (%70)")]
        [SerializeField] private float shieldDamageReduction = 0.70f;

        [Tooltip("Saldırı temas alanı (Hitbox)")]
        [SerializeField] private Collider2D attackHitbox;
        #endregion

        #region Detection Parameters
        [Header("Zemin & Duvar Algılama")]
        [Tooltip("Zemin algılama referans noktası (boşsa collider alt noktası otomatik kullanılır)")]
        [SerializeField] private Transform groundCheckPoint;
        [Tooltip("Zemin BoxCast tarama kutusu boyutu (Genişlik, Yükseklik)")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
        [Tooltip("Zemin tarama mesafesi")]
        [SerializeField] private float groundCheckDistance = 0.05f;
        [Tooltip("Zemin katmanı")]
        [SerializeField] private LayerMask groundLayer;

        [SerializeField] private Transform chestCheckPoint;
        [SerializeField] private Transform kneeCheckPoint;
        [SerializeField] private float wallCheckDistance = 0.45f;
        [SerializeField] private LayerMask wallLayer;
        #endregion

        #region Events (Observer Pattern)
        /// <summary>
        /// Kalkan açıldığında veya kapandığında tetiklenir (Aktif/Pasif).
        /// </summary>
        public event Action<bool> OnShieldToggled;

        /// <summary>
        /// Başarılı bir mükemmel savuşturma (Parry) yapıldığında tetiklenir.
        /// </summary>
        public event Action OnParrySuccessful;

        /// <summary>
        /// Karakter hasar aldığında veya savuşturduğunda tetiklenir.
        /// </summary>
        public event Action<float, bool> OnDamageTaken;
        #endregion

        #region Properties & State Access
        public Rigidbody2D Rb => rb;
        public Animator Anim => anim;
        public Collider2D MainCollider => mainCollider;
        public PrincessStateMachine StateMachine { get; private set; }

        public bool IsFacingRight { get; private set; } = true;
        public Vector2 MoveInput { get; private set; }
        public bool JumpInputDown { get; private set; }
        public bool JumpInputHeld { get; private set; }
        public bool AttackInputDown { get; private set; }
        public bool ShieldInputHeld { get; private set; }

        public bool IsInputLocked => Time.time < inputLockEndTime;
        public bool IsShieldActive { get; private set; }
        public bool IsParrying { get; private set; }

        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

        // Parametre Getter'ları
        public float MoveSpeed => moveSpeed;
        public float AirMoveSpeed => airMoveSpeed;
        public float JumpForce => jumpForce;
        public float MaxFallSpeed => maxFallSpeed;
        public float WallSlideMaxFallSpeed => wallSlideMaxFallSpeed;
        public Vector2 WallJumpDirection => wallJumpDirection;
        public float WallJumpInputLockTime => wallJumpInputLockTime;
        public float AttackDashForce => attackDashForce;
        public float AttackHitboxDuration => attackHitboxDuration;
        public float AttackDuration => attackDuration;
        public float ParryWindowDuration => parryWindowDuration;
        public float ShieldSpeedMultiplier => shieldSpeedMultiplier;
        public float ShieldDamageReduction => shieldDamageReduction;

        // FSM Durum Örnekleri
        public PrincessIdleState IdleState { get; private set; }
        public PrincessRunState RunState { get; private set; }
        public PrincessJumpState JumpState { get; private set; }
        public PrincessFallState FallState { get; private set; }
        public PrincessWallSlideState WallSlideState { get; private set; }
        public PrincessWallJumpState WallJumpState { get; private set; }
        public PrincessAttackState AttackState { get; private set; }
        public PrincessShieldState ShieldState { get; private set; }
        public PrincessParryState ParryState { get; private set; }
        #endregion

        #region Private Fields
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float inputLockEndTime;
        private Coroutine freezeCoroutine;

        public bool IsFrozen { get; private set; }
        public bool IsDead { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (mainCollider == null) mainCollider = GetComponent<Collider2D>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.gravityScale = defaultGravityScale;
            DisableAttackHitbox();

            InitializeStateMachine();
        }

        private void Start()
        {
            StateMachine.Initialize(IdleState);
        }

        private void Update()
        {
            if (IsDead || IsFrozen)
            {
                MoveInput = Vector2.zero;
                JumpInputDown = false;
                JumpInputHeld = false;
                AttackInputDown = false;
                ShieldInputHeld = false;
                return;
            }

            UpdateInput();
            UpdateTimers();

            StateMachine.CurrentState?.LogicUpdate();
        }

        private void FixedUpdate()
        {
            if (IsDead)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            if (IsFrozen)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocityY));
                return;
            }

            StateMachine.CurrentState?.PhysicsUpdate();
        }
        #endregion

        #region State Machine Initialization
        private void InitializeStateMachine()
        {
            StateMachine = new PrincessStateMachine();

            IdleState = new PrincessIdleState(this, StateMachine, "Idle");
            RunState = new PrincessRunState(this, StateMachine, "Run");
            JumpState = new PrincessJumpState(this, StateMachine, "Jump");
            FallState = new PrincessFallState(this, StateMachine, "Fall");
            WallSlideState = new PrincessWallSlideState(this, StateMachine, "WallSlide");
            WallJumpState = new PrincessWallJumpState(this, StateMachine, "Jump");
            AttackState = new PrincessAttackState(this, StateMachine, "Attack");
            ShieldState = new PrincessShieldState(this, StateMachine, "Shield");
            ParryState = new PrincessParryState(this, StateMachine, "Parry");
        }
        #endregion

        #region Input Handling (New Input System)
        private void UpdateInput()
        {
            if (IsInputLocked)
            {
                MoveInput = Vector2.zero;
                return;
            }

#if ENABLE_INPUT_SYSTEM
            // New Input System ile klavye ve oyun kolu okuma
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            float moveX = 0f;
            float moveY = 0f;

            if (keyboard != null)
            {
                // 1. Oyuncu (Prenses) - Klavye sol tarafı (Ok tuşları 2. Oyuncu / Ejderha içindir)
                if (keyboard.aKey.isPressed) moveX -= 1f;
                if (keyboard.dKey.isPressed) moveX += 1f;
                if (keyboard.wKey.isPressed) moveY += 1f;
                if (keyboard.sKey.isPressed) moveY -= 1f;

                JumpInputDown = keyboard.spaceKey.wasPressedThisFrame;
                JumpInputHeld = keyboard.spaceKey.isPressed;

                AttackInputDown = keyboard.fKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame;
                ShieldInputHeld = keyboard.gKey.isPressed || keyboard.kKey.isPressed;
            }

            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.1f) moveX = stick.x;
                if (Mathf.Abs(stick.y) > 0.1f) moveY = stick.y;

                if (gamepad.buttonSouth.wasPressedThisFrame) JumpInputDown = true;
                if (gamepad.buttonSouth.isPressed) JumpInputHeld = true;

                if (gamepad.buttonWest.wasPressedThisFrame) AttackInputDown = true;
                if (gamepad.leftShoulder.isPressed || gamepad.rightTrigger.isPressed) ShieldInputHeld = true;
            }

            MoveInput = new Vector2(moveX, moveY);
#else
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            JumpInputDown = Input.GetButtonDown("Jump");
            JumpInputHeld = Input.GetButton("Jump");
            AttackInputDown = Input.GetButtonDown("Fire1");
            ShieldInputHeld = Input.GetButton("Fire2");
#endif

            // Jump Buffer kaydı
            if (JumpInputDown)
            {
                jumpBufferTimer = jumpBufferTime;
            }
        }

        private void UpdateTimers()
        {
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            if (coyoteTimer > 0f)
            {
                coyoteTimer -= Time.deltaTime;
            }
        }
        #endregion

        #region Unity 6 LinearVelocity Physics Helpers
        /// <summary>
        /// Yatay hızı Unity 6 standardında ayarlar.
        /// </summary>
        public void SetVelocityX(float velocityX)
        {
            rb.linearVelocityX = velocityX;
        }

        /// <summary>
        /// Dikey hızı Unity 6 standardında ayarlar.
        /// </summary>
        public void SetVelocityY(float velocityY)
        {
            rb.linearVelocityY = velocityY;
        }

        /// <summary>
        /// Yatay ve dikey hızı aynı anda ayarlar.
        /// </summary>
        public void SetVelocity(Vector2 velocity)
        {
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Yatay hızı sıfırlar.
        /// </summary>
        public void StopX()
        {
            rb.linearVelocityX = 0f;
        }

        /// <summary>
        /// Dikey hızı sıfırlar.
        /// </summary>
        public void StopY()
        {
            rb.linearVelocityY = 0f;
        }

        /// <summary>
        /// Tuş bırakıldığında yatay kaymayı önlemek için sönümleme uygular.
        /// </summary>
        public void ApplyGroundFriction()
        {
            if (Mathf.Abs(rb.linearVelocityX) < 0.05f)
            {
                rb.linearVelocityX = 0f;
                return;
            }

            rb.linearVelocityX = Mathf.MoveTowards(rb.linearVelocityX, 0f, groundDeceleration * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Düşüş anında daha tok bir iniş hissi için artırılmış yerçekimi ve terminal hız limiti uygular.
        /// </summary>
        public void ApplyFallGravity()
        {
            rb.gravityScale = fallGravityScale;

            if (rb.linearVelocityY < maxFallSpeed)
            {
                rb.linearVelocityY = maxFallSpeed;
            }
        }

        /// <summary>
        /// Zıplama tuşu erken bırakıldığında yerçekimini artırarak kısa zıplama sağlar (Variable Jump).
        /// </summary>
        public void ApplyVariableJumpGravity()
        {
            rb.gravityScale = lowJumpGravityScale;
        }

        /// <summary>
        /// Yerçekimi ölçeğini varsayılan değere döndürür.
        /// </summary>
        public void ResetGravityScale()
        {
            rb.gravityScale = defaultGravityScale;
        }
        #endregion

        #region Buffer & Coyote Time Helpers
        /// <summary>
        /// Yere basmadan önce basılan zıplama komutu hafızada varsa tüketir ve true döner.
        /// </summary>
        public bool ConsumeJumpBuffer()
        {
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Platformdan boşluğa düşüldüğünde Coyote timer'ı başlatır.
        /// </summary>
        public void StartCoyoteTimer()
        {
            coyoteTimer = coyoteTime;
        }

        /// <summary>
        /// Coyote süresi hala geçerli mi kontrol eder.
        /// </summary>
        public bool CanUseCoyoteJump()
        {
            return coyoteTimer > 0f;
        }

        /// <summary>
        /// Coyote hakkını sıfırlar.
        /// </summary>
        public void ClearCoyoteTimer()
        {
            coyoteTimer = 0f;
        }

        /// <summary>
        /// Belirli bir süre boyunca yön girdilerini kilitler (Örn: WallJump sonrası).
        /// </summary>
        public void SetInputLock(float duration)
        {
            inputLockEndTime = Time.time + duration;
        }
        #endregion

        #region Ground & Wall Checks
        /// <summary>
        /// Zemin taramasının başlatılacağı dünya pozisyonunu döner.
        /// groundCheckPoint atanmışsa onu, atanmamışsa ana collider'ın tabanını kullanır.
        /// </summary>
        public Vector2 GetGroundCheckOrigin()
        {
            if (groundCheckPoint != null)
            {
                return (Vector2)groundCheckPoint.position;
            }

            if (mainCollider != null)
            {
                Bounds bounds = mainCollider.bounds;
                return new Vector2(bounds.center.x, bounds.min.y);
            }

            return (Vector2)transform.position;
        }

        /// <summary>
        /// BoxCast kullanarak zemin temasını kontrol eder.
        /// </summary>
        public bool CheckIsGrounded()
        {
            Vector2 origin = GetGroundCheckOrigin();
            RaycastHit2D hit = Physics2D.BoxCast(origin, groundCheckSize, 0f, Vector2.down, groundCheckDistance, groundLayer);
            return hit.collider != null;
        }

        /// <summary>
        /// Göğüs hizası duvar algılama noktasını döner.
        /// </summary>
        public Vector2 GetChestCheckOrigin()
        {
            if (chestCheckPoint != null)
            {
                return (Vector2)chestCheckPoint.position;
            }

            if (mainCollider != null)
            {
                Bounds bounds = mainCollider.bounds;
                return new Vector2(bounds.center.x, bounds.center.y + (bounds.extents.y * 0.35f));
            }

            return (Vector2)transform.position + new Vector2(0f, 0.5f);
        }

        /// <summary>
        /// Diz hizası duvar algılama noktasını döner.
        /// </summary>
        public Vector2 GetKneeCheckOrigin()
        {
            if (kneeCheckPoint != null)
            {
                return (Vector2)kneeCheckPoint.position;
            }

            if (mainCollider != null)
            {
                Bounds bounds = mainCollider.bounds;
                return new Vector2(bounds.center.x, bounds.center.y - (bounds.extents.y * 0.35f));
            }

            return (Vector2)transform.position + new Vector2(0f, -0.2f);
        }

        /// <summary>
        /// Göğüs ve diz hizasından atılan 2 adet Raycast ile duvar temasını denetler.
        /// </summary>
        public bool CheckIsTouchingWall()
        {
            Vector2 facingDir = IsFacingRight ? Vector2.right : Vector2.left;

            Vector2 chestOrigin = GetChestCheckOrigin();
            Vector2 kneeOrigin = GetKneeCheckOrigin();

            RaycastHit2D chestHit = Physics2D.Raycast(chestOrigin, facingDir, wallCheckDistance, wallLayer);
            RaycastHit2D kneeHit = Physics2D.Raycast(kneeOrigin, facingDir, wallCheckDistance, wallLayer);

            return chestHit.collider != null || kneeHit.collider != null;
        }

        /// <summary>
        /// Karakterin duvara doğru yön tuşuna basıp basmadığını kontrol eder.
        /// </summary>
        public bool IsPushingAgainstWall()
        {
            if (IsFacingRight && MoveInput.x > 0.05f) return true;
            if (!IsFacingRight && MoveInput.x < -0.05f) return true;
            return false;
        }
        #endregion

        #region Orientation / Facing
        /// <summary>
        /// Girdi yönüne göre karakterin bakış açısını günceller.
        /// </summary>
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

        /// <summary>
        /// Karakter yönünü zorla belirli bir tarafa çevirir.
        /// </summary>
        public void ForceFlip(bool faceRight)
        {
            if (IsFacingRight != faceRight)
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
        #endregion

        #region Combat & Defense Control
        /// <summary>
        /// Kılıç hitbox alanını aktif eder.
        /// </summary>
        public void EnableAttackHitbox()
        {
            if (attackHitbox != null)
            {
                attackHitbox.enabled = true;
            }
        }

        /// <summary>
        /// Kılıç hitbox alanını kapatır.
        /// </summary>
        public void DisableAttackHitbox()
        {
            if (attackHitbox != null)
            {
                attackHitbox.enabled = false;
            }
        }

        /// <summary>
        /// Savuşturma penceresi durumunu ayarlar.
        /// </summary>
        public void SetParrying(bool active)
        {
            IsParrying = active;
        }

        /// <summary>
        /// Kalkan siper durumunu ayarlar ve Observer event'ini tetikler.
        /// </summary>
        public void SetShieldActive(bool active)
        {
            IsShieldActive = active;
            OnShieldToggled?.Invoke(active);
        }

        /// <summary>
        /// Gelen saldırıyı işler (Parry penceresi, Shield hasar azaltımı veya doğrudan hasar).
        /// </summary>
        /// <param name="rawDamage">Ham gelen hasar miktarı.</param>
        public void TakeDamage(float rawDamage)
        {
            if (IsParrying)
            {
                // Mükemmel savuşturma: Hasar tamamen engellenir
                OnParrySuccessful?.Invoke();
                StartCoroutine(HitStopRoutine(0.08f));
                return;
            }

            float finalDamage = rawDamage;

            if (IsShieldActive)
            {
                // Kalkan açık: Hasar %70 azaltılır
                finalDamage *= (1f - shieldDamageReduction);
            }

            OnDamageTaken?.Invoke(finalDamage, IsShieldActive);
        }

        /// <summary>
        /// Prensesi belirtilen süre boyunca dondurur (hareketi keser ve buz mavisi renk uygular).
        /// </summary>
        /// <param name="duration">Donma süresi (sn).</param>
        public void Freeze(float duration = 5f)
        {
            if (IsDead) return;

            if (freezeCoroutine != null)
            {
                StopCoroutine(freezeCoroutine);
            }

            freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
        }

        private IEnumerator FreezeRoutine(float duration)
        {
            IsFrozen = true;
            if (rb != null) rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocityY));
            if (anim != null) anim.speed = 0f;

            Color iceColor = new Color(0.15f, 0.50f, 0.95f, 1f);
            if (spriteRenderer != null) spriteRenderer.color = iceColor;

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(iceColor, Color.white, Mathf.Clamp01(elapsed / safeDuration));
                }

                yield return null;
            }

            IsFrozen = false;
            if (anim != null) anim.speed = 1f;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            freezeCoroutine = null;
        }

        /// <summary>
        /// Prenses öldüğünde hareketi durdurur ve aktif sahneyi başa sarar.
        /// </summary>
        public void Die()
        {
            if (IsDead) return;

            IsDead = true;
            IsFrozen = false;

            if (freezeCoroutine != null)
            {
                StopCoroutine(freezeCoroutine);
                freezeCoroutine = null;
            }

            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (anim != null) anim.speed = 0f;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);

            StartCoroutine(RestartSceneRoutine());
        }

        private IEnumerator RestartSceneRoutine()
        {
            yield return new WaitForSeconds(0.25f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float originalScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalScale;
        }

        #region Animation Event Callbacks
        /// <summary>
        /// Animator üzerinden tetiklenebilen hitbox açma olayı (Animation Event).
        /// </summary>
        public void AnimEvent_OpenHitbox()
        {
            EnableAttackHitbox();
        }

        /// <summary>
        /// Animator üzerinden tetiklenebilen hitbox kapatma olayı (Animation Event).
        /// </summary>
        public void AnimEvent_CloseHitbox()
        {
            DisableAttackHitbox();
        }

        /// <summary>
        /// Animator üzerinden saldırı animasyonunun bittiğini bildiren olay (Animation Event).
        /// </summary>
        public void AnimEvent_FinishAttack()
        {
            if (StateMachine.CurrentState == AttackState)
            {
                if (CheckIsGrounded())
                {
                    StateMachine.ChangeState(Mathf.Abs(MoveInput.x) > 0.05f ? RunState : IdleState);
                }
                else
                {
                    StateMachine.ChangeState(FallState);
                }
            }
        }
        #endregion
        #endregion

        #region Gizmos & Debugging
        private void OnDrawGizmosSelected()
        {
            // Zemin BoxCast görselleştirmesi (Yere basıyorsa Yeşil, havadaysa Kırmızı)
            bool isGrounded = CheckIsGrounded();
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector2 groundOrigin = GetGroundCheckOrigin();
            Gizmos.DrawWireCube(groundOrigin + (Vector2.down * groundCheckDistance), groundCheckSize);

            // Duvar Raycast görselleştirmesi (Duvara değiyorsa Sarı, değmiyorsa Mavi)
            bool isTouchingWall = CheckIsTouchingWall();
            Gizmos.color = isTouchingWall ? Color.yellow : Color.blue;
            Vector2 facingDir = IsFacingRight ? Vector2.right : Vector2.left;
            Vector2 chestOrigin = GetChestCheckOrigin();
            Vector2 kneeOrigin = GetKneeCheckOrigin();

            Gizmos.DrawLine(chestOrigin, chestOrigin + (facingDir * wallCheckDistance));
            Gizmos.DrawLine(kneeOrigin, kneeOrigin + (facingDir * wallCheckDistance));
        }
        #endregion
    }
}

