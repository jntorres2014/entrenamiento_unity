using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Navegación móvil global: Atrás del sistema, swipe desde bordes y safe area.
    /// </summary>
    public sealed class MobileBackGestureController : MonoBehaviour
    {
        private static readonly string[] TopBackButtonNames =
        {
            "ModernBackButton", "ARBackButton", "CameraBackButton", "SoloCameraBack", "ProgressBackButton"
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
            HandleMouseSwipe();
#endif
            bool changed = Screen.safeArea != _lastSafeArea || Screen.width != _lastWidth || Screen.height != _lastHeight;
            if (changed || Time.unscaledTime >= _nextLayoutRefresh)
            {
                ApplyBackButtonSafeArea();
                _nextLayoutRefresh = Time.unscaledTime + 0.35f;
            }
        }

        private void HandleTouchSwipe()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null || Screen.width <= 0) return;
            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                Vector2 p = touch.position.ReadValue();
                float edge = Mathf.Max(36f, Screen.width * 0.085f);
                _trackingTouch = p.x <= edge || p.x >= Screen.width - edge;
                _gestureFromLeft = p.x <= edge;
                _touchStart = p;
                _touchStartedAt = Time.unscaledTime;
            }
            if (!_trackingTouch) return;
            if (touch.press.wasReleasedThisFrame)
            {
                Vector2 end = touch.position.ReadValue();
                bool valid = IsBackSwipe(_touchStart, end, _gestureFromLeft, Time.unscaledTime - _touchStartedAt);
                _trackingTouch = false;
                if (valid) PerformBackGesture();
            }
        }

#if UNITY_EDITOR
        private void HandleMouseSwipe()
        {
            var mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0) return;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 p = mouse.position.ReadValue();
                float edge = Mathf.Max(36f, Screen.width * 0.085f);
                _trackingMouse = p.x <= edge || p.x >= Screen.width - edge;
                _mouseFromLeft = p.x <= edge;
                _mouseStart = p;
                _mouseStartedAt = Time.unscaledTime;
            }
            if (!_trackingMouse) return;
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                Vector2 end = mouse.position.ReadValue();
                bool valid = IsBackSwipe(_mouseStart, end, _mouseFromLeft, Time.unscaledTime - _mouseStartedAt);
                _trackingMouse = false;
                if (valid) PerformBackGesture();
            }
        }
#endif

        private static bool IsBackSwipe(Vector2 start, Vector2 end, bool fromLeft, float duration)
        {
            if (duration > 1.10f) return false;
            Vector2 delta = end - start;
            float horizontal = Mathf.Max(90f, Screen.width * 0.14f);
            float vertical = Mathf.Max(90f, Screen.height * 0.16f);
            if (Mathf.Abs(delta.y) > vertical) return false;
            return fromLeft ? delta.x >= horizontal : delta.x <= -horizontal;
        }

        private void PerformBackGesture()
        {
            if (IsRootHomeVisible()) return;

            string[] buttons =
            {
                "ProgressBackButton",
                "SoloCameraBack", "SoloOptionsBack", "SoloSelectorBack",
                "CameraBackButton", "ARBackButton", "ExerciseBackButton", "ModernBackButton"
            };

            foreach (string name in buttons)
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
            var role = FindDeep("RolePanel");
            if (role == null || !role.activeInHierarchy) return false;
            string[] overlays =
            {
                "ExerciseSelectionPanel", "SoloTrainingUI", "CameraTrainingUI", "ARTrainingUI", "TrainingProgressPanel"
            };
            foreach (string name in overlays)
            {
                var overlay = FindDeep(name);
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
            float left = safe.xMin / Screen.width;
            float top = safe.yMax / Screen.height;
            bool landscape = Screen.width > Screen.height;
            float width = landscape ? 0.18f : 0.20f;
            float height = landscape ? 0.085f : 0.056f;
            float xMin = Mathf.Clamp01(left + 0.025f);
            float xMax = Mathf.Clamp01(xMin + width);
            float yMax = Mathf.Clamp01(top - 0.016f);
            float yMin = Mathf.Clamp01(yMax - height);

            foreach (string name in TopBackButtonNames)
            {
                var go = FindDeep(name);
                if (go == null) continue;
                var rect = go.GetComponent<RectTransform>();
                if (rect == null) continue;
                rect.anchorMin = new Vector2(xMin, yMin);
                rect.anchorMax = new Vector2(xMax, yMax);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                if (go.activeInHierarchy) go.transform.SetAsLastSibling();
            }
        }

        private GameObject FindDeep(string objectName)
        {
            if (_canvas == null) return null;
            foreach (var t in _canvas.GetComponentsInChildren<Transform>(true))
                if (t.name == objectName) return t.gameObject;
            return null;
        }
    }
}
