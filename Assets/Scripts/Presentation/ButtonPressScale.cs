using UnityEngine;
using UnityEngine.EventSystems;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Feedback táctil simple: el botón se achica levemente mientras está
    /// presionado. Sin Animator, sin dependencias.
    /// </summary>
    public class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressedScale = 0.94f;

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}
