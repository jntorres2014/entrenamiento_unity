using System;
using System.Collections.Generic;

namespace Entrenamiento.Transport
{
    /// <summary>
    /// Transporte simulado en memoria: no usa red real. Permite probar el flujo
    /// completo (activar estación -> "toque" -> registro de evento) dentro del
    /// mismo proceso, sin depender de Nearby Connections ni de otro dispositivo.
    ///
    /// El método SimulateIncomingTouch representa lo que en la versión real
    /// llegaría desde una estación remota a través de Nearby Connections.
    /// </summary>
    public class SimulatedTransport : ILocalTransport
    {
        private readonly HashSet<string> _connectedStations = new HashSet<string>();

        public event Action<string, string> OnMessageReceived;
        public event Action<string> OnStationConnected;
        public event Action<string> OnStationDisconnected;

        /// <summary>
        /// Simula que una estación se conecta a la sala (equivalente a que un
        /// teléfono real se una vía Nearby Connections).
        /// </summary>
        public void SimulateStationConnected(string stationId)
        {
            if (_connectedStations.Add(stationId))
            {
                OnStationConnected?.Invoke(stationId);
            }
        }

        public void SimulateStationDisconnected(string stationId)
        {
            if (_connectedStations.Remove(stationId))
            {
                OnStationDisconnected?.Invoke(stationId);
            }
        }

        /// <summary>
        /// Simula que llega un mensaje desde una estación (por ejemplo, el aviso
        /// de que el deportista tocó la pantalla). En la versión real, este
        /// mismo evento (OnMessageReceived) se dispararía al recibir datos por
        /// Nearby Connections.
        /// </summary>
        public void SimulateIncomingTouch(string stationId, string payload)
        {
            if (!_connectedStations.Contains(stationId))
            {
                throw new InvalidOperationException(
                    $"La estación '{stationId}' no está conectada en el transporte simulado.");
            }

            OnMessageReceived?.Invoke(stationId, payload);
        }

        public void SendToStation(string stationId, string payload)
        {
            if (!_connectedStations.Contains(stationId))
            {
                UnityEngine.Debug.LogWarning(
                    $"[SimulatedTransport] Intento de enviar a estación no conectada: {stationId}");
                return;
            }

            UnityEngine.Debug.Log($"[SimulatedTransport] -> {stationId}: {payload}");
        }

        public void Broadcast(string payload)
        {
            foreach (var stationId in _connectedStations)
            {
                UnityEngine.Debug.Log($"[SimulatedTransport] -> {stationId} (broadcast): {payload}");
            }
        }
    }
}
