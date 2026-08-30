using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Entrenamiento.App
{
    /// <summary>
    /// Helper para pedir en runtime los permisos que Nearby Connections necesita.
    /// Los permisos ya están declarados en el AndroidManifest del plugin (AAR);
    /// acá solo se piden los "peligrosos" que Android exige confirmar en runtime.
    /// Llamar RequestAll() antes de StartHost/StartStation.
    /// </summary>
    public static class NearbyPermissions
    {
        public static void RequestAll()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var wanted = new List<string>
            {
                // Android 12+ (en versiones anteriores el sistema los ignora)
                "android.permission.BLUETOOTH_SCAN",
                "android.permission.BLUETOOTH_ADVERTISE",
                "android.permission.BLUETOOTH_CONNECT",
                // Android 13+
                "android.permission.NEARBY_WIFI_DEVICES",
                // Android 12 o menor
                Permission.FineLocation,
            };

            var missing = new List<string>();
            foreach (string p in wanted)
            {
                if (!Permission.HasUserAuthorizedPermission(p))
                {
                    missing.Add(p);
                }
            }

            if (missing.Count > 0)
            {
                Debug.Log($"[NearbyPermissions] Pidiendo permisos: {string.Join(", ", missing)}");
                Permission.RequestUserPermissions(missing.ToArray());
            }
#else
            Debug.Log("[NearbyPermissions] Solo aplica en dispositivo Android real.");
#endif
        }
    }
}
