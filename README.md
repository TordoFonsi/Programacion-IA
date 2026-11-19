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

- Etapa 1: steering reutilizable (`SteeringBody`).
- Etapa 2: percepcion por triggers (`TriggerSensor`) con listas locales por sensor.
- Etapa 3: flocking con Separation, Alignment y Cohesion, pesos y radios configurables.
- Etapa 4: vida compartida (`Health`) y FSM propia del Boid con Flocking, SeekObject,
  Consume, Evade, Dead y Respawn.

## A probar en esta etapa

- Al dar Play los Boids forman un grupo y se mueven juntos sin superponerse.
- Cambiar `separationWeight`, `alignmentWeight` y `cohesionWeight` en `BoidFlocking` cambia el comportamiento.
- `separationRadius` debe quedar menor que `neighbourRadius`.
- Mover o separar Boids antes de Play no rompe nada.
- Cada Boid arranca en `Flocking`. Todavia no hay Cazador ni objetos, asi que Evade,
  SeekObject, Consume y Respawn se prueban en la etapa 5.
- En la vista Scene se ven los radios de flocking, vision y sensores.
