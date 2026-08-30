# Testing

## En el Editor (sin red real)
- Escenas StationTest / MultiStationTest / TrainingTest usan SimulatedTransport:
  correr en el Editor y verificar en consola/UI el ciclo estación indicada ->
  toque simulado -> acierto con tiempo de reacción.

## Puente Unity↔Kotlin (Tarea 4)
- Build de la escena NearbyBridgeTest a un teléfono Android.
- Tocar el botón Ping: debe aparecer "PONG:hola-desde-unity".

## Nearby real (Tarea 5) — requiere DOS teléfonos
1. En Android Studio: Build > Make Module :nearbyplugin (variante release) y
   copiar nearbyplugin/build/outputs/aar/nearbyplugin-release.aar a
   Assets/Plugins/Android/ (reemplazar el existente).
2. En Unity: crear escena NearbyConnectionTest con Canvas, tres botones
   (Host / Estación / Enviar), un ResultLabel y un GameObject con
   NearbyMessageReceiver + NearbyConnectionTestBootstrap (asignar referencias).
3. Build a los dos teléfonos. Encender Bluetooth, Wi-Fi y Ubicación en ambos.
4. Aceptar los permisos que pide la app al abrir.

## Sesión de entrenamiento (Tarea 6)
En el Editor (sin celulares):
1. Menú Entrenamiento > Crear escena TrainingNearby (una sola vez).
2. Play -> SOY EL HOST. A los ~1s se conectan sim-1 y sim-2.
3. Configurar rondas / participación -> ARRANCAR SESIÓN.
4. Las estaciones simuladas "tocan" solas; si participás como estación, cuando
   te toque la pantalla se pinta de color: hacé clic en ella.
5. Al final debe aparecer el resumen con todas las rondas, promedio y mejor.

Con teléfonos: mismo APK en todos; uno elige SOY EL HOST y los demás
SOY ESTACIÓN. El resto igual que en Editor, pero los toques son reales.

## Casos a verificar
- Teléfono A toca Host -> label muestra [ADVERTISING_OK].
- Teléfono B toca Estación -> [DISCOVERY_OK], luego [ENDPOINT_FOUND|...],
  y al final "CONECTADO: <endpointId>" en ambos.
- Enviar desde A -> B muestra "Recibido de <id>: hola #N ...", y viceversa.
- Cerrar la app en un teléfono -> el otro muestra "DESCONECTADO: <id>".
- Si algo falla, mirar logcat con filtro "NearbyPlugin" o "[NearbyTransport]".
