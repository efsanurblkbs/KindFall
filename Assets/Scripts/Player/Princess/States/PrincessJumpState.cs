using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin yukarı yönlü dikey sıçrama (Jump) durumu.
    /// Zıplama kuvveti, variable jump (tuş bırakılınca artan yerçekimi) ve havada yatay kontrolü yönetir.
    /// </summary>
    public class PrincessJumpState : PrincessState
    {
        public PrincessJumpState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Jump")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // Zıplama başlangıcında yerçekimini varsayılan ölçeğe sıfırla
            controller.ResetGravityScale();

            // Zıplama dikey fırlatma hızını uygula
            controller.SetVelocityY(controller.JumpForce);
            controller.ClearCoyoteTimer();
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

            // Havada yön dönmesi
            if (Mathf.Abs(controller.MoveInput.x) > 0.05f)
            {
                controller.CheckFlip(controller.MoveInput.x);
            }

            // Duvara dokunuyorsa ve duvara doğru basıyorsa kayma durumuna geçiş
            if (controller.CheckIsTouchingWall() && controller.IsPushingAgainstWall())
            {
                stateMachine.ChangeState(controller.WallSlideState);
                return;
            }

            // Dikey hız sıfır veya altına düştüğünde düşme durumuna geçiş
            if (controller.CurrentVelocity.y <= 0f)
            {
                stateMachine.ChangeState(controller.FallState);
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            // Havada yatay hareket
            float targetSpeed = controller.MoveInput.x * controller.AirMoveSpeed;
            controller.SetVelocityX(targetSpeed);

            // Variable Jump: Tuş erken bırakılırsa yerçekimi çarpanı uygulanır
            if (!controller.JumpInputHeld && controller.CurrentVelocity.y > 0f)
            {
                controller.ApplyVariableJumpGravity();
            }
        }
    }
}

