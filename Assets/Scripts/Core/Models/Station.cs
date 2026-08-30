using System;

namespace Entrenamiento.Core.Models
{
    /// <summary>
    /// Colores posibles que puede mostrar una estación a pantalla completa.
    /// </summary>
    public enum StationColor
    {
        None,
        Red,
        Green,
        Blue,
        Yellow
    }

    /// <summary>
    /// Estado actual de una estación dentro de la sesión de entrenamiento.
    /// </summary>
    public enum StationState
    {
        Idle,       // Conectada, esperando indicación
        Active,     // Mostrando color, esperando toque del deportista
        Touched     // Ya fue tocada durante la ronda actual
    }

    /// <summary>
    /// Representa una estación (teléfono) dentro de la sala de entrenamiento.
    /// Clase de datos pura: no depende de Unity ni de MonoBehaviour.
    /// </summary>
    [Serializable]
    public class Station
    {
        public string Id { get; }
        public string DisplayName { get; }
        public StationState State { get; private set; }
        public StationColor CurrentColor { get; private set; }

        public Station(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("El id de la estación no puede ser vacío.", nameof(id));
            }

            Id = id;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            State = StationState.Idle;
            CurrentColor = StationColor.None;
        }

        public void Activate(StationColor color)
        {
            CurrentColor = color;
            State = StationState.Active;
        }

        public void MarkTouched()
        {
            State = StationState.Touched;
        }

        public void Reset()
        {
            State = StationState.Idle;
            CurrentColor = StationColor.None;
        }
    }
}
