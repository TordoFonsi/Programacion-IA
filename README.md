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

## Probar mover agentes

El area de juego la define el objeto `Bounds` (tiene `WorldBounds`): los Boids se frenan y
se mantienen dentro de esa caja. Si se seleccionan todos los objetos de la escena y se
mueven, `Bounds` tambien se mueve y el area se corre con ellos, por eso da la impresion
de que los Boids se escapan del mapa.

Para probar el clamp:

1. Mover o separar solo los Boids (o el padre `Boids`), no `Bounds`.
2. Dar Play: los Boids vuelven solos hacia el area de `Bounds`.
3. Si algo quedo desacomodado, `Tools/Setup Simulator` reconstruye la escena completa.

## Capas necesarias

| Capa | Uso |
| --- | --- |
| `Boid` | Boids y su sensor de vecinos |
| `Hunter` | Cazador |
| `InterestObject` | Objetos de interes |
| `Obstacle` | Piso, paredes y waypoints |

`SimulatorSetup` busca las capas por nombre y avisa en consola si falta alguna.

## Pasos manuales (si no se usa el menu)

1. `Bounds`: GameObject vacio en `(10, 0, 6)` con `WorldBounds` y tamano `(44, 1, 36)`.
2. Piso: `Plane` escalado `(4.4, 1, 3.6)` en el centro, capa `Obstacle`.
3. Paredes: 4 cubos de 0.3 de espesor en los bordes, capa `Obstacle`.
4. Camara ortografica cenital con tag `MainCamera` y `AudioListener`, mas una luz direccional.
5. Waypoints: 4 cilindros en las esquinas internas, capa `Obstacle`, sin collider.
6. Cada Boid: cubo en capa `Boid` con `SteeringBody`, `BoidFlocking`, `Health`,
   `HealthFeedback`, `BoidFSM` y `BoidAgent`.
7. Cada Boid necesita tres hijos con `TriggerSensor`, `SphereCollider` trigger y
   `Rigidbody` kinematic: `NeighbourSensor` (capa `Boid`), `ThreatSensor` (capa `Hunter`)
   e `InterestSensor` (capa `InterestObject`).
8. Cazador: capsula en capa `Hunter` con `SteeringBody`, `HunterFSM`, `HunterAttackVisual`
   (junto a un `LineRenderer`) y `HunterFeedback`; un hijo `VisionSensor` con
   `TriggerSensor` rol `HunterVision` y capa `Boid`.
9. Asignar en el Inspector del Cazador los waypoints, `WorldBounds` y el prefab de objeto
   de interes. En los Boids asignar `WorldBounds` y la mascara de Boids.
10. Prefab `InterestObject`: esfera en capa `InterestObject` con `Health`, `InterestObject`
    y `HealthFeedback`.

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
- Etapa 8: vision del Cazador reducida, HUD del Cazador ampliado y HUD de estado por Boid.

## A probar en esta etapa

- Al dar Play los Boids forman un grupo y se mueven juntos sin superponerse.
- Cada Boid muestra su estado actual en una etiqueta sobre la barra de vida.
- El Cazador recorre los waypoints en `Patrol` y cada tanto deja un objeto de interes.
  Su vision es de 10 unidades, asi que la patrulla se aprecia mejor.
- Cuando hay un Boid vivo dentro de su vision y paso el TBA, pasa a `Hunt`: persigue,
  ataca de lejos (dano 1) o de cerca (dano 2) y no cambia de presa hasta que muera o
  salga de los limites.
- Al recibir dano el Boid parpadea en rojo y su barra de vida sobre el objeto baja.
  Lo mismo pasa con los objetos de interes que consumen los Boids.
- Si un Boid muere dentro de su vision, pasa a `Gather` y lo recolecta.
- Si se queda sin energia en `Hunt`, pasa a `Rest` y no sale hasta recuperar el maximo.
- El HUD del Cazador muestra estado y energia; la etiqueta de cada Boid muestra su estado.
- Cambiar `separationWeight`, `alignmentWeight` y `cohesionWeight` en `BoidFlocking` cambia el comportamiento.
- `separationRadius` debe quedar menor que `neighbourRadius`.
- Mover o separar Boids (o el Cazador) antes de Play no rompe nada; mover `Bounds`
  corre el area de juego y da la sensacion de que los Boids se escapan.
- En la vista Scene se ven los radios de flocking, vision y sensores.

## Resumen de la consigna

- Steering reutilizable: `SteeringBody` con Seek, Flee, Arrive, Pursuit, Evade,
  Separation, Alignment y Cohesion; devuelve aceleracion y mueve por velocidad.
- Percepcion por triggers: `TriggerSensor` con una lista local por sensor y roles
  `BoidNeighbours`, `BoidThreat`, `BoidInterest` y `HunterVision`.
- Flocking: `BoidFlocking` con radios y pesos configurables.
- FSM del Boid: `BoidFSM` y estados Flocking, SeekObject, Consume, Evade, Dead y Respawn.
- FSM del Cazador: `HunterFSM` y estados Patrol, Hunt, Gather y Rest, con TBA, energia,
  melee/ranged y presa bloqueada hasta que muera o salga de los limites.
- Feedback de dano: flash de color y barra de vida en `HealthFeedback`.
- Sin estado global: no hay registros ni `FindObjectsOfType`; los agentes se detectan
  entre si con sus sensores y el Cazador guarda la presa solo mientras la persigue.
