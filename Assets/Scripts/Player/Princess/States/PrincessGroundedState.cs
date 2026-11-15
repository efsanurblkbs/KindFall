using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Zemin üzerindeki durumların (Idle, Run vb.) ortak ata sınıfı.
    /// Zemin kontrollerini, zıplama ve düşüş geçiş tetikleyicilerini yönetir.
    /// </summary>
    public abstract class PrincessGroundedState : PrincessState
    {
        /// <summary>
        /// Yeni bir zemin durumu başlatır.
        /// </summary>
        protected PrincessGroundedState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName)
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            // Zemin durumuna girildiğinde zıplama hakkı yenilenir (Coyote time sıfırlanabilir)
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // Zıplama girdisi geldiyse zıplama durumuna geçiş kontrolü
            if (controller.ConsumeJumpBuffer())
            {
                stateMachine.ChangeState(controller.JumpState);
                return;
            }

            // Zeminden ayrıldıysa ve aşağı doğru düşüş başladıysa düşme durumuna geçiş
            if (!controller.CheckIsGrounded())
            {
                // Karakter boşluğa adım attığında Coyote time başlatılır
                controller.StartCoyoteTimer();
                stateMachine.ChangeState(controller.FallState);
                return;
            }

            // Saldırı girdisi
            if (controller.AttackInputDown)
            {
                stateMachine.ChangeState(controller.AttackState);
                return;
            }

            // Kalkan / Parry girdisi (Halihazırda kalkan siperinde değilsek yeni bir Parry penceresi başlat)
            if (this != controller.ShieldState && controller.ShieldInputHeld)
            {
                stateMachine.ChangeState(controller.ParryState);
                return;
            }
        }
    }
}

