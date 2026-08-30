using Entrenamiento.Transport;
using UnityEngine;

namespace Entrenamiento.App
{
    /// <summary>
    /// Puente de entrada: este es el componente cuyo nombre de GameObject se le
    /// pasa a NearbyTransport, para que Kotlin (vía UnitySendMessage) le pueda
    /// llegar un mensaje a Unity. El nombre del método OnNearbyPluginMessage
    /// tiene que coincidir EXACTO con el que usa NearbyConnectionsPlugin.kt en
    /// UnityPlayer.UnitySendMessage(gameObjectName, "OnNearbyPluginMessage", ...).
    /// </summary>
    public class NearbyMessageReceiver : MonoBehaviour
    {
        private NearbyTransport _transport;

        public void SetTransport(NearbyTransport transport)
        {
            _transport = transport;
        }

        /// <summary>
        /// Unity llama este método automáticamente cuando Kotlin invoca
        /// UnitySendMessage con este mismo nombre de método. NO renombrar sin
        /// actualizar también NearbyConnectionsPlugin.kt.
        /// </summary>
        public void OnNearbyPluginMessage(string payload)
        {
            Debug.Log($"[NearbyMessageReceiver] Mensaje recibido desde el plugin: {payload}");
            _transport?.ReceiveRawMessageFromPlugin(payload);
        }
    }
}
