# Changelog

## [Sin versión] - En progreso
### Agregado
- Marca definida: la app se llama ReflexPod (productName y applicationId
  com.cyvalis.reflexpod en ProjectSettings; wordmark REFLEXPOD en la pantalla
  de rol). Verificar disponibilidad de marca antes de publicar.
- Pantallas nuevas generadas con la skill entrenamiento-pro-ui: Ajustes
  (sonido/vibración persistentes en PlayerPrefs, botón AJUSTES en la pantalla
  de rol; la vibración de las rondas ahora respeta el toggle) e Historial de
  sesiones (lista con estado vacío, datos de ejemplo en memoria, escena
  SessionHistory). Incluye SafeAreaFitter (notch/barra de gestos) reutilizable.
- Tarea 8 (en curso): modo GO/NO-GO, límite de tiempo por ronda, colores
  configurables, aceptación manual de estaciones desde el host (cambio en el
  plugin Kotlin: CONNECTION_REQUEST + accept/reject; requiere recompilar el
  AAR), UI suavizada (gradiente, sombras, paleta menos saturada) y feedback
  de resultado por ronda en overlay. Protocolo: ARM con flag go, OFF, END
  con errores.
- Tarea 7 (en curso): UI "deportivo oscuro" (UiTheme, sprite redondeado
  autogenerado, cards), animaciones (PanelFadeIn, ButtonPressScale, PulseScale),
  cuenta regresiva 3-2-1, vibración al armarse y tiempo de reacción gigante
  tras el toque. Regenerar la escena TrainingNearby desde el menú.
- Tarea 6 (completada 28/07/2026): sesión de entrenamiento real entre teléfonos.
  TrainingProtocol (START/ARM/END/HIT), SessionCoordinator (host: rondas,
  estación aleatoria, host puede participar como estación), StationAgent
  (medición local del tiempo de reacción), TrainingNearbyBootstrap (una escena
  con selección de rol, config, progreso en vivo, pantalla de color y resumen)
  y menú Entrenamiento > Crear escena TrainingNearby. Probable en Editor con
  estaciones simuladas que tocan solas.
- Tareas 1-3: modelos Core (Station, ReactionEvent, TrainingSession),
  ILocalTransport + SimulatedTransport, escenas de prueba en Editor
  (StationTest, MultiStationTest, TrainingTest).
- Tarea 4: puente Unity↔Kotlin (ping/pong) con AAR en Assets/Plugins/Android,
  NearbyTransport (stub), NearbyMessageReceiver, escena NearbyBridgeTest.
- Tarea 5 (completada 28/07/2026): Nearby Connections real en el plugin Kotlin
  (advertising/discovery/payloads, P2P_STAR), permisos en manifest del AAR,
  NearbyTransport con protocolo de eventos real, NearbyPermissions (runtime),
  NearbyConnectionTestBootstrap, escena NearbyConnectionTest (generada por
  script de Editor) y dependencia play-services-nearby en
  launcherTemplate.gradle. Verificada con dos teléfonos: conexión y mensajes
  en ambas direcciones.
