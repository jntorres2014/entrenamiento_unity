using UnityEngine;

namespace Entrenamiento.App
{
    /// <summary>
    /// Preferencias del usuario (sonido y vibración) persistidas en
    /// PlayerPrefs. Punto único de verdad: cualquier código que quiera
    /// sonar o vibrar pasa por acá, así el toggle de Ajustes manda siempre.
    /// </summary>
    public static class AppSettings
    {
        private const string SoundKey = "settings.soundEnabled";
        private const string VibrationKey = "settings.vibrationEnabled";

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Vibra el teléfono solo si la vibración está habilitada.
        /// En el Editor no hace nada. Usar siempre este helper en lugar de
        /// llamar a Handheld.Vibrate() directo.
        /// </summary>
        public static void Vibrate()
        {
            if (!VibrationEnabled)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
