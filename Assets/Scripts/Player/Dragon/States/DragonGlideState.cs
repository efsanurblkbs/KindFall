using UnityEngine;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderhanın havada serbest süzülme (Glide) durumu.
    /// Yere temas etmeden 2D uzayda (A/D/W/S) akıcı şekilde süzülür; Prenses taşırken stamina tüketir.
    /// </summary>
    public class DragonGlideState : DragonState
    {
        public DragonGlideState(DragonController controller, DragonStateMachine stateMachine, string animBoolName = "Glide")
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

            // Prenses taşırken havada süzülme de stamina tüketir
            if (controller.IsCarryingPrincess)
            {
                controller.ConsumeStamina(controller.GlideCarryStaminaCost * Time.deltaTime);
            }

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

            if (controller.MoveInput.sqrMagnitude <= 0.0025f)
            {
                stateMachine.ChangeState(controller.IdleState);
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

            Vector2 dir = controller.MoveInput.normalized;
            controller.SetVelocity(dir * controller.GetCurrentGlideSpeed());
        }
    }
}
