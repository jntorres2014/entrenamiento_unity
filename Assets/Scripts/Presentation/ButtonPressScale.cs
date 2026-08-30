using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Feedback táctil y estilo visual común para los botones de la app.
    /// La jerarquía de color se resuelve por el nombre del objeto para poder
    /// modernizar la escena existente sin modificar la lógica ni referencias.
    /// </summary>
    public class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.96f;

        private Vector3 _restScale;

        private void Awake()
        {
            _restScale = transform.localScale;
            ApplyVisualStyle();
        }

        private void ApplyVisualStyle()
        {
            var button = GetComponent<Button>();
            if (button == null) return;

            string objectName = gameObject.name.ToLowerInvariant();

            if (objectName.Contains("reject") || objectName.Contains("cancel") || objectName.Contains("delete"))
            {
                TrainingUiStyler.StyleDanger(button);
            }
            else if (objectName.Contains("accept") || objectName.Contains("confirm"))
            {
                TrainingUiStyler.StylePositive(button);
            }
            else if (objectName.Contains("station") && objectName.Contains("role"))
            {
                TrainingUiStyler.StyleInfo(button);
            }
            else if (objectName.Contains("start") || objectName.Contains("host") || objectName.Contains("restart"))
            {
                TrainingUiStyler.StylePrimary(button);
            }
            else
            {
                TrainingUiStyler.StyleSecondary(button);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = _restScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _restScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _restScale;
        }

        private void OnDisable()
        {
            transform.localScale = _restScale;
        }
    }
}
