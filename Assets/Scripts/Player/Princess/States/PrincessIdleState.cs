using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin hareketsiz bekleme (Idle) durumu.
    /// Zemin sürtünmesi uygular ve yatay girdi geldiğinde Run durumuna geçer.
    /// </summary>
    public class PrincessIdleState : PrincessGroundedState
    {
        public PrincessIdleState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Idle")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            // Durma aşamasına girildiğinde zemin sürtünmesi devreye girer
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // Eğer başka bir duruma geçilmediyse yatay girdi kontrolü
            if (stateMachine.CurrentState != this) return;

            if (Mathf.Abs(controller.MoveInput.x) > 0.05f)
            {
                stateMachine.ChangeState(controller.RunState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            // Zemin sürtünmesi (drag) uygula
            controller.ApplyGroundFriction();
        }
    }
}

