using System;

namespace KindFall.Core.Interfaces
{
    /// <summary>
    /// Kapı ve mekanizmaları tetikleyebilen tüm bulmaca bileşenlerinin
    /// (Ağırlık butonu, ateş kristali, uzak şalter vb.) uygulayacağı ortak Observer arayüzü.
    /// </summary>
    public interface IPuzzleTrigger
    {
        /// <summary>
        /// Tetikleyicinin şu anda aktif (basılı/yanıyor) olup olmadığını döner.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Tetikleyicinin durumu değiştiğinde tetiklenir (true: Aktif, false: Pasif).
        /// </summary>
        event Action<bool> OnStateChanged;
    }
}
