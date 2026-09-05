using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Entrenamiento.Presentation
{
    /// <summary>
    /// Coordina la Home moderna una vez que todos los accesos runtime existen.
    /// El modo SOLO pasa a ser la acción principal; AR y Camera Training quedan
    /// como herramientas secundarias. También integra el logo de marca.
    /// </summary>
    public sealed class SoloHomeLayoutFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "TrainingNearby") return;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.isRootCanvas && canvas.GetComponent<SoloHomeLayoutFix>() == null)
                {
                    canvas.gameObject.AddComponent<SoloHomeLayoutFix>();
                    break;
                }
            }
        }

        private void Awake()
        {
            StartCoroutine(ApplyWhenReady());
        }

        private IEnumerator ApplyWhenReady()
        {
            GameObject solo = null;
            GameObject camera = null;
            GameObject ar = null;
            GameObject hero = null;

            for (int i = 0; i < 150; i++)
            {
                solo = FindDeep("SoloTrainingButton");
                camera = FindDeep("CameraTrainingButton");
                ar = FindDeep("ARTrainingButton");
                hero = FindDeep("ARHeroCard");
                if (solo != null && camera != null && ar != null && hero != null) break;
                yield return null;
            }

            if (solo == null || hero == null) yield break;

            var statusCard = FindDeep("ARStatusCard");
            if (statusCard != null) statusCard.SetActive(false);

            ApplyHeroCopy(hero);
            AddBrandLogo(hero);
            LayoutPrimarySolo(solo);
            LayoutUtilityButton(ar, 0.072f, 0.075f, 0.490f, 0.140f, "AR TRAINING", "Solo en equipos compatibles");
            LayoutUtilityButton(camera, 0.510f, 0.075f, 0.928f, 0.140f, "CALIBRAR CÁMARA", "Prueba libre del detector");

            RelabelRoleButton("HostRoleButton",
                "CON PODS\n<size=67%><color=#B7C0CC>Crear y dirigir una sesión</color></size>");
            RelabelRoleButton("StationRoleButton",
                "ESTACIÓN\n<size=67%><color=#B7C0CC>Usar este teléfono como pod</color></size>");
        }

        private void ApplyHeroCopy(GameObject hero)
        {
            var badge = FindText(hero, "HeroBadge");
            if (badge != null)
            {
                badge.text = "1 TELÉFONO  ·  SIN ARCORE";
                badge.color = UiTheme.Accent;
            }

            var title = FindText(hero, "HeroTitle");
            if (title != null)
            {
                title.text = "Tu espacio.\nTu entrenamiento.";
                title.fontSizeMax = 41f;
                title.fontSizeMin = 25f;
            }

            var subtitle = FindText(hero, "HeroSubtitle");
            if (subtitle != null)
            {
                subtitle.text = "Apoyá el teléfono, calibrá 5 zonas y entrená reacción, velocidad y decisión sin pods.";
                subtitle.fontSizeMax = 20f;
                subtitle.fontSizeMin = 15f;
                subtitle.color = UiTheme.TextSecondary;
            }

            var glow = FindChild(hero, "HeroGlow");
            if (glow != null) glow.SetActive(false);
            var target = FindChild(hero, "ARTarget");
            if (target != null) target.SetActive(false);
        }

        private void AddBrandLogo(GameObject hero)
        {
            if (FindChild(hero, "BrandLogoTile") != null) return;

            var tileGo = new GameObject("BrandLogoTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tileGo.transform.SetParent(hero.transform, false);
            var tile = tileGo.GetComponent<Image>();
            tile.color = Color.white;
            tile.raycastTarget = false;
            SetRect(tile.rectTransform, 0.705f, 0.575f, 0.925f, 0.885f);

            var logoGo = new GameObject("BrandLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            logoGo.transform.SetParent(tileGo.transform, false);
            var logoRect = logoGo.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.08f, 0.08f);
            logoRect.anchorMax = new Vector2(0.92f, 0.92f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;

            var logo = logoGo.GetComponent<RawImage>();
            logo.texture = BrandLogo.Texture;
            logo.color = Color.white;
            logo.raycastTarget = false;

            var fitter = logoGo.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            tileGo.transform.SetAsLastSibling();
        }

        private void LayoutPrimarySolo(GameObject solo)
        {
            var rect = solo.GetComponent<RectTransform>();
            if (rect != null) SetRect(rect, 0.10f, 0.535f, 0.90f, 0.615f);

            var image = solo.GetComponent<Image>();
            if (image != null) image.color = UiTheme.Accent;

            var label = solo.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "EMPEZAR ENTRENAMIENTO   →";
                label.alignment = TextAlignmentOptions.Center;
                label.fontSizeMax = 23f;
                label.fontSizeMin = 16f;
                label.color = new Color32(0x0B, 0x0F, 0x14, 0xFF);
                label.fontStyle = FontStyles.Bold;
                label.rectTransform.offsetMin = new Vector2(18f, 8f);
                label.rectTransform.offsetMax = new Vector2(-18f, -8f);
            }

            solo.transform.SetAsLastSibling();
        }

        private void LayoutUtilityButton(GameObject go, float xMin, float yMin, float xMax, float yMax,
            string title, string detail)
        {
            if (go == null) return;

            var rect = go.GetComponent<RectTransform>();
            if (rect != null) SetRect(rect, xMin, yMin, xMax, yMax);

            var image = go.GetComponent<Image>();
            if (image != null) image.color = UiTheme.Surface;

            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = title + "\n<size=63%><color=#B7C0CC>" + detail + "</color></size>";
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSizeMax = 17f;
                label.fontSizeMin = 11f;
                label.color = UiTheme.TextPrimary;
                label.rectTransform.offsetMin = new Vector2(18f, 6f);
                label.rectTransform.offsetMax = new Vector2(-12f, -6f);
            }

            go.transform.SetAsLastSibling();
        }

        private void RelabelRoleButton(string objectName, string value)
        {
            var go = FindDeep(objectName);
            if (go == null) return;
            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.text = value;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.fontSizeMax = 23f;
            label.fontSizeMin = 14f;
        }

        private static TMP_Text FindText(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == objectName) return text;
            }
            return null;
        }

        private static GameObject FindChild(GameObject root, string objectName)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private GameObject FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
            return null;
        }

        private static void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
