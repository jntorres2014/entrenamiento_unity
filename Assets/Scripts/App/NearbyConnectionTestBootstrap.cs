using Entrenamiento.Presentation;
using Entrenamiento.Transport;
using UnityEngine;
using UnityEngine.UI;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de la Tarea 5: prueba de Nearby Connections REAL entre dos
    /// teléfonos. En un teléfono se toca "Host", en el otro "Estación"; cuando
    /// se conectan, el botón "Enviar" manda un broadcast con timestamp y el
    /// otro lado lo muestra en pantalla.
    ///
    /// Requiere: dispositivo Android real, Bluetooth y Ubicación/Wi-Fi activos,
    /// y el AAR actualizado del plugin en Assets/Plugins/Android.
    /// </summary>
    public class NearbyConnectionTestBootstrap : MonoBehaviour
    {
        [SerializeField] private Button hostButton;
        [SerializeField] private Button stationButton;
        [SerializeField] private Button sendButton;
        [SerializeField] private ResultLabel statusLabel;
        [SerializeField] private NearbyMessageReceiver messageReceiver;

        private NearbyTransport _transport;
        private int _sentCount;

        private void Start()
        {
            if (hostButton == null || stationButton == null || sendButton == null ||
                statusLabel == null || messageReceiver == null)
            {
                Debug.LogError("[NearbyConnectionTestBootstrap] Faltan referencias en el Inspector.");
                return;
            }

            NearbyPermissions.RequestAll();

            _transport = new NearbyTransport(gameObject.name);
            messageReceiver.SetTransport(_transport);

            _transport.OnStationConnected += HandleConnected;
            _transport.OnStationDisconnected += HandleDisconnected;
            _transport.OnMessageReceived += HandleMessage;
            _transport.OnStatus += HandleStatus;

            hostButton.onClick.AddListener(HandleHostClicked);
            stationButton.onClick.AddListener(HandleStationClicked);
            sendButton.onClick.AddListener(HandleSendClicked);

            statusLabel.SetText("Elegí rol: Host o Estación");
        }

        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.OnStationConnected -= HandleConnected;
                _transport.OnStationDisconnected -= HandleDisconnected;
                _transport.OnMessageReceived -= HandleMessage;
                _transport.OnStatus -= HandleStatus;
                _transport.StopAll();
            }
        }

        private void HandleHostClicked()
        {
            statusLabel.SetText("Iniciando HOST (advertising)...");
            _transport.StartHost(SystemInfo.deviceName);
        }

        private void HandleStationClicked()
        {
            statusLabel.SetText("Iniciando ESTACIÓN (discovery)...");
            _transport.StartStation(SystemInfo.deviceName);
        }

        private void HandleSendClicked()
        {
            _sentCount++;
            string payload = $"hola #{_sentCount} t={Time.realtimeSinceStartup:F1}s";
            _transport.Broadcast(payload);
            statusLabel.SetText($"Enviado: {payload}");
        }

        private void HandleConnected(string stationId)
        {
            statusLabel.SetText($"CONECTADO: {stationId}");
        }

        private void HandleDisconnected(string stationId)
        {
            statusLabel.SetText($"DESCONECTADO: {stationId}");
        }

        private void HandleMessage(string stationId, string payload)
        {
            statusLabel.SetText($"Recibido de {stationId}: {payload}");
        }

        private void HandleStatus(string status)
        {
            statusLabel.SetText($"[{status}]");
        }
    }
}
