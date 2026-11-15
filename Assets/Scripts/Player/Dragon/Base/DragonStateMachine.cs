using System;
using UnityEngine;

namespace KindFall.Player.Dragon
{
    /// <summary>
    /// Yavru Ejderha karakterinin durum geçişlerini ve mevcut durumunu yöneten FSM durum makinesi.
    /// Gözlemci Deseni (Observer Pattern) ile durum değişikliklerini OnStateChanged olayı üzerinden duyurur.
    /// </summary>
    public class DragonStateMachine
    {
        /// <summary>
        /// Ejderhanın o an içinde bulunduğu aktif durum.
        /// </summary>
        public DragonState CurrentState { get; private set; }

        /// <summary>
        /// Durum değiştiğinde tetiklenen gözlemci olayı (Önceki Durum, Yeni Durum).
        /// </summary>
        public event Action<DragonState, DragonState> OnStateChanged;

        /// <summary>
        /// Durum makinesini başlangıç durumuyla çalıştırır.
        /// </summary>
        /// <param name="startingState">Karakterin başlayacağı ilk durum.</param>
        public void Initialize(DragonState startingState)
        {
            if (startingState == null)
            {
                Debug.LogError("[DragonStateMachine] Başlangıç durumu boş (null) olamaz!");
                return;
            }

            CurrentState = startingState;
            CurrentState.Enter();
        }

        /// <summary>
        /// Mevcut durumdan yeni bir duruma geçişi yönetir.
        /// </summary>
        /// <param name="newState">Geçiş yapılacak hedef durum.</param>
        public void ChangeState(DragonState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[DragonStateMachine] Hedef durum null olduğu için geçiş iptal edildi.");
                return;
            }

            if (CurrentState == newState)
            {
                return;
            }

            DragonState previousState = CurrentState;
            CurrentState?.Exit();

            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(previousState, newState);
        }
    }
}

