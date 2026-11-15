using UnityEngine;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderhanın havada küçük ateş topu fırlatma (Attack) durumu.
    /// Atış tamamlandığında mevcut uçuş/süzülme/asılı kalma durumuna geri döner.
    /// </summary>
    public class DragonAttackState : DragonState
    {
        public DragonAttackState(DragonController controller, DragonStateMachine stateMachine, string animBoolName = "Attack")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            controller.SetZeroGravity();
            controller.FireFireball();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            float elapsed = Time.time - startTime;
            if (elapsed < controller.AttackDuration)
            {
                return;
            }

            if (controller.FlyInputHeld && controller.CanFlyBoost())
            {
                stateMachine.ChangeState(controller.FlyState);
            }
            else if (controller.MoveInput.sqrMagnitude > 0.0025f)
            {
                stateMachine.ChangeState(controller.GlideState);
            }
            else
            {
                stateMachine.ChangeState(controller.IdleState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (controller.MoveInput.sqrMagnitude > 0.0025f)
            {
                controller.SetVelocity(controller.MoveInput.normalized * (controller.GetCurrentGlideSpeed() * 0.7f));
            }
            else
            {
                controller.ApplyAirDamping();
            }
        }
    }
}
