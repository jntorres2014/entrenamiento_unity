using Entrenamiento.App;
using Entrenamiento.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.EditorTools
{
    /// <summary>
    /// Crea la escena de prueba de la Tarea 5 (Nearby real) con un clic:
    /// menú "Entrenamiento > Crear escena NearbyConnectionTest".
    /// Genera Canvas + botones Host/Estación/Enviar + label de estado +
    /// bootstrap con todas las referencias ya asignadas, y la guarda en
    /// Assets/Scenes/NearbyConnectionTest.unity.
    /// </summary>
    public static class CreateNearbyTestScene
    {
        private const string ScenePath = "Assets/Scenes/NearbyConnectionTest.unity";

        [MenuItem("Entrenamiento/Crear escena NearbyConnectionTest")]
        public static void Create()
        {
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Escena ya existe",
                    $"{ScenePath} ya existe. ¿Sobrescribir?",
                    "Sobrescribir", "Cancelar"))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Cámara (para el fondo detrás del Canvas) ---
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
            camGo.tag = "MainCamera";

            // --- EventSystem (Input System nuevo) ---
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // --- Canvas ---
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // portrait
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // --- Label de estado (arriba) ---
            var labelGo = new GameObject("StatusLabel");
            labelGo.transform.SetParent(canvasGo.transform, false);
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 52;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.05f, 0.70f);
            labelRt.anchorMax = new Vector2(0.95f, 0.95f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var resultLabel = labelGo.AddComponent<ResultLabel>();

            // --- Botones ---
            Button hostButton = CreateButton(canvasGo.transform, "HostButton", "HOST",
                new Color(0.18f, 0.55f, 0.25f), 0.50f, 0.62f);
            Button stationButton = CreateButton(canvasGo.transform, "StationButton", "ESTACIÓN",
                new Color(0.15f, 0.35f, 0.65f), 0.36f, 0.48f);
            Button sendButton = CreateButton(canvasGo.transform, "SendButton", "ENVIAR",
                new Color(0.45f, 0.45f, 0.48f), 0.18f, 0.30f);

            // --- Bootstrap + receiver (mismo GameObject: el plugin manda
            //     UnitySendMessage al nombre de este GO) ---
            var bootstrapGo = new GameObject("NearbyBootstrap");
            var receiver = bootstrapGo.AddComponent<NearbyMessageReceiver>();
            var bootstrap = bootstrapGo.AddComponent<NearbyConnectionTestBootstrap>();

            var so = new SerializedObject(bootstrap);
            so.FindProperty("hostButton").objectReferenceValue = hostButton;
            so.FindProperty("stationButton").objectReferenceValue = stationButton;
            so.FindProperty("sendButton").objectReferenceValue = sendButton;
            so.FindProperty("statusLabel").objectReferenceValue = resultLabel;
            so.FindProperty("messageReceiver").objectReferenceValue = receiver;
            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Guardar y agregar a Build Settings ---
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);

            Debug.Log($"[CreateNearbyTestScene] Escena creada y guardada en {ScenePath}");
            EditorUtility.DisplayDialog("Listo",
                $"Escena creada: {ScenePath}\nYa quedó agregada a Build Settings.", "OK");
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Color color, float anchorYMin, float anchorYMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = color;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.10f, anchorYMin);
            rt.anchorMax = new Vector2(0.90f, anchorYMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 64;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return button;
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    return; // ya estaba
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
