#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;

namespace Entrenamiento.EditorTools
{
    /// <summary>
    /// Deja ARCore habilitado en XR Plug-in Management para Android.
    /// Se ejecuta automáticamente al importar los paquetes y también puede
    /// lanzarse manualmente desde Entrenamiento/AR/Configurar ARCore.
    /// </summary>
    [InitializeOnLoad]
    public static class ConfigureARCoreForAndroid
    {
        private const string ArCoreLoaderName = "UnityEngine.XR.ARCore.ARCoreLoader";

        static ConfigureARCoreForAndroid()
        {
            EditorApplication.delayCall += TryConfigure;
        }

        [MenuItem("Entrenamiento/AR/Configurar ARCore para Android")]
        public static void ConfigureFromMenu()
        {
            if (TryConfigure())
            {
                Debug.Log("[AR] ARCore quedó habilitado para Android.");
            }
            else
            {
                Debug.LogWarning("[AR] Todavía no se pudo habilitar ARCore. Esperá a que Package Manager termine de importar y volvé a ejecutar este menú.");
            }
        }

        private static bool TryConfigure()
        {
            try
            {
                var perBuildTarget = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
                if (perBuildTarget == null || perBuildTarget.AssignedSettings == null)
                {
                    return false;
                }

                bool assigned = XRPackageMetadataStore.AssignLoader(
                    perBuildTarget.AssignedSettings,
                    ArCoreLoaderName,
                    BuildTargetGroup.Android);

                if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
                {
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                }

                AssetDatabase.SaveAssets();
                return assigned || IsAlreadyAssigned(perBuildTarget.AssignedSettings);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AR] Configuración automática pendiente: {ex.Message}");
                return false;
            }
        }

        private static bool IsAlreadyAssigned(UnityEngine.XR.Management.XRManagerSettings manager)
        {
            if (manager == null) return false;

            foreach (var loader in manager.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == ArCoreLoaderName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
