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
    /// Genera la pantalla de historial de sesiones con el estilo "deportivo
    /// oscuro" del proyecto: menú "Entrenamiento > Crear escena SessionHistory".
    ///
    /// Jerarquía: fondo con gradiente (borde a borde) + SafeArea
    /// (SafeAreaFitter) > HistoryPanel (PanelFadeIn) con título, lista
    /// scrolleable de cards (una por sesión), estado vacío y botón VOLVER
    /// (única acción principal). Todo se cablea a SessionHistoryBootstrap por
    /// SerializedObject; se regenera sin pasos manuales.
    /// </summary>
    public static class CreateSessionHistoryScene
    {
        private const string ScenePath = "Assets/Scenes/SessionHistory.unity";

        [MenuItem("Entrenamiento/Crear escena SessionHistory")]
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
            TMP_FontAsset titleFont = TryLoadFont(
                "Assets/UI/Fonts/ArchivoBlack-Regular SDF.asset",
                "Assets/UI/Fonts/Archivo Black SDF.asset");
            TMP_FontAsset bodyFont = TryLoadFont(
                "Assets/UI/Fonts/Barlow-Regular SDF.asset",
                "Assets/UI/Fonts/Barlow SDF.asset");

            if (titleFont == null || bodyFont == null)
            {
                Debug.Log("[CreateSessionHistoryScene] Fuentes de marca no encontradas en " +
                    "Assets/UI/Fonts; se usa la fuente TMP por defecto (ver design-system.md).");
            }

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

            // Fondo con gradiente (fuera del safe area: llega a los bordes físicos)
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.sprite = gradient;
            bgImage.type = Image.Type.Simple;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;
            Stretch(bg.GetComponent<RectTransform>());

            // Contenedor con SafeAreaFitter: todo lo interactivo cuelga de acá
            var safeGo = new GameObject("SafeArea");
            safeGo.transform.SetParent(canvasGo.transform, false);
            Stretch(safeGo.AddComponent<RectTransform>());
            safeGo.AddComponent<SafeAreaFitter>();

            // ---------------- Panel: Historial ----------------
            var historyPanel = CreatePanel(safeGo.transform, "HistoryPanel");

            var title = CreateLabel(historyPanel.transform, "Title", "HISTORIAL", 72,
                0.05f, 0.905f, 0.95f, 0.965f, UiTheme.Accent);
            title.fontStyle = FontStyles.Bold;
            CreateLabel(historyPanel.transform, "Subtitle", "TUS ÚLTIMAS SESIONES", 40,
                0.05f, 0.855f, 0.95f, 0.90f, UiTheme.TextSecondary);

            // Lista scrolleable
            var (listRoot, listContent) = CreateScrollList(historyPanel.transform, "SessionList",
                0.05f, 0.19f, 0.95f, 0.845f);
            var rowView = CreateRowTemplate(listContent, rounded);

            // Estado vacío
            var emptyState = new GameObject("EmptyState");
            emptyState.transform.SetParent(historyPanel.transform, false);
            emptyState.AddComponent<RectTransform>();
            SetAnchors(emptyState, 0.05f, 0.42f, 0.95f, 0.68f);
            CreateCard(emptyState.transform, "Card", rounded, 0f, 0f, 1f, 1f);
            var emptyTitle = CreateLabel(emptyState.transform, "EmptyTitle",
                "TODAVÍA NO HAY SESIONES", 46, 0.06f, 0.54f, 0.94f, 0.88f, UiTheme.TextPrimary);
            emptyTitle.fontStyle = FontStyles.Bold;
            CreateLabel(emptyState.transform, "EmptyHint",
                "Arrancá una sesión de entrenamiento\ny va a aparecer acá.", 40,
                0.06f, 0.12f, 0.94f, 0.50f, UiTheme.TextSecondary);
            emptyState.SetActive(false);

            // Acción principal (único botón Accent de la pantalla)
            var backButton = CreateButton(historyPanel.transform, "BackButton", "VOLVER",
                UiTheme.Accent, rounded, 0.10f, 0.055f, 0.90f, 0.165f);

            // ---------------- Bootstrap ----------------
            var bootstrapGo = new GameObject("SessionHistoryBootstrap");
            var bootstrap = bootstrapGo.AddComponent<SessionHistoryBootstrap>();

            var so = new SerializedObject(bootstrap);
            so.FindProperty("listRoot").objectReferenceValue = listRoot;
            so.FindProperty("listContent").objectReferenceValue = listContent;
            so.FindProperty("rowTemplate").objectReferenceValue = rowView;
            so.FindProperty("emptyState").objectReferenceValue = emptyState;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Fuentes de marca (si ya están instaladas)
            ApplyFonts(canvasGo, title, titleFont, bodyFont);

            EnsureScenesFolder();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);

            Debug.Log($"[CreateSessionHistoryScene] Escena creada en {ScenePath}");
            EditorUtility.DisplayDialog("Listo",
                $"Escena creada: {ScenePath}\nYa quedó agregada a Build Settings.", "OK");
        }

        // ------------------------------------------------------------------
        // Lista scrolleable + fila
        // ------------------------------------------------------------------

        private static (GameObject root, RectTransform content) CreateScrollList(
            Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            SetAnchors(root, xMin, yMin, xMax, yMax);
            var scroll = root.AddComponent<ScrollRect>();

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(root.transform, false);
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.clear; // solo para recibir el drag
            viewportGo.AddComponent<RectMask2D>();
            Stretch(viewportGo.GetComponent<RectTransform>());

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 24f;
            layout.padding = new RectOffset(0, 0, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            return (root, content);
        }

        private static SessionHistoryRowView CreateRowTemplate(RectTransform content, Sprite rounded)
        {
            var row = new GameObject("RowTemplate");
            row.transform.SetParent(content, false);

            var cardImage = row.AddComponent<Image>();
            cardImage.sprite = rounded;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = UiTheme.Card;
            cardImage.raycastTarget = false;
            AddShadow(row);

            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.minHeight = 200f;
            layoutElement.preferredHeight = 200f;

            var modeLabel = CreateLabel(row.transform, "ModeLabel", "CLÁSICO", 44,
                0.05f, 0.52f, 0.55f, 0.92f, UiTheme.TextPrimary);
            modeLabel.fontStyle = FontStyles.Bold;
            modeLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var dateLabel = CreateLabel(row.transform, "DateLabel", "HOY 18:30", 36,
                0.55f, 0.54f, 0.95f, 0.90f, UiTheme.TextSecondary);
            dateLabel.alignment = TextAlignmentOptions.MidlineRight;

            var scoreLabel = CreateLabel(row.transform, "ScoreLabel", "0 aciertos · 0 errores", 40,
                0.05f, 0.10f, 0.62f, 0.48f, UiTheme.TextPrimary);
            scoreLabel.alignment = TextAlignmentOptions.MidlineLeft;
            scoreLabel.richText = true;

            var averageLabel = CreateLabel(row.transform, "AverageLabel", "PROM 0,00 s", 40,
                0.58f, 0.10f, 0.95f, 0.48f, UiTheme.AccentLime);
            averageLabel.alignment = TextAlignmentOptions.MidlineRight;

            var view = row.AddComponent<SessionHistoryRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("modeLabel").objectReferenceValue = modeLabel;
            so.FindProperty("dateLabel").objectReferenceValue = dateLabel;
            so.FindProperty("scoreLabel").objectReferenceValue = scoreLabel;
            so.FindProperty("averageLabel").objectReferenceValue = averageLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            row.SetActive(false); // plantilla: el bootstrap la clona y activa
            return view;
        }

        // ------------------------------------------------------------------
        // Fuentes (opcionales hasta que se instalen los assets TMP)
        // ------------------------------------------------------------------

        private static TMP_FontAsset TryLoadFont(params string[] paths)
        {
            foreach (string path in paths)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null)
                {
                    return font;
                }
            }

            return null;
        }

        private static void ApplyFonts(GameObject canvasGo, TMP_Text titleLabel,
            TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            if (bodyFont != null)
            {
                foreach (var label in canvasGo.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.font = bodyFont;
                }
            }

            if (titleFont != null)
            {
                titleLabel.font = titleFont;
            }
        }

        // ------------------------------------------------------------------
        // Sprites autogenerados (Assets/UI) — mismo patrón que TrainingNearby
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
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(36, 36, 36, 36);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureUiFolder()
        {
            if (!System.IO.Directory.Exists("Assets/UI"))
            {
                System.IO.Directory.CreateDirectory("Assets/UI");
            }
        }

        private static void EnsureScenesFolder()
        {
            if (!System.IO.Directory.Exists("Assets/Scenes"))
            {
                System.IO.Directory.CreateDirectory("Assets/Scenes");
            }
        }

        // ------------------------------------------------------------------
        // Helpers de UI (mismo estilo que CreateTrainingNearbyScene)
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
