using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakter durumlarının (FSM States) türediği soyut temel sınıf.
    /// Her durum Giriş (Enter), Mantık Güncellemesi (LogicUpdate),
    /// Fizik Güncellemesi (PhysicsUpdate) ve Çıkış (Exit) yaşam döngülerine sahiptir.
    /// </summary>
    public abstract class PrincessState
    {
        /// <summary>
        /// Durumun bağlı olduğu ana kontrolcü referansı.
        /// </summary>
        protected readonly PrincessController controller;

        /// <summary>
        /// Durum geçişlerini yöneten durum makinesi referansı.
        /// </summary>
        protected readonly PrincessStateMachine stateMachine;

        /// <summary>
        /// Duruma girildiğinde Animator üzerinde aktif edilecek bool parametre adı.
        /// </summary>
        protected readonly string animBoolName;

        /// <summary>
        /// Duruma ilk girildiği zaman damgası (Time.time).
        /// </summary>
        protected float startTime;

        /// <summary>
        /// Yeni bir durum örneği başlatır.
        /// </summary>
        /// <param name="controller">Durumu yürüten Prenses kontrolcüsü.</param>
        /// <param name="stateMachine">Durum geçişlerini yöneten durum makinesi.</param>
        /// <param name="animBoolName">Animator üzerindeki bool parametre adı (isteğe bağlı).</param>
        protected PrincessState(PrincessController controller, PrincessStateMachine stateMachine, string animBoolName)
        {
            this.controller = controller;
            this.stateMachine = stateMachine;
            this.animBoolName = animBoolName;
        }

        /// <summary>
        /// Bu duruma ilk girildiğinde tetiklenir.
        /// </summary>
        public virtual void Enter()
        {
            startTime = Time.time;

            if (!string.IsNullOrEmpty(animBoolName) && controller.Anim != null)
            {
                controller.Anim.SetBool(animBoolName, true);
            }
        }

        /// <summary>
        /// Her karede (Update) durum mantığını, girdileri ve geçiş koşullarını denetler.
        /// </summary>
        public virtual void LogicUpdate()
        {
        }

        /// <summary>
        /// Her fizik adımında (FixedUpdate) hız ve kuvvet hesaplamalarını yürütür.
        /// </summary>
        public virtual void PhysicsUpdate()
        {
        }

        /// <summary>
        /// Bu durumdan başka bir duruma geçilirken temizlik ve sıfırlama işlemlerini yapar.
        /// </summary>
        public virtual void Exit()
        {
            if (!string.IsNullOrEmpty(animBoolName) && controller.Anim != null)
            {
                controller.Anim.SetBool(animBoolName, false);
            }
        }
    }
}

