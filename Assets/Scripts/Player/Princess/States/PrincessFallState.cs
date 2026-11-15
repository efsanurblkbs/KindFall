using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin düşüş (Fall) durumu.
    /// Yerçekimi hızlanması, coyote time ile zıplama şansı ve yere iniş kontrollerini yönetir.
    /// </summary>
    public class PrincessFallState : PrincessState
    {
        public PrincessFallState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Fall")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // Düşüş durumuna girildiğinde artırılmış yerçekimi ölçeğini uygula
            controller.ApplyFallGravity();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // Havadayken saldırı girdisi
            if (controller.AttackInputDown)
            {
                stateMachine.ChangeState(controller.AttackState);
                return;
            }

            // Coyote time süresi içindeyken zıplama komutu verilirse zıplamaya izin ver
            if (controller.CanUseCoyoteJump() && controller.ConsumeJumpBuffer())
            {
                stateMachine.ChangeState(controller.JumpState);
                return;
            }

            // Duvara dokunuyor, düşüyor ve duvara doğru basıyorsa WallSlide'a geç
            if (controller.CheckIsTouchingWall() && controller.IsPushingAgainstWall())
            {
                stateMachine.ChangeState(controller.WallSlideState);
                return;
            }

            // Yere indiğinde
            if (controller.CheckIsGrounded() && controller.CurrentVelocity.y <= 0.05f)
            {
                // Yere inişte jump buffer kontrolü: Yere basmadan hemen önce basılmışsa zıplat
                if (controller.ConsumeJumpBuffer())
                {
                    stateMachine.ChangeState(controller.JumpState);
                    return;
                }

                if (Mathf.Abs(controller.MoveInput.x) > 0.05f)
                {
                    stateMachine.ChangeState(controller.RunState);
                }
                else
                {
                    stateMachine.ChangeState(controller.IdleState);
                }
                return;
            }

            // Havada yön dönmesi
            if (Mathf.Abs(controller.MoveInput.x) > 0.05f)
            {
                controller.CheckFlip(controller.MoveInput.x);
            }
        }

        public override void Exit()
        {
            base.Exit();

            // Düşüş bittiğinde yerçekimi ölçeğini varsayılana döndür
            controller.ResetGravityScale();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            // Havada yatay hareket
            float targetSpeed = controller.MoveInput.x * controller.AirMoveSpeed;
            controller.SetVelocityX(targetSpeed);

            // Düşüş yerçekimi çarpanı (tok ve profesyonel his)
            controller.ApplyFallGravity();
        }
    }
}

