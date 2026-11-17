namespace KindFall.Core.Interfaces
{
    /// <summary>
    /// Yavru Ejderhanın ateş topuyla yakılabilen veya eritilebilen dünya nesneleri
    /// (dikenli sarmaşık, buz bariyeri, ateş kristali vb.) için ortak arayüz.
    /// </summary>
    public interface IBurnable
    {
        /// <summary>
        /// Nesnenin tamamen yanıp yanmadığını (veya aktifleşip aktifleşmediğini) döner.
        /// </summary>
        bool IsBurned { get; }

        /// <summary>
        /// Nesneye ateş hasarı/etkisi uygular.
        /// </summary>
        /// <param name="fireDamage">Ateş topunun uyguladığı etki miktarı.</param>
        void Burn(float fireDamage);
    }
}
