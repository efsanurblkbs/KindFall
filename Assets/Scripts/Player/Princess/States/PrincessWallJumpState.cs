using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin duvardan zıt yöne açılı fırlama (WallJump) durumu.
    /// Ters yöne açılı kuvvet uygular ve 0.15 sn boyunca yön girdisini kilitler.
    /// </summary>
    public class PrincessWallJumpState : PrincessState
    {
        public PrincessWallJumpState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Jump")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // Yerçekimini varsayılana ayarla
            controller.ResetGravityScale();

            // Karakterin baktığı yönün (duvar yönünün) tam tersi yönü al
            int jumpDir = controller.IsFacingRight ? -1 : 1;

            // Ters yöne açılı fırlatma kuvveti uygula (X: 9f * jumpDir, Y: 13f)
            Vector2 force = new Vector2(controller.WallJumpDirection.x * jumpDir, controller.WallJumpDirection.y);
            controller.SetVelocity(force);

            // Oyuncunun bakış açısını zıpladığı yöne çevir
            controller.ForceFlip(jumpDir > 0);

            // 0.15 sn boyunca yatay yön girdisini kilitle
            controller.SetInputLock(controller.WallJumpInputLockTime);
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

            // Girdi kilidi bittiğinde kontrolü dikey hıza göre Jump veya Fall durumuna devret
            if (!controller.IsInputLocked)
            {
                if (controller.CurrentVelocity.y > 0f)
                {
                    stateMachine.ChangeState(controller.JumpState);
                }
                else
                {
                    stateMachine.ChangeState(controller.FallState);
                }
            }
        }
    }
}

