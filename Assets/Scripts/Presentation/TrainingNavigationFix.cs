using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Mantiene la navegación dentro de la UI moderna.
    /// Evita que el botón Atrás recargue TrainingNearby y vuelva a mostrar
    /// momentáneamente (o de forma permanente) el RolePanel serializado viejo.
    /// También reinstala los controladores runtime si algún flujo necesita
    /// recargar la escena completa, como la salida del modo AR.
    /// </summary>
    public sealed class TrainingNavigationFix : MonoBehaviour
    {
        private Button _modernBackButton;
        private bool _backHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "TrainingNearby") return;

            Canvas canvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in canvases)
            {
                if (candidate != null && candidate.isRootCanvas)
                {
                    canvas = candidate;
                    break;
                }
            }

            if (canvas == null) return;

            // En una recarga de escena, RuntimeInitializeOnLoadMethod no vuelve a
            // construir necesariamente todos los componentes runtime. Los dejamos
            // explícitamente presentes para que la Home C sea siempre la portada.
            if (canvas.GetComponent<TrainingModernUiController>() == null)
                canvas.gameObject.AddComponent<TrainingModernUiController>();

            if (canvas.GetComponent<TrainingUiMagic>() == null)
                canvas.gameObject.AddComponent<TrainingUiMagic>();

            if (canvas.GetComponent<ARTrainingModeController>() == null)
                canvas.gameObject.AddComponent<ARTrainingModeController>();

            if (canvas.GetComponent<TrainingHomeCView>() == null)
                canvas.gameObject.AddComponent<TrainingHomeCView>();

            if (canvas.GetComponent<TrainingNavigationFix>() == null)
                canvas.gameObject.AddComponent<TrainingNavigationFix>();
        }

        private void Update()
        {
            if (_backHooked && _modernBackButton != null) return;

            var go = FindDeep("ModernBackButton");
            if (go == null) return;

            _modernBackButton = go.GetComponent<Button>();
            if (_modernBackButton == null) return;

            // El controlador visual original recargaba la escena. Sustituimos
            // únicamente la acción del botón, manteniendo su aspecto y animación.
            _modernBackButton.onClick.RemoveAllListeners();
            _modernBackButton.onClick.AddListener(ReturnToModernHome);
            _backHooked = true;
        }

        private void ReturnToModernHome()
        {
            SetPanelActive("HostConfigPanel", false);
            SetPanelActive("HostProgressPanel", false);
            SetPanelActive("StationWaitPanel", false);
            SetPanelActive("SummaryPanel", false);

            var stationView = GetComponentInChildren<StationView>(true);
            if (stationView != null)
            {
                stationView.gameObject.SetActive(false);
            }

            var rolePanel = FindDeep("RolePanel");
            if (rolePanel != null)
            {
                rolePanel.SetActive(true);
            }

            var homeVisuals = FindDeep("HomeCVisuals");
            if (homeVisuals != null)
            {
                homeVisuals.SetActive(true);
                var group = homeVisuals.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1f;
            }

            SetObjectActive("HostRoleButton", true);
            SetObjectActive("StationRoleButton", true);
            SetObjectActive("ARTrainingButton", true);

            // En la portada no debe verse el botón global Atrás.
            if (_modernBackButton != null)
            {
                _modernBackButton.gameObject.SetActive(false);
            }
        }

        private void SetPanelActive(string objectName, bool active)
        {
            var go = FindDeep(objectName);
            if (go != null) go.SetActive(active);
        }

        private void SetObjectActive(string objectName, bool active)
        {
            var go = FindDeep(objectName);
            if (go != null) go.SetActive(active);
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }

            return null;
        }
    }
}
