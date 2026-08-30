using System;

namespace Entrenamiento.Transport
{
    /// <summary>
    /// Abstracción del transporte de mensajes entre el anfitrión y las estaciones.
    /// La lógica de entrenamiento (Core) solo conoce esta interfaz; nunca conoce
    /// si por debajo hay Nearby Connections, sockets, o una simulación en memoria.
    /// Mensajes representados como texto simple (payload) para el MVP; se puede
    /// migrar a un formato más estructurado (JSON) sin romper el contrato.
    /// </summary>
    public interface ILocalTransport
    {
        /// <summary>
        /// Se dispara cuando llega un mensaje de una estación identificada por su id.
        /// </summary>
        event Action<string, string> OnMessageReceived;

        /// <summary>
        /// Se dispara cuando una estación se conecta (id de la estación).
        /// </summary>
        event Action<string> OnStationConnected;

        /// <summary>
        /// Se dispara cuando una estación se desconecta (id de la estación).
        /// </summary>
        event Action<string> OnStationDisconnected;

        /// <summary>
        /// Envía un mensaje a una estación puntual.
        /// </summary>
        void SendToStation(string stationId, string payload);

        /// <summary>
        /// Envía un mensaje a todas las estaciones conectadas.
        /// </summary>
        void Broadcast(string payload);
    }
}
