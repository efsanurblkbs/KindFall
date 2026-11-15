using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin mükemmel savuşturma (Parry) penceresi durumu.
    /// Kalkan tuşuna basıldığı ilk 0.18 saniye (180 ms) boyunca aktiftir.
    /// Başarılı savuşturmada hasarı %100 engeller, hit-stop ve sersemletme tetikler.
    /// Süre bittiğinde kalkan tuşu basılıysa Shield durumuna geçer.
    /// </summary>
    public class PrincessParryState : PrincessState
    {
        public PrincessParryState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName = "Parry")
            : base(controller, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            // Savuşturma anında yatay hızı sıfırla
            controller.StopX();

            // Parry penceresini aktif et
            controller.SetParrying(true);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            float elapsed = Time.time - startTime;

            // Kalkan tuşu erken bırakılırsa durumu sonlandır
            if (!controller.ShieldInputHeld)
            {
                stateMachine.ChangeState(controller.IdleState);
                return;
            }

            // 0.18 sn savuşturma penceresi dolduysa ve kalkan basılı tutuluyorsa normal siper (Shield) durumuna geç
            if (elapsed >= controller.ParryWindowDuration)
            {
                stateMachine.ChangeState(controller.ShieldState);
            }
        }

        public override void Exit()
        {
            base.Exit();
            controller.SetParrying(false);
        }
    }
}

