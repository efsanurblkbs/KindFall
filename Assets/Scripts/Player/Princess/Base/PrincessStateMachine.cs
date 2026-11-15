using System;
using UnityEngine;

namespace KindFall.Player.Princess
{
    /// <summary>
    /// Prenses karakterinin durum geçişlerini ve mevcut durumunu yöneten FSM durum makinesi.
    /// Gözlemci Deseni (Observer Pattern) ile durum değişikliklerini OnStateChanged olayı üzerinden duyurur.
    /// </summary>
    public class PrincessStateMachine
    {
        /// <summary>
        /// Karakterin o an içinde bulunduğu aktif durum.
        /// </summary>
        public PrincessState CurrentState { get; private set; }

        /// <summary>
        /// Durum değiştiğinde tetiklenen gözlemci olayı (Önceki Durum, Yeni Durum).
        /// Bulmaca ve tetikleyici mekanizmaları için dinlenebilir.
        /// </summary>
        public event Action<PrincessState, PrincessState> OnStateChanged;

        /// <summary>
        /// Durum makinesini başlangıç durumuyla çalıştırır.
        /// </summary>
        /// <param name="startingState">Karakterin başlayacağı ilk durum (Örn: IdleState).</param>
        public void Initialize(PrincessState startingState)
        {
            if (startingState == null)
            {
                Debug.LogError("[PrincessStateMachine] Başlangıç durumu boş (null) olamaz!");
                return;
            }

            CurrentState = startingState;
            CurrentState.Enter();
        }

        /// <summary>
        /// Mevcut durumdan yeni bir duruma geçişi yönetir.
        /// </summary>
        /// <param name="newState">Geçiş yapılacak hedef durum.</param>
        public void ChangeState(PrincessState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[PrincessStateMachine] Hedef durum null olduğu için geçiş iptal edildi.");
                return;
            }

            if (CurrentState == newState)
            {
                return;
            }

            PrincessState previousState = CurrentState;
            CurrentState?.Exit();

            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(previousState, newState);
        }
    }
}

