using UnityEngine;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Ajusta el RectTransform propio al Screen.safeArea convirtiéndolo a
    /// anclas proporcionales, para que nada interactivo quede debajo del notch
    /// o de la barra de gestos. Va en un contenedor raíz dentro del Canvas;
    /// el fondo (gradiente) queda afuera para llegar hasta los bordes físicos.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;

        private void Awake()
        {
            Apply();
        }

        private void Update()
        {
            // Cubre rotaciones y cambios de resolución sin eventos dedicados.
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height);
            rt.anchorMax = new Vector2(safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
