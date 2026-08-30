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
    /// Genera la escena de la sesión de entrenamiento con estilo "deportivo
    /// oscuro" suavizado (gradiente, sombras, bordes redondeados):
    /// menú "Entrenamiento > Crear escena TrainingNearby".
    /// Crea los sprites necesarios en Assets/UI la primera vez.
    /// </summary>
    public static class CreateTrainingNearbyScene
    {
        private const string ScenePath = "Assets/Scenes/TrainingNearby.unity";

        [MenuItem("Entrenamiento/Crear escena TrainingNearby")]
        public static void Create()
        {
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Escena ya existe",
                    $"{ScenePath} ya existe. ¿Sobrescribir?", "Sobrescribir", "Cancelar"))
            {
                return;
            }

            Sprite rounded = EnsureRoundedSprite();
            Sprite gradient = EnsureGradientSprite();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Cámara + EventSystem
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.Background;
            camGo.tag = "MainCamera";

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo con gradiente suave
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.sprite = gradient;
            bgImage.type = Image.Type.Simple;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;
            Stretch(bg.GetComponent<RectTransform>());

            // ---------------- Panel: Rol ----------------
            var rolePanel = CreatePanel(canvasGo.transform, "RolePanel");
            var title = CreateLabel(rolePanel.transform, "Title", "REFLEX<color=#A8D84F>POD</color>", 104,
                0.05f, 0.72f, 0.95f, 0.95f, UiTheme.Accent);
            title.fontStyle = FontStyles.Bold;
            CreateLabel(rolePanel.transform, "Subtitle", "REACCIÓN · VELOCIDAD · FOCO", 40,
                0.05f, 0.66f, 0.95f, 0.71f, UiTheme.TextSecondary);
            var hostRoleButton = CreateButton(rolePanel.transform, "HostRoleButton", "SOY EL HOST",
                UiTheme.Positive, rounded, 0.10f, 0.46f, 0.90f, 0.58f);
            var stationRoleButton = CreateButton(rolePanel.transform, "StationRoleButton", "SOY ESTACIÓN",
                UiTheme.Info, rounded, 0.10f, 0.30f, 0.90f, 0.42f);

            // ---------------- Panel: Config host ----------------
            var configPanel = CreatePanel(canvasGo.transform, "HostConfigPanel");
            var configTitle = CreateLabel(configPanel.transform, "Title", "CONFIGURACIÓN", 66,
                0.05f, 0.905f, 0.95f, 0.965f, UiTheme.Accent);
            configTitle.fontStyle = FontStyles.Bold;

            CreateCard(configPanel.transform, "RoundsCard", rounded, 0.05f, 0.815f, 0.95f, 0.895f);
            var roundsValueLabel = CreateLabel(configPanel.transform, "RoundsValue", "Rondas: 10", 50,
                0.08f, 0.825f, 0.50f, 0.885f, UiTheme.TextPrimary);
            var roundsMinus = CreateButton(configPanel.transform, "RoundsMinus", "−",
                UiTheme.Neutral, rounded, 0.53f, 0.822f, 0.71f, 0.888f);
            var roundsPlus = CreateButton(configPanel.transform, "RoundsPlus", "+",
                UiTheme.Neutral, rounded, 0.74f, 0.822f, 0.92f, 0.888f);

            var modeButton = CreateButton(configPanel.transform, "ModeButton", "Modo: CLÁSICO",
                UiTheme.Neutral, rounded, 0.05f, 0.725f, 0.95f, 0.795f, 44);
            var timeoutButton = CreateButton(configPanel.transform, "TimeoutButton",
                "Límite por ronda: SIN LÍMITE",
                UiTheme.Neutral, rounded, 0.05f, 0.645f, 0.95f, 0.715f, 44);
            var colorModeButton = CreateButton(configPanel.transform, "ColorModeButton",
                "Colores: VARIADOS",
                UiTheme.Neutral, rounded, 0.05f, 0.565f, 0.95f, 0.635f, 44);
            var participateButton = CreateButton(configPanel.transform, "ParticipateButton",
                "Participo como estación: SÍ",
                UiTheme.Info, rounded, 0.05f, 0.485f, 0.95f, 0.555f, 44);

            CreateCard(configPanel.transform, "ConnectedCard", rounded, 0.05f, 0.345f, 0.95f, 0.465f);
            var connectedLabel = CreateLabel(configPanel.transform, "ConnectedLabel",
                "Esperando estaciones...", 42, 0.08f, 0.355f, 0.92f, 0.455f, UiTheme.TextSecondary);

            // Tarjeta de solicitud de conexión (aceptar / rechazar)
            var requestCard = new GameObject("RequestCard");
            requestCard.transform.SetParent(configPanel.transform, false);
            var requestImage = requestCard.AddComponent<Image>();
            requestImage.sprite = rounded;
            requestImage.type = Image.Type.Sliced;
            requestImage.color = new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.18f);
            requestImage.raycastTarget = false;
            SetAnchors(requestCard, 0.05f, 0.185f, 0.95f, 0.325f);
            AddShadow(requestCard);
            var requestLabel = CreateLabel(requestCard.transform, "RequestLabel",
                "\"...\" quiere unirse", 42, 0.05f, 0.55f, 0.95f, 0.95f, UiTheme.TextPrimary);
            var acceptButton = CreateButton(requestCard.transform, "AcceptButton", "ACEPTAR",
                UiTheme.Positive, rounded, 0.06f, 0.08f, 0.48f, 0.50f, 40);
            var rejectButton = CreateButton(requestCard.transform, "RejectButton", "RECHAZAR",
                UiTheme.Danger, rounded, 0.52f, 0.08f, 0.94f, 0.50f, 40);
            requestCard.SetActive(false);

            var startButton = CreateButton(configPanel.transform, "StartButton", "ARRANCAR SESIÓN",
                UiTheme.Accent, rounded, 0.10f, 0.055f, 0.90f, 0.165f);

            // ---------------- Panel: Progreso host ----------------
            var progressPanel = CreatePanel(canvasGo.transform, "HostProgressPanel");
            var progressTitle = CreateLabel(progressPanel.transform, "Title", "SESIÓN EN CURSO", 60,
                0.05f, 0.90f, 0.95f, 0.97f, UiTheme.AccentLime);
            progressTitle.fontStyle = FontStyles.Bold;
            var progressLabel = CreateLabel(progressPanel.transform, "ProgressLabel",
                "Arrancando...", 54, 0.05f, 0.30f, 0.95f, 0.88f, UiTheme.TextPrimary);

            // ---------------- Panel: Espera estación ----------------
            var waitPanel = CreatePanel(canvasGo.transform, "StationWaitPanel");
            var waitTitle = CreateLabel(waitPanel.transform, "Title", "ESTACIÓN", 72,
                0.05f, 0.88f, 0.95f, 0.96f, UiTheme.Info);
            waitTitle.fontStyle = FontStyles.Bold;
            var stationStatusLabel = CreateLabel(waitPanel.transform, "StationStatusLabel",
                "Buscando al host...", 56, 0.05f, 0.35f, 0.95f, 0.75f, UiTheme.TextPrimary);

            // ---------------- Panel: Resumen ----------------
            var summaryPanel = CreatePanel(canvasGo.transform, "SummaryPanel");
            var summaryTitle = CreateLabel(summaryPanel.transform, "Title", "RESULTADOS", 72,
                0.05f, 0.90f, 0.95f, 0.97f, UiTheme.Accent);
            summaryTitle.fontStyle = FontStyles.Bold;
            var summaryLabel = CreateLabel(summaryPanel.transform, "SummaryLabel", "", 42,
                0.05f, 0.24f, 0.95f, 0.88f, UiTheme.TextPrimary);
            var restartButton = CreateButton(summaryPanel.transform, "RestartButton", "NUEVA SESIÓN",
                UiTheme.Positive, rounded, 0.10f, 0.08f, 0.90f, 0.20f);

            // ---------------- Diagnóstico ----------------
            var debugLabel = CreateLabel(canvasGo.transform, "DebugLabel", "", 32,
                0.02f, 0.00f, 0.98f, 0.05f, UiTheme.TextSecondary);

            // ---------------- Pantalla de color (tapa todo) ----------------
            var colorGo = new GameObject("ColorView");
            colorGo.transform.SetParent(canvasGo.transform, false);
            var colorImage = colorGo.AddComponent<Image>();
            colorImage.color = Color.black;
            Stretch(colorGo.GetComponent<RectTransform>());
            var colorView = colorGo.AddComponent<StationView>();

            var hint = CreateLabel(colorGo.transform, "TapHint", "¡TOCÁ!", 130,
                0.10f, 0.40f, 0.90f, 0.60f, Color.white);
            hint.fontStyle = FontStyles.Bold;
            hint.raycastTarget = false;
            hint.gameObject.AddComponent<PulseScale>();

            colorGo.SetActive(false);

            // ---------------- Overlay (cuenta regresiva / tiempo / feedback) ----------------
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(canvasGo.transform, false);
            var overlayLabel = overlayGo.AddComponent<TextMeshProUGUI>();
            overlayLabel.text = "3";
            overlayLabel.fontSize = 220;
            overlayLabel.fontStyle = FontStyles.Bold;
            overlayLabel.alignment = TextAlignmentOptions.Center;
            overlayLabel.color = UiTheme.AccentLime;
            overlayLabel.raycastTarget = false;
            Stretch(overlayGo.GetComponent<RectTransform>());
            overlayGo.SetActive(false);

            // Estado inicial
            configPanel.SetActive(false);
            progressPanel.SetActive(false);
            waitPanel.SetActive(false);
            summaryPanel.SetActive(false);

            // ---------------- Bootstrap + receiver ----------------
            var bootstrapGo = new GameObject("TrainingBootstrap");
            var receiver = bootstrapGo.AddComponent<NearbyMessageReceiver>();
            var bootstrap = bootstrapGo.AddComponent<TrainingNearbyBootstrap>();

            var so = new SerializedObject(bootstrap);
            so.FindProperty("rolePanel").objectReferenceValue = rolePanel;
            so.FindProperty("hostConfigPanel").objectReferenceValue = configPanel;
            so.FindProperty("hostProgressPanel").objectReferenceValue = progressPanel;
            so.FindProperty("stationWaitPanel").objectReferenceValue = waitPanel;
            so.FindProperty("summaryPanel").objectReferenceValue = summaryPanel;
            so.FindProperty("hostRoleButton").objectReferenceValue = hostRoleButton;
            so.FindProperty("stationRoleButton").objectReferenceValue = stationRoleButton;
            so.FindProperty("roundsValueLabel").objectReferenceValue = roundsValueLabel;
            so.FindProperty("roundsMinusButton").objectReferenceValue = roundsMinus;
            so.FindProperty("roundsPlusButton").objectReferenceValue = roundsPlus;
            so.FindProperty("modeButton").objectReferenceValue = modeButton;
            so.FindProperty("timeoutButton").objectReferenceValue = timeoutButton;
            so.FindProperty("colorModeButton").objectReferenceValue = colorModeButton;
            so.FindProperty("participateButton").objectReferenceValue = participateButton;
            so.FindProperty("participateLabel").objectReferenceValue =
                participateButton.GetComponentInChildren<TextMeshProUGUI>(true);
            so.FindProperty("connectedLabel").objectReferenceValue = connectedLabel;
            so.FindProperty("startSessionButton").objectReferenceValue = startButton;
            so.FindProperty("requestCard").objectReferenceValue = requestCard;
            so.FindProperty("requestLabel").objectReferenceValue = requestLabel;
            so.FindProperty("acceptButton").objectReferenceValue = acceptButton;
            so.FindProperty("rejectButton").objectReferenceValue = rejectButton;
            so.FindProperty("progressLabel").objectReferenceValue = progressLabel;
            so.FindProperty("stationStatusLabel").objectReferenceValue = stationStatusLabel;
            so.FindProperty("debugLabel").objectReferenceValue = debugLabel;
            so.FindProperty("summaryLabel").objectReferenceValue = summaryLabel;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.FindProperty("colorView").objectReferenceValue = colorView;
            so.FindProperty("overlayLabel").objectReferenceValue = overlayLabel;
            so.FindProperty("messageReceiver").objectReferenceValue = receiver;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);

            Debug.Log($"[CreateTrainingNearbyScene] Escena creada en {ScenePath}");
            EditorUtility.DisplayDialog("Listo",
                $"Escena creada: {ScenePath}\nYa quedó agregada a Build Settings.", "OK");
        }

        // ------------------------------------------------------------------
        // Sprites autogenerados (Assets/UI)
        // ------------------------------------------------------------------

        private static Sprite EnsureRoundedSprite()
        {
            const string path = "Assets/UI/RoundedRect.png";
            EnsureUiFolder();

            if (!System.IO.File.Exists(path))
            {
                const int size = 96;
                const float radius = 34f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                        float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                        float alpha = Mathf.Clamp01(radius - d + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            // Clave: sin esto queda en modo "Multiple" sin recortes y el
            // sprite no existe (fondo blanco / botones cuadrados).
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(36, 36, 36, 36);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return LoadSpriteOrFail(path);
        }

        private static Sprite EnsureGradientSprite()
        {
            const string path = "Assets/UI/GradientBg.png";
            EnsureUiFolder();

            if (!System.IO.File.Exists(path))
            {
                const int h = 256;
                var top = new Color32(0x1B, 0x20, 0x29, 0xFF);
                var bottom = new Color32(0x0C, 0x0E, 0x12, 0xFF);
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);

                for (int y = 0; y < h; y++)
                {
                    Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
                    for (int x = 0; x < 4; x++)
                    {
                        tex.SetPixel(x, y, c);
                    }
                }

                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            return LoadSpriteOrFail(path);
        }

        /// <summary>
        /// Carga el sprite y falla RUIDOSAMENTE si no está listo: una escena
        /// generada con sprites null sale con fondo blanco y botones cuadrados
        /// sin ningún error visible (pasó en la primera generación).
        /// </summary>
        private static Sprite LoadSpriteOrFail(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"No se pudo cargar el sprite {path}.\n" +
                    "Volvé a ejecutar el menú: la segunda vez siempre funciona.", "OK");
                throw new System.InvalidOperationException($"Sprite no disponible: {path}");
            }

            return sprite;
        }

        private static void EnsureUiFolder()
        {
            if (!System.IO.Directory.Exists("Assets/UI"))
            {
                System.IO.Directory.CreateDirectory("Assets/UI");
                AssetDatabase.Refresh();
            }
        }

        // ------------------------------------------------------------------
        // Helpers de UI
        // ------------------------------------------------------------------

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddShadow(GameObject go)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -6f);
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            go.AddComponent<CanvasGroup>();
            go.AddComponent<PanelFadeIn>();
            return go;
        }

        private static void CreateCard(Transform parent, string name, Sprite rounded,
            float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = UiTheme.Card;
            image.raycastTarget = false;
            SetAnchors(go, xMin, yMin, xMax, yMax);
            AddShadow(go);
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            float fontSize, float xMin, float yMin, float xMax, float yMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            SetAnchors(go, xMin, yMin, xMax, yMax);
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Color color, Sprite rounded, float xMin, float yMin, float xMax, float yMax,
            float fontSize = 52)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.8f;
            colors.disabledColor = color * 0.4f;
            button.colors = colors;

            go.AddComponent<ButtonPressScale>();
            AddShadow(go);
            SetAnchors(go, xMin, yMin, xMax, yMax);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiTheme.TextPrimary;
            text.raycastTarget = false;
            Stretch(textGo.GetComponent<RectTransform>());

            return button;
        }

        private static void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
