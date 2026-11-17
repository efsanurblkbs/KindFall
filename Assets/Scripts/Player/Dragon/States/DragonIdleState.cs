using UnityEngine;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderhanın havada asılı kalma (Hover / Idle) durumu.
    /// Ejderha oyun boyunca yere temas etmeden havada süzülür ve bu durumda stamina yenilenir.
    /// </summary>
    public class DragonIdleState : DragonState
    {
        public DragonIdleState(DragonController controller, DragonStateMachine stateMachine, string animBoolName = "Idle")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            controller.SetZeroGravity();
            controller.StopVelocity();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (controller.AttackInputDown && controller.CanFireball())
            {
                stateMachine.ChangeState(controller.AttackState);
                return;
            }

            if (controller.FlyInputHeld && controller.CanFlyBoost())
            {
                stateMachine.ChangeState(controller.FlyState);
                return;
            }

            if (controller.MoveInput.sqrMagnitude > 0.0025f)
            {
                stateMachine.ChangeState(controller.GlideState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            controller.ApplyAirDamping();
        }
    }
}
