using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin kılıç saldırı (Attack) durumu.
    /// Saldırı anında hafifçe ileri atılma (attackDashForce) ve hitbox zamanlayıcısını (0.1 sn) yönetir.
    /// </summary>
    public class PrincessAttackState : PrincessState
    {
        private bool hitboxActive;

        public PrincessAttackState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Attack")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            hitboxActive = false;

            // Saldırı anında hafif ileri atılma
            float dir = controller.IsFacingRight ? 1f : -1f;
            controller.SetVelocityX(dir * controller.AttackDashForce);

            // Hitbox'ı etkinleştir
            controller.EnableAttackHitbox();
            hitboxActive = true;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            float elapsed = Time.time - startTime;

            // 0.1 sn sonra hitbox'ı kapat
            if (hitboxActive && elapsed >= controller.AttackHitboxDuration)
            {
                controller.DisableAttackHitbox();
                hitboxActive = false;
            }

            // Toplam saldırı süresi tamamlandığında zemin/hareket durumuna dön
            if (elapsed >= controller.AttackDuration)
            {
                if (controller.CheckIsGrounded())
                {
                    stateMachine.ChangeState(Mathf.Abs(controller.MoveInput.x) > 0.05f ? controller.RunState : controller.IdleState);
                }
                else
                {
                    stateMachine.ChangeState(controller.FallState);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            // İleri atılmanın ardından zeminde sürtünme ile doğal yavaşlama
            if (controller.CheckIsGrounded())
            {
                controller.ApplyGroundFriction();
            }
        }

        public override void Exit()
        {
            base.Exit();

            if (hitboxActive)
            {
                controller.DisableAttackHitbox();
                hitboxActive = false;
            }
        }
    }
}

