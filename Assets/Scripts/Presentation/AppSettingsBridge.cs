namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Puente de compatibilidad para que los componentes de presentación
    /// reutilicen las preferencias globales sin duplicar configuración.
    /// </summary>
    internal static class AppSettings
    {
        public static bool SoundEnabled
        {
            get => Entrenamiento.App.AppSettings.SoundEnabled;
            set => Entrenamiento.App.AppSettings.SoundEnabled = value;
        }

        public static bool VibrationEnabled
        {
            get => Entrenamiento.App.AppSettings.VibrationEnabled;
            set => Entrenamiento.App.AppSettings.VibrationEnabled = value;
        }

        public static void Vibrate()
        {
            Entrenamiento.App.AppSettings.Vibrate();
        }
    }
}
