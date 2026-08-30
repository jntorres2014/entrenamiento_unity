using Entrenamiento.Presentation;
using Entrenamiento.Transport;
using UnityEngine;
using UnityEngine.UI;

namespace Entrenamiento.App
{
    /// <summary>
    /// Bootstrap de la Tarea 4: prueba end-to-end del puente Unity-Kotlin.
    /// Al tocar un botón en pantalla, se llama al plugin Android real (ping),
    /// y cuando responde vía UnitySendMessage se muestra el resultado.
    /// Solo tiene sentido correrlo en un dispositivo Android real (build),
    /// no en el Editor.
    /// </summary>
    public class NearbyBridgeTestBootstrap : MonoBehaviour
    {
        [SerializeField] private Button pingButton;
        [SerializeField] private ResultLabel statusLabel;
        [SerializeField] private NearbyMessageReceiver messageReceiver;

        private NearbyTransport _transport;

        private void Start()
        {
            if (pingButton == null || statusLabel == null || messageReceiver == null)
            {
                Debug.LogError("[NearbyBridgeTestBootstrap] Faltan referencias en el Inspector.");
                return;
            }

            _transport = new NearbyTransport(gameObject.name);
            messageReceiver.SetTransport(_transport);

            _transport.OnMessageReceived += HandlePluginMessage;
            pingButton.onClick.AddListener(HandlePingButtonClicked);
        }

        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.OnMessageReceived -= HandlePluginMessage;
            }

            if (pingButton != null)
            {
                pingButton.onClick.RemoveListener(HandlePingButtonClicked);
            }
        }

        private void HandlePingButtonClicked()
        {
            Debug.Log("[NearbyBridgeTestBootstrap] Enviando ping al plugin...");
            statusLabel.SetText("Enviando ping...");
            _transport.SendTestPing("hola-desde-unity");
        }

        private void HandlePluginMessage(string stationId, string payload)
        {
            Debug.Log($"[NearbyBridgeTestBootstrap] Respuesta recibida: {payload}");
            statusLabel.SetText($"Respuesta del plugin: {payload}");
        }
    }
}
