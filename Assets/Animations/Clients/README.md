# Animación de clientes (NPC)

Todo lo de esta carpeta lo genera la herramienta
`Assets/Scripts/Editor/ClientAnimationSetup.cs`
(**Tools > GuirisTrap > Generate Client Animation Assets** en el menú de Unity).

## Qué hay aquí

- **`ClientAnimator.controller`** — controlador compartido por todos los clientes.
  - Parámetros: `State` (int, espejo del enum `Client.State`) y `Speed` (float, velocidad del NavMeshAgent).
  - Estados: **Idle / Walk / Sit / Eat**.
- **`Client_Placeholder_*.anim`** — 4 clips provisionales que animan el transform
  `ModelPivot` (creado en tiempo de ejecución en `Client.initialize()`). Funcionan
  con los modelos actuales porque no necesitan esqueleto: solo mueven el pivote
  (rebote al andar, balanceo, bajada al sentarse, cabeceo al comer).

## Cómo poner las animaciones de verdad (drag & drop)

1. Abre `ClientAnimator.controller` en la ventana **Animator**.
2. Selecciona cada estado (Idle, Walk, Sit, Eat) y arrastra el clip real al campo
   **Motion** del Inspector.
3. Nada más. El código ya manda `State` y `Speed` — no hay que tocar ningún script.

### Si los clips nuevos son esqueléticos (modelo rigado / Humanoid)

- Configura el import del modelo a **Humanoid**.
- Añádele un componente `Animator` (con su Avatar) al prefab del modelo y asígnale
  este mismo controlador. El código usa el Animator del modelo si lo encuentra
  (`Client.initialize()` lo busca primero).
- Quita entonces el `Animator` del raíz de `Client.prefab` para no duplicar.
- Los clips deben ser *in-place* (sin root motion): quien mueve al cliente es el
  NavMeshAgent (`applyRootMotion = false`).

## Mapeo del parámetro `State`

| int | `Client.State` | Estado del animator |
|---|---|---|
| 0 | WalkingToEntrance | Walk / Idle (según `Speed`) |
| 1 | Waiting | Idle |
| 2 | WalkingToTable | Walk |
| 3 | WaitingForFood | Sit |
| 4 | Eating | Eat |
| 5 | DoneEating | Sit |
| 6 | Leaving | Idle → Walk (se levanta y sale) |
| 7 | Angry | Idle → Walk |

## Notas

- Volver a ejecutar la herramienta **sobrescribe** clips y controlador. No la
  reejecutes si ya has puesto clips reales en los estados.
- Los parámetros de movimiento de los placeholders (amplitud, periodo, inclinación…)
  están como constantes al principio de `ClientAnimationSetup.cs`.
- El Animator del prefab usa *Cull Update Transforms* (las cámaras son fijas por
  salas, así los clientes fuera de pantalla no gastan CPU).
