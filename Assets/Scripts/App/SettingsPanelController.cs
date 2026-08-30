using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Entrenamiento.App
{
    /// <summary>
    /// Pantalla de Ajustes: sonido sí/no, vibración sí/no y volver.
    /// MonoBehaviour fino: solo UI; la persistencia vive en AppSettings.
    ///
    /// Se abre con el botón "AJUSTES" del panel de rol y al volver
    /// restaura ese panel. Al activar la vibración hace una vibración de
    /// prueba en el teléfono, para que el cambio se sienta al instante.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Paneles")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject rolePanel;

        [Header("Botones")]
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private Button backButton;

        [Header("Labels")]
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private TMP_Text vibrationLabel;
        [SerializeField] private TMP_Text hintLabel;

        private const string DefaultHint = "Los cambios se guardan solos.";

        private void Start()
        {
            openSettingsButton.onClick.AddListener(Open);
            backButton.onClick.AddListener(Close);
            soundButton.onClick.AddListener(ToggleSound);
            vibrationButton.onClick.AddListener(ToggleVibration);

            settingsPanel.SetActive(false);
            RefreshUi();
        }

        private void Open()
        {
            hintLabel.text = DefaultHint;
            RefreshUi();
            rolePanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void Close()
        {
            settingsPanel.SetActive(false);
            rolePanel.SetActive(true);
        }

        private void ToggleSound()
        {
            AppSettings.SoundEnabled = !AppSettings.SoundEnabled;
            hintLabel.text = AppSettings.SoundEnabled
                ? "Sonido activado."
                : "Sonido apagado.";
            RefreshUi();
        }

        private void ToggleVibration()
        {
            AppSettings.VibrationEnabled = !AppSettings.VibrationEnabled;

            if (AppSettings.VibrationEnabled)
            {
                AppSettings.Vibrate(); // vibración de prueba (solo teléfono)
                hintLabel.text = Application.isEditor
                    ? "La vibración se prueba en el teléfono."
                    : "¿La sentiste? Así avisa tu estación.";
            }
            else
            {
                hintLabel.text = "Vibración apagada.";
            }

            RefreshUi();
        }

        private void RefreshUi()
        {
            soundLabel.text = AppSettings.SoundEnabled
                ? "Sonido: SÍ"
                : "Sonido: NO";
            vibrationLabel.text = AppSettings.VibrationEnabled
                ? "Vibración: SÍ"
                : "Vibración: NO";
        }
    }
}
