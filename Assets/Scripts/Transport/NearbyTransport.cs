using System;
using UnityEngine;

namespace Entrenamiento.Transport
{
    /// <summary>
    /// Implementación de ILocalTransport que se comunica con el plugin Android
    /// nativo (Kotlin) vía AndroidJavaObject, usando Nearby Connections real.
    ///
    /// Protocolo de mensajes que llegan desde Kotlin (un string por evento,
    /// vía NearbyMessageReceiver -> ReceiveRawMessageFromPlugin):
    ///   CONNECTED|endpointId|endpointName
    ///   DISCONNECTED|endpointId
    ///   MSG|endpointId|payload
    ///   STATUS|texto            (diagnóstico: errores, progreso)
    ///   PONG:texto              (respuesta del ping de prueba, Tarea 4)
    ///
    /// El "stationId" que se expone por ILocalTransport es el endpointId de
    /// Nearby. La correspondencia endpointId -> nombre lógico de estación se
    /// resuelve en capas superiores si hace falta.
    ///
    /// Solo funciona en un dispositivo Android real; en el Editor usá
    /// SimulatedTransport.
    /// </summary>
    public class NearbyTransport : ILocalTransport
    {
        private const string PluginClassName = "com.entrenamiento.nearby.NearbyConnectionsPlugin";

        public event Action<string, string> OnMessageReceived;
        public event Action<string> OnStationConnected;
        public event Action<string> OnStationDisconnected;

        /// <summary>Mensajes STATUS| del plugin, para mostrar diagnóstico en UI de prueba.</summary>
        public event Action<string> OnStatus;

        /// <summary>
        /// (Solo host) Una estación pide unirse: (endpointId, nombre). Responder
        /// con AcceptStation o RejectStation.
        /// </summary>
        public event Action<string, string> OnConnectionRequest;

        private AndroidJavaObject _pluginInstance;
        private readonly string _receiverGameObjectName;

        /// <summary>
        /// receiverGameObjectName: nombre exacto del GameObject en la escena que
        /// tiene el componente NearbyMessageReceiver, donde el plugin Kotlin va a
        /// mandar los mensajes de vuelta vía UnitySendMessage.
        /// </summary>
        public NearbyTransport(string receiverGameObjectName)
        {
            _receiverGameObjectName = receiverGameObjectName;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _pluginInstance = new AndroidJavaObject(PluginClassName);
                Debug.Log("[NearbyTransport] Plugin Android instanciado correctamente.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NearbyTransport] Error al instanciar el plugin Android: {e.Message}");
            }
#else
            Debug.LogWarning("[NearbyTransport] Este transporte solo funciona en un dispositivo Android real. " +
                              "En el Editor, usá SimulatedTransport.");
#endif
        }

        // ------------------------------------------------------------------
        // Control de rol
        // ------------------------------------------------------------------

        /// <summary>Arranca como anfitrión: advertising, espera estaciones.</summary>
        public void StartHost(string hostName)
        {
            CallPlugin("startHost", _receiverGameObjectName, hostName);
        }

        /// <summary>Arranca como estación: discovery, se conecta al host que encuentre.</summary>
        public void StartStation(string stationName)
        {
            CallPlugin("startStation", _receiverGameObjectName, stationName);
        }

        /// <summary>Corta advertising, discovery y todas las conexiones.</summary>
        public void StopAll()
        {
            CallPlugin("stopAll");
        }

        /// <summary>(Solo host) Acepta la solicitud de una estación pendiente.</summary>
        public void AcceptStation(string endpointId)
        {
            CallPlugin("acceptConnection", endpointId);
        }

        /// <summary>(Solo host) Rechaza la solicitud de una estación pendiente.</summary>
        public void RejectStation(string endpointId)
        {
            CallPlugin("rejectConnection", endpointId);
        }

        /// <summary>Prueba de puente de la Tarea 4 (se mantiene para diagnóstico).</summary>
        public void SendTestPing(string message)
        {
            CallPlugin("ping", _receiverGameObjectName, message);
        }

        // ------------------------------------------------------------------
        // ILocalTransport
        // ------------------------------------------------------------------

        public void SendToStation(string stationId, string payload)
        {
            CallPlugin("sendToEndpoint", stationId, payload);
        }

        public void Broadcast(string payload)
        {
            CallPlugin("broadcast", payload);
        }

        // ------------------------------------------------------------------
        // Entrada desde Kotlin
        // ------------------------------------------------------------------

        /// <summary>
        /// Llamado por NearbyMessageReceiver cuando llega un mensaje desde Kotlin.
        /// Parsea el protocolo y dispara el evento correspondiente.
        /// </summary>
        public void ReceiveRawMessageFromPlugin(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            // Compatibilidad con el ping de prueba de la Tarea 4.
            if (raw.StartsWith("PONG:"))
            {
                OnMessageReceived?.Invoke("plugin-test", raw);
                return;
            }

            // Formato: TIPO|resto (el payload de MSG puede contener '|', por eso
            // se limita la cantidad de cortes).
            string[] parts = raw.Split(new[] { '|' }, 3);
            switch (parts[0])
            {
                case "CONNECTION_REQUEST" when parts.Length >= 3:
                    OnConnectionRequest?.Invoke(parts[1], parts[2]);
                    break;

                case "CONNECTED" when parts.Length >= 2:
                    OnStationConnected?.Invoke(parts[1]);
                    break;

                case "DISCONNECTED" when parts.Length >= 2:
                    OnStationDisconnected?.Invoke(parts[1]);
                    break;

                case "MSG" when parts.Length >= 3:
                    OnMessageReceived?.Invoke(parts[1], parts[2]);
                    break;

                case "STATUS" when parts.Length >= 2:
                    // parts puede haberse cortado en 3: rearmar el texto completo.
                    string status = parts.Length == 3 ? $"{parts[1]}|{parts[2]}" : parts[1];
                    Debug.Log($"[NearbyTransport] STATUS: {status}");
                    OnStatus?.Invoke(status);
                    break;

                default:
                    Debug.LogWarning($"[NearbyTransport] Mensaje no reconocido del plugin: {raw}");
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void CallPlugin(string method, params object[] args)
        {
            if (_pluginInstance == null)
            {
                Debug.LogWarning($"[NearbyTransport] '{method}' ignorado: no hay instancia de plugin (¿estás en el Editor?).");
                return;
            }

            try
            {
                _pluginInstance.Call(method, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NearbyTransport] Error llamando '{method}': {e.Message}");
            }
        }
    }
}
