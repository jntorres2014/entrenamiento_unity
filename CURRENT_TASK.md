# Tarea actual

## Tarea 8: Modos de entrenamiento + aceptación de estaciones + UI suavizada

## Objetivo
- Modo GO/NO-GO: rondas señuelo (rojo = quieto); tocar el señuelo o llegar
  tarde cuenta como error; quedarse quieto en el señuelo es acierto.
- Límite de tiempo por ronda configurable (sin límite / 3s / 5s / 10s);
  vencido el tiempo el host manda OFF y la ronda se resuelve.
- Colores configurables (variados por estación o todos iguales; en go/no-go
  son fijos: verde=go, rojo=no).
- El host ACEPTA o RECHAZA cada estación que pide unirse (clave si hay varios
  grupos usando la app cerca). Requiere AAR nuevo.
- UI suavizada: fondo con gradiente, sombras, bordes más redondeados, paleta
  menos saturada (incluye los colores de estación).

## Cambios técnicos
- Protocolo: ARM|ronda|color|go, OFF|ronda, END|hits|misses|avgMs|bestMs.
- Kotlin: el host ya no auto-acepta; manda CONNECTION_REQUEST|id|nombre a
  Unity y expone acceptConnection/rejectConnection. La estación auto-acepta.
- SessionConfig (Core) + SessionCoordinator con timeout y go/no-go;
  StationAgent con OnRoundTimedOut y flag go.
- NearbyTransport: evento OnConnectionRequest + AcceptStation/RejectStation.
- Bootstrap: botones de modo/límite/colores, tarjeta aceptar/rechazar,
  timer de ronda, feedback en overlay (¡MUY LENTO! / ¡BIEN, QUIETO! /
  ¡ERA ROJO!), resumen con aciertos y errores.
- Sim del Editor: las estaciones fantasma piden unirse (hay que aceptarlas)
  y en no-go se quedan quietas el 70% de las veces.

## Estado
- [x] Todo el código (Kotlin + Unity)
- [ ] RECOMPILAR AAR (Make Module :nearbyplugin) y copiar a Assets/Plugins/Android
- [ ] REGENERAR escena (menú Entrenamiento > Crear escena TrainingNearby)
- [ ] Probar en Editor (aceptar sims, modo go/no-go con timeout)
- [ ] Rebuild y prueba con teléfonos

## Pendiente de decidir (futuro)
- Sensor de proximidad / acelerómetro como alternativa al tacto.
- Cámara como "fotocélula" de salida/llegada (WebCamTexture + detección de
  movimiento en una franja del cuadro).
- Persistencia de resultados / export CSV.
