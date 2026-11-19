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

## Organizacion de scripts

- `Assets/Scripts/Recuperatorio/`: todo lo que usa la escena `Simulator.unity`.
- `Assets/Scripts/Parcial2/`: todo lo que usa la escena `Parcial2.unity`.

Dentro de `Recuperatorio`:

- `Boids/`: agente, flocking, FSM y estados del Boid.
- `Hunter/`: FSM y estados del Cazador.
- `FSM/`: `State` y `StateMachine` compartidos.
- `Core/`: steering, percepcion, vida, limites y objetos de interes.
- `Feedback/`: HUD y visuales.

Cada escena usa solo sus propios scripts: no hay referencias cruzadas entre `Recuperatorio`
y `Parcial2`.

## Estado actual

- Etapa 1: steering reutilizable (`SteeringBody`).
- Etapa 2: percepcion por triggers (`TriggerSensor`) con listas locales por sensor.
- Etapa 3: flocking con Separation, Alignment y Cohesion, pesos y radios configurables.
- Etapa 4: vida compartida (`Health`) y FSM propia del Boid con Flocking, SeekObject,
  Consume, Evade, Dead y Respawn.
- Etapa 5: FSM del Cazador con Patrol, Hunt, Gather y Rest, ataque melee/ranged,
  energia, objetos de interes y HUD de estado.
- Etapa 6: feedback de dano con flash de color y barra de vida sobre cada Boid y objeto.
- Etapa 7: limpieza del codigo viejo del Simulator.

## A probar en esta etapa

- Al dar Play los Boids forman un grupo y se mueven juntos sin superponerse.
- El Cazador recorre los waypoints en `Patrol` y cada tanto deja un objeto de interes.
- Cuando hay un Boid vivo y paso el TBA, pasa a `Hunt`: persigue, ataca de lejos (dano 1)
  o de cerca (dano 2) y no cambia de presa hasta que muera o salga de los limites.
- Al recibir dano el Boid parpadea en rojo y su barra de vida sobre el objeto baja.
  Lo mismo pasa con los objetos de interes que consumen los Boids.
- Si un Boid muere dentro de su vision, pasa a `Gather` y lo recolecta.
- Si se queda sin energia en `Hunt`, pasa a `Rest` y no sale hasta recuperar el maximo.
- El HUD muestra estado y energia del Cazador.
- Cambiar `separationWeight`, `alignmentWeight` y `cohesionWeight` en `BoidFlocking` cambia el comportamiento.
- `separationRadius` debe quedar menor que `neighbourRadius`.
- Mover o separar Boids (o el Cazador) antes de Play no rompe nada.
- En la vista Scene se ven los radios de flocking, vision y sensores.
