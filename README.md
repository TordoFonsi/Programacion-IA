# Simulator - Recuperatorio Parcial 1

Escena de practica para el recuperatorio. Todo se configura desde el Inspector y no hay
controlador global: cada agente decide con sus propios sensores.

## Abrir / reconstruir la escena

1. Abrir el proyecto con Unity `6000.3.17f1`.
2. Menu `Tools/Setup Simulator` para reconstruir `Assets/Scenes/Simulator.unity`.
3. Abrir la escena si no quedo abierta: `Assets/Scenes/Simulator.unity`.
4. Presionar Play.

El script de Editor arma limites, piso, paredes, camara, waypoints, Boids y Cazador.
Los valores de radios, pesos, velocidades y tiempos quedan editables en el Inspector.

## Estado actual

- Etapa 1: steering reutilizable (`SteeringBody`) y una demo temporal de movimiento.
- Etapa 2: percepcion por triggers (`TriggerSensor`) con listas locales por sensor.

## A probar en esta etapa

- Al dar Play los Boids se mueven, frenan al llegar a un destino y no salen de los limites.
- Cambiar `maxSpeed` y `maxForce` en `SteeringBody` cambia la forma de moverse.
- Mover o separar Boids antes de Play no rompe nada: cada uno busca su propio destino.
- En la vista Scene, seleccionar un Boid: se ven los radios de sus tres sensores.
  Durante Play, el sensor dibuja lineas verdes hacia los objetos que detecta.
- Todavia no hay flocking: los sensores ya reciben vecinos, pero la etapa 3 los usa.
