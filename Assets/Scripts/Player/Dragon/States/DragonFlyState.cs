using UnityEngine;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderhanın stamina tüketerek hızlı uçtuğu (Fly Boost) durumu.
    /// Prenses taşınıyorsa ekstra stamina tüketir; stamina tükenirse süzülme veya bekleme durumuna geçer.
    /// </summary>
    public class DragonFlyState : DragonState
    {
        public DragonFlyState(DragonController controller, DragonStateMachine stateMachine, string animBoolName = "Fly")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            controller.SetZeroGravity();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            controller.ConsumeStamina(controller.FlyStaminaCost * Time.deltaTime);

            if (!controller.CanFlyBoost())
            {
                stateMachine.ChangeState(controller.MoveInput.sqrMagnitude > 0.0025f
                    ? controller.GlideState
                    : controller.IdleState);
                return;
            }

            if (controller.AttackInputDown && controller.CanFireball())
            {
                stateMachine.ChangeState(controller.AttackState);
                return;
            }

            if (!controller.FlyInputHeld)
            {
                stateMachine.ChangeState(controller.MoveInput.sqrMagnitude > 0.0025f
                    ? controller.GlideState
                    : controller.IdleState);
                return;
            }

            if (Mathf.Abs(controller.MoveInput.x) > 0.05f)
            {
                controller.CheckFlip(controller.MoveInput.x);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            Vector2 dir = controller.MoveInput.sqrMagnitude > 0.0025f
                ? controller.MoveInput.normalized
                : Vector2.up;

            controller.SetVelocity(dir * controller.FlyBoostSpeed);
        }
    }
}
