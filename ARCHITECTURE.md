# Arquitectura - App Entrenamiento

## Principios
- Separación estricta: UI / Lógica de entrenamiento / Datos / Transporte de red.
- Core (modelos y reglas) es C# puro, sin MonoBehaviour, sin dependencias de Unity.
- Toda comunicación entre teléfonos pasa por la interfaz ILocalTransport.
- MVP usa SimulatedTransport. Nearby Connections se integra después vía plugin Android (Kotlin).

## Estructura de carpetas
Assets/Scripts/
  Core/
    Models/      -> Station, ReactionEvent, TrainingSession, etc.
    Rules/       -> Lógica de ejercicios: aciertos, errores, tiempo de reacción.
  Transport/
    ILocalTransport.cs
    SimulatedTransport.cs
    NearbyTransport.cs (futuro)
  Presentation/  -> MonoBehaviours finos (UI, pantallas de color)
  App/           -> Bootstrap / composición de dependencias

Assets/Plugins/Android/ -> Plugin Kotlin (futuro, Nearby Connections)

## Restricciones del MVP
- Sin Firebase.
- Sin AR.
- Sin internet (comunicación local únicamente).
- Máximo 4 teléfonos.
- No agregar paquetes sin justificar antes por qué son necesarios.