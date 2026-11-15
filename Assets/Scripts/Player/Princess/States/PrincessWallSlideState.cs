using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin duvarda kontrollü kayma (WallSlide) durumu.
    /// Düşüş hızını (linearVelocityY) maksimum -2.5f ile sınırlandırır.
    /// </summary>
    public class PrincessWallSlideState : PrincessState
    {
        public PrincessWallSlideState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "WallSlide")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // Duvara tutunulduğunda yatay kaymayı durdur ve yerçekimini sıfırla
            controller.StopX();
            controller.ResetGravityScale();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // Duvardayken zıplama tuşuna basılırsa WallJump durumuna geç
            if (controller.ConsumeJumpBuffer())
            {
                stateMachine.ChangeState(controller.WallJumpState);
                return;
            }

            // Yere temas ettiyse zemin durumuna dön
            if (controller.CheckIsGrounded())
            {
                stateMachine.ChangeState(controller.IdleState);
                return;
            }

            // Duvar temasını kaybettiyse veya oyuncu duvardan uzaklaşma yönüne basıyorsa düşüşe geç
            if (!controller.CheckIsTouchingWall() || !controller.IsPushingAgainstWall())
            {
                stateMachine.ChangeState(controller.FallState);
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            // Unity 6 linearVelocity standardı: Düşüş hızını -2.5f ile sınırla
            if (controller.CurrentVelocity.y < controller.WallSlideMaxFallSpeed)
            {
                controller.SetVelocityY(controller.WallSlideMaxFallSpeed);
            }
        }
    }
}

