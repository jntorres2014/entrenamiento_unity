using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Navegación móvil global para TrainingNearby.
    /// - Respeta el gesto Atrás del sistema que cada modo ya recibe como Escape.
    /// - Agrega swipe desde cualquiera de los bordes laterales como alternativa.
    /// - Mantiene los botones Atrás superiores dentro de Screen.safeArea.
    ///
    /// El swipe intenta pulsar el botón Atrás propio del contexto actual; si no
    /// existe, vuelve a cargar TrainingNearby como retorno limpio a la Home.
    /// </summary>
    public sealed class MobileBackGestureController : MonoBehaviour
    {
        private static readonly string[] TopBackButtonNames =
        {
            "ModernBackButton",
            "ARBackButton",
            "CameraBackButton",
            "SoloCameraBack"
        };

        private Canvas _canvas;
        private bool _trackingTouch;
        private bool _gestureFromLeft;
        private Vector2 _touchStart;
        private float _touchStartedAt;

        private bool _trackingMouse;
        private bool _mouseFromLeft;
        private Vector2 _mouseStart;
        private float _mouseStartedAt;

        private Rect _lastSafeArea = Rect.zero;
        private int _lastWidth;
        private int _lastHeight;
        private float _nextLayoutRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<MobileBackGestureController>() == null)
                {
                    canvas.gameObject.AddComponent<MobileBackGestureController>();
                    break;
                }
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            ApplyBackButtonSafeArea();
        }

        private void Update()
        {
            HandleTouchSwipe();

#if UNITY_EDITOR
            // Permite probar el mismo gesto con mouse dentro del Game View.
            HandleMouseSwipe();
#endif

            bool screenChanged = Screen.safeArea != _lastSafeArea ||
                                 Screen.width != _lastWidth ||
                                 Screen.height != _lastHeight;

            // Algunos botones se crean varios frames después del Canvas.
            if (screenChanged || Time.unscaledTime >= _nextLayoutRefresh)
            {
                ApplyBackButtonSafeArea();
                _nextLayoutRefresh = Time.unscaledTime + 0.35f;
            }
        }

        private void HandleTouchSwipe()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null || Screen.width <= 0 || Screen.height <= 0) return;

            var touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                Vector2 position = touch.position.ReadValue();
                float edge = Mathf.Max(36f, Screen.width * 0.085f);

                _trackingTouch = position.x <= edge || position.x >= Screen.width - edge;
                _gestureFromLeft = position.x <= edge;
                _touchStart = position;
                _touchStartedAt = Time.unscaledTime;
            }

            if (!_trackingTouch) return;

            if (touch.press.wasReleasedThisFrame)
            {
                Vector2 end = touch.position.ReadValue();
                bool valid = IsBackSwipe(_touchStart, end, _gestureFromLeft, Time.unscaledTime - _touchStartedAt);
                _trackingTouch = false;

                if (valid)
                {
                    PerformBackGesture();
                }
            }
        }

#if UNITY_EDITOR
        private void HandleMouseSwipe()
        {
            var mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0 || Screen.height <= 0) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 position = mouse.position.ReadValue();
                float edge = Mathf.Max(36f, Screen.width * 0.085f);

                _trackingMouse = position.x <= edge || position.x >= Screen.width - edge;
                _mouseFromLeft = position.x <= edge;
                _mouseStart = position;
                _mouseStartedAt = Time.unscaledTime;
            }

            if (!_trackingMouse) return;

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                Vector2 end = mouse.position.ReadValue();
                bool valid = IsBackSwipe(_mouseStart, end, _mouseFromLeft, Time.unscaledTime - _mouseStartedAt);
                _trackingMouse = false;

                if (valid)
                {
                    PerformBackGesture();
                }
            }
        }
#endif

        private static bool IsBackSwipe(Vector2 start, Vector2 end, bool fromLeft, float duration)
        {
            if (duration > 1.10f) return false;

            Vector2 delta = end - start;
            float horizontalNeeded = Mathf.Max(90f, Screen.width * 0.14f);
            float verticalTolerance = Mathf.Max(90f, Screen.height * 0.16f);

            if (Mathf.Abs(delta.y) > verticalTolerance) return false;

            return fromLeft
                ? delta.x >= horizontalNeeded
                : delta.x <= -horizontalNeeded;
        }

        private void PerformBackGesture()
        {
            // En la portada el gesto no cierra la app accidentalmente.
            if (IsRootHomeVisible()) return;

            // Priorizamos el botón propio de cada flujo para conservar su lógica.
            string[] contextualBackButtons =
            {
                "SoloCameraBack",
                "SoloOptionsBack",
                "SoloSelectorBack",
                "CameraBackButton",
                "ARBackButton",
                "ExerciseBackButton",
                "ModernBackButton"
            };

            foreach (string name in contextualBackButtons)
            {
                var go = FindDeep(name);
                if (go == null || !go.activeInHierarchy) continue;

                var button = go.GetComponent<Button>();
                if (button == null || !button.interactable) continue;

                button.onClick.Invoke();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private bool IsRootHomeVisible()
        {
            var rolePanel = FindDeep("RolePanel");
            if (rolePanel == null || !rolePanel.activeInHierarchy) return false;

            string[] overlays =
            {
                "ExerciseSelectionPanel",
                "SoloTrainingUI",
                "CameraTrainingUI",
                "ARTrainingUI"
            };

            foreach (string overlayName in overlays)
            {
                var overlay = FindDeep(overlayName);
                if (overlay != null && overlay.activeInHierarchy) return false;
            }

            return true;
        }

        private void ApplyBackButtonSafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            _lastSafeArea = safe;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            float safeLeft = safe.xMin / Screen.width;
            float safeTop = safe.yMax / Screen.height;

            bool landscape = Screen.width > Screen.height;
            float width = landscape ? 0.18f : 0.27f;
            float height = landscape ? 0.085f : 0.060f;
            float marginX = landscape ? 0.018f : 0.025f;
            float marginTop = landscape ? 0.025f : 0.018f;

            float xMin = Mathf.Clamp01(safeLeft + marginX);
            float xMax = Mathf.Clamp01(xMin + width);
            float yMax = Mathf.Clamp01(safeTop - marginTop);
            float yMin = Mathf.Clamp01(yMax - height);

            foreach (string buttonName in TopBackButtonNames)
            {
                var go = FindDeep(buttonName);
                if (go == null) continue;

                var rect = go.GetComponent<RectTransform>();
                if (rect == null) continue;

                rect.anchorMin = new Vector2(xMin, yMin);
                rect.anchorMax = new Vector2(xMax, yMax);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;

                // Siempre sobre HUD/tarjetas del modo actual.
                if (go.activeInHierarchy)
                {
                    go.transform.SetAsLastSibling();
                }
            }
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;

            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }

            return null;
        }
    }
}
