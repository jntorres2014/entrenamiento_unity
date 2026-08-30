using Entrenamiento.App;
using Entrenamiento.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Entrenamiento.EditorTools
{
    /// <summary>
    /// Agrega la pantalla de Ajustes (sonido, vibración, volver) a la
    /// escena TrainingNearby ya generada:
    ///  - botón secundario "AJUSTES" en el panel de rol,
    ///  - SettingsPanel (dentro de un SafeAreaFitter) con estilo del
    ///    design system, cableado a SettingsPanelController por
    ///    SerializedObject.
    /// Menú "Entrenamiento > Agregar Ajustes a TrainingNearby".
    /// Es re-ejecutable: si la pantalla ya existe, la regenera.
    /// </summary>
    public static class AddSettingsPanelToTrainingNearbyScene
    {
        private const string ScenePath = "Assets/Scenes/TrainingNearby.unity";
        private const string RoundedSpritePath = "Assets/UI/RoundedRect.png";

        [MenuItem("Entrenamiento/Agregar Ajustes a TrainingNearby")]
        public static void Add()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Falta la escena",
                    "Primero generá la escena con\n\"Entrenamiento > Crear escena TrainingNearby\".", "OK");
                return;
            }

            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (rounded == null)
            {
                EditorUtility.DisplayDialog("Faltan los sprites de UI",
                    "No está Assets/UI/RoundedRect.png. Corré primero\n" +
                    "\"Entrenamiento > Crear escena TrainingNearby\".", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGo = GameObject.Find("Canvas");
            Transform canvasT = canvasGo != null ? canvasGo.transform : null;
            Transform rolePanelT = canvasT != null ? canvasT.Find("RolePanel") : null;

            if (canvasT == null || rolePanelT == null)
            {
                EditorUtility.DisplayDialog("Escena inesperada",
                    "No encontré Canvas/RolePanel en la escena. Regenerala con\n" +
                    "\"Entrenamiento > Crear escena TrainingNearby\" y volvé a intentar.", "OK");
                return;
            }

            // ---------------- Limpieza (para poder re-ejecutar) ----------------
            DestroyIfExists(canvasT, "SettingsPanel");
            DestroyIfExists(rolePanelT, "SettingsButton");
            var oldController = GameObject.Find("SettingsController");
            if (oldController != null)
            {
                Object.DestroyImmediate(oldController);
            }

            // ---------------- Botón "AJUSTES" en el panel de rol ----------------
            // Secundario (Neutral): la acción principal del panel sigue siendo elegir rol.
            var openButton = CreateButton(rolePanelT, "SettingsButton", "AJUSTES",
                UiTheme.Neutral, rounded, 0.10f, 0.15f, 0.90f, 0.235f, 44);

            // ---------------- Panel: Ajustes ----------------
            var panel = CreatePanel(canvasT, "SettingsPanel");

            // Debe renderizar junto a los demás paneles, debajo de
            // DebugLabel / ColorView / Overlay.
            var summary = canvasT.Find("SummaryPanel");
            if (summary != null)
            {
                panel.transform.SetSiblingIndex(summary.GetSiblingIndex() + 1);
            }

            // Contenido interactivo dentro del safe area (notch / barra de gestos).
            var safeGo = new GameObject("SafeArea");
            safeGo.transform.SetParent(panel.transform, false);
            Stretch(safeGo.AddComponent<RectTransform>());
            safeGo.AddComponent<SafeAreaFitter>();
            Transform content = safeGo.transform;

            var title = CreateLabel(content, "Title", "AJUSTES", 66,
                0.05f, 0.905f, 0.95f, 0.965f, UiTheme.Accent);
            title.fontStyle = FontStyles.Bold;
            CreateLabel(content, "Subtitle", "Sonido y vibración del teléfono", 40,
                0.05f, 0.845f, 0.95f, 0.895f, UiTheme.TextSecondary);

            // Toggles con el mismo patrón que la config del host ("Modo: ...").
            var soundButton = CreateButton(content, "SoundButton", "Sonido: SÍ",
                UiTheme.Neutral, rounded, 0.05f, 0.725f, 0.95f, 0.795f, 44);
            var vibrationButton = CreateButton(content, "VibrationButton", "Vibración: SÍ",
                UiTheme.Neutral, rounded, 0.05f, 0.645f, 0.95f, 0.715f, 44);

            var hintLabel = CreateLabel(content, "HintLabel", "Los cambios se guardan solos.", 36,
                0.05f, 0.555f, 0.95f, 0.625f, UiTheme.TextSecondary);

            // Acción principal de la pantalla: volver (único botón Accent).
            var backButton = CreateButton(content, "BackButton", "VOLVER",
                UiTheme.Accent, rounded, 0.10f, 0.055f, 0.90f, 0.165f);

            panel.SetActive(false);

            ApplyBrandFonts(panel, openButton.gameObject, title);

            // ---------------- Controller + cableado ----------------
            var controllerGo = new GameObject("SettingsController");
            var controller = controllerGo.AddComponent<SettingsPanelController>();

            var so = new SerializedObject(controller);
            so.FindProperty("settingsPanel").objectReferenceValue = panel;
            so.FindProperty("rolePanel").objectReferenceValue = rolePanelT.gameObject;
            so.FindProperty("openSettingsButton").objectReferenceValue = openButton;
            so.FindProperty("soundButton").objectReferenceValue = soundButton;
            so.FindProperty("vibrationButton").objectReferenceValue = vibrationButton;
            so.FindProperty("backButton").objectReferenceValue = backButton;
            // includeInactive=true: el panel arranca desactivado.
            so.FindProperty("soundLabel").objectReferenceValue =
                soundButton.GetComponentInChildren<TextMeshProUGUI>(true);
            so.FindProperty("vibrationLabel").objectReferenceValue =
                vibrationButton.GetComponentInChildren<TextMeshProUGUI>(true);
            so.FindProperty("hintLabel").objectReferenceValue = hintLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[AddSettingsPanel] Pantalla de Ajustes agregada a " + ScenePath);
            EditorUtility.DisplayDialog("Listo",
                "Pantalla de Ajustes agregada a la escena TrainingNearby.\n" +
                "Se abre con el botón AJUSTES del panel de rol.", "OK");
        }

        // ------------------------------------------------------------------
        // Tipografía de marca (opcional: no falla si aún no está instalada)
        // ------------------------------------------------------------------

        private static void ApplyBrandFonts(GameObject panel, GameObject openButton, TMP_Text title)
        {
            var titleFont = LoadFont(
                "Assets/UI/Fonts/ArchivoBlack-Regular SDF.asset",
                "Assets/UI/Fonts/Archivo Black SDF.asset");
            var bodyFont = LoadFont(
                "Assets/UI/Fonts/Barlow-Regular SDF.asset",
                "Assets/UI/Fonts/Barlow SDF.asset");

            if (bodyFont != null)
            {
                foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.font = bodyFont;
                }

                foreach (var label in openButton.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.font = bodyFont;
                }
            }

            if (titleFont != null)
            {
                title.font = titleFont;
            }

            if (titleFont == null || bodyFont == null)
            {
                Debug.Log("[AddSettingsPanel] Fuentes de marca aún no instaladas en " +
                          "Assets/UI/Fonts: queda la fuente TMP por defecto.");
            }
        }

        private static TMP_FontAsset LoadFont(params string[] candidatePaths)
        {
            foreach (string path in candidatePaths)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null)
                {
                    return font;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // Helpers de UI (mismo estilo que CreateTrainingNearbyScene)
        // ------------------------------------------------------------------

        private static void DestroyIfExists(Transform parent, string childName)
        {
            var child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

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
    }
}
