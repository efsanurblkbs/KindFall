using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin koşma (Run) durumu.
    /// Yatay hız, yön dönmesi (Flip) ve durma kontrollerini yönetir.
    /// </summary>
    public class PrincessRunState : PrincessGroundedState
    {
        public PrincessRunState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Run")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (stateMachine.CurrentState != this) return;

            // Yatay girdi bırakıldığında Idle durumuna dön
            if (Mathf.Abs(controller.MoveInput.x) <= 0.05f)
            {
                stateMachine.ChangeState(controller.IdleState);
                return;
            }

            // Girdi yönüne göre karakterin bakış açısını güncelle
            controller.CheckFlip(controller.MoveInput.x);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            // Unity 6 linearVelocity standardına uygun yatay hareket
            float targetSpeed = controller.MoveInput.x * controller.MoveSpeed;
            controller.SetVelocityX(targetSpeed);
        }
    }
}

