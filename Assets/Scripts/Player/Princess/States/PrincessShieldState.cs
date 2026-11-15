using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin kalkanla siper alma (Shield) durumu.
    /// Alınan hasarı %70 azaltır ve hareket hızını %50 oranında düşürür.
    /// Kalkan tuşu bırakıldığında zemin durumuna geri döner.
    /// </summary>
    public class PrincessShieldState : PrincessGroundedState
    {
        public PrincessShieldState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Shield")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            // Kalkan korumasını aktif et (%70 hasar azaltımı)
            controller.SetShieldActive(true);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // Kalkan tuşu bırakıldığında zemin durumuna dön
            if (!controller.ShieldInputHeld)
            {
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

            // Siper halindeyken yön dönmesi
            if (Mathf.Abs(controller.MoveInput.x) > 0.05f)
            {
                controller.CheckFlip(controller.MoveInput.x);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            // Kalkan açıkken hareket hızı %50'ye düşürülür
            float reducedSpeed = controller.MoveInput.x * controller.MoveSpeed * controller.ShieldSpeedMultiplier;
            controller.SetVelocityX(reducedSpeed);
        }

        public override void Exit()
        {
            base.Exit();
            // Kalkan korumasını kapat
            controller.SetShieldActive(false);
        }
    }
}

