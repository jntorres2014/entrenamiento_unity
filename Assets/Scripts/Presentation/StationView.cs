using System;
using Entrenamiento.Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// MonoBehaviour "fino": solo muestra el color de la estación a pantalla completa
    /// y notifica cuando el usuario toca la pantalla. No contiene reglas de negocio;
    /// eso vive en Core.TrainingSession.
    ///
    /// Usa IPointerClickHandler (sistema de eventos de UI de Unity), que funciona
    /// tanto con el Input System nuevo como con el Input Manager clásico sin
    /// necesitar saber cuál está configurado en el proyecto.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class StationView : MonoBehaviour, IPointerClickHandler
    {
        public event Action OnTapped;

        private Image _backgroundImage;

        private void Awake()
        {
            EnsureImage();
        }

        /// <summary>
        /// Inicialización perezosa: si el GameObject arranca desactivado en la
        /// escena, Awake no corre hasta la primera activación, pero SetColor
        /// puede llamarse antes (por ejemplo, justo antes de SetActive(true)).
        /// </summary>
        private void EnsureImage()
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }
        }

        /// <summary>
        /// Aplica el color correspondiente a la estación activa.
        /// </summary>
        public void SetColor(StationColor color)
        {
            EnsureImage();
            _backgroundImage.color = StationColorPalette.ToUnityColor(color);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnTapped?.Invoke();
        }
    }
}
