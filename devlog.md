# devlog.md — GuirisTrap Project Analysis

## 1. General Overview
- **Name**: GuirisTrap
- **Engine**: Unity 6 (6000.4.1f1)
- **Pipeline**: Universal Render Pipeline (URP 17.4.0)
- **Language**: C#
- **Input**: Unity Input System (1.19.0)
- **Camera**: Cinemachine (3.1.5)
- **UI**: TextMeshPro + TMPEffects (Luca3317)
- **Other packages**: AI Navigation (NavMesh 2.0.12), Visual Scripting
- **Repo**: https://github.com/sebaso/GuirisTrap.git

## 2. Concept
A restaurant/beach-bar simulator where the player serves tourists ("guiris").
The loop alternates between:
1. **PreparationScene** — buy furniture/equipment and arrange the venue on a grid system.
2. **GameScene** — run the service, cook food through minigames, serve clients, earn money, and repeat until the day timer ends.

## 3. Architecture & Module Breakdown

### Core Systems
- **SceneController** — Singleton that manages scene transitions and initializes `GameGridManager`s on load.
- **CashManager** — Singleton economy tracker (spend/earn events).
- **DayManager** — Singleton timer that runs the in-game day and forces a return to `PreparationScene`.
- **PlayerController** — Extends `ControllableMonoBehaviour` and handles movement, pickup/drop food, and interaction with key objects (tables, kitchens, minigames).

### Grid / Placement (PreparationScene)
- **GameGridManager** — Manages a 2D grid for a specific surface type (Floor, WallSouth, WallEast, WallWest). Handles visibility, placement validation, cell occupancy, and chair orientation.
- **GridController** — Switches the active grid based on camera view, selects/moves/places `PlaceableObject`s, and cleans up invalid placements.
- **PlaceableObject / PlaceableItemData** — Data-driven placement via ScriptableObjects (prefab, icon, category, cost, surface compatibility, offset).
- **PlaceableCategory** — Categories like Table, Chair, WallItem, etc.
- **Inventory** — Grid-based inventory where bought items are stored before placement.
- **GridToolEditor** — Custom editor tool for shelf placement (Editor folder).

### Gameplay Systems (GameScene)
- **GameManager** — Bridges inventory and grid: handles `Buy()` and `Place()` of placeables.
- **Kitchen / CookingStation / FoodStorage** — Food stations that produce or hold `Food` objects.
- **Minigames** — Cooking minigames are implemented with a base (`MiniGameBase`) and concrete variants:
  - `CongeladorMinigame`
  - `DespensaMinigame`
  - `EspetoMinigame`
  - `NeveraMinigame`
  - `MinijuegoEspecias` (spice minigame with `Especia`, `Cubonegro`, UI layout)
- **Client System** —
  - `ClientSpawner` — Spawns clients during the day.
  - `Clientgroup` / `Client` — Groups of clients that order, wait (patience), eat, and pay.
  - `RestaurantManager` — Client routing/seating logic.
- **UI / Feedback** —
  - `CashUI`, `DayTimerUI`, `PatienceBar`
  - `FadeEffectComponent` for scene transitions
  - `InteractableFeedback` for interaction prompts
  - `CameraOcclusion` / `CameraController` with multiple views (Perspective, TopDown, Wall views)

## 5. Notable Technical Observations

### Positives
- **Data-driven design**: `PlaceableItemData` as ScriptableObject keeps prefab/cost/surface rules decoupled from code.
- **Event-driven singletons**: `CashManager` and `DayManager` expose events (`OnMoneyChanged`, `OnDayProgress`, `OnDayEnded`) that allow UI and gameplay to react without tight coupling.
- **Multi-surface grid**: The floor/wall grid split with camera-view switching is a solid approach for a room editor.
- **Modular minigames**: The minigame system uses inheritance (`MiniGameBase`) with per-station implementations, making it easy to add new cooking stations.
- **Modern Unity features**: Uses the new Input System, URP, and newer APIs (`FindObjectsByType<T>`, `Physics.OverlapSphere`, `SceneManager.sceneLoaded`).

### Areas to Watch / Potential Improvements
- **Singleton overuse**: `GameManager`, `SceneController`, `CashManager`, `DayManager`, `PlayerController` all rely on singleton patterns. This works for a small project but can become fragile as scope grows.
- **Scene transitions**: `SceneController.ChangeScene` calls `LoadScene` directly; if `GameGridManager` data needs persist across scenes, consider a dedicated persistence/initialization layer instead of `FindObjectsByType` in `OnSceneLoaded`.
- **Debug spam**: Several `Debug.Log` calls should be replaced with `[Conditional("UNITY_EDITOR")]` loggers or removed.
- **Null / hard-coded strings**: `GameObject.Find("")` in `GameManager.Place()` is a no-op smell. Typed references would be safer.
- **Rigidbody movement**: `PlayerController` sets velocity in `FixedUpdate` but reads input in `Update`-style callbacks from `ControllableMonoBehaviour` — make sure the direction vector is cached/synchronized per frame to avoid input lag.

## 6. Git Workflow
- **Branching**: `develop` is the integration branch. Feature branches include `BranchSebas`, `BranchPepe`, `BranchAmanda`, `multiGrid`, `toolGrid`, `transparentShader`, `ReworkFP`.
- **Recent history highlights**:
  - Wall transparency shader work (`transparento las paredes`, `transparencia remake`)
  - Multi-grid refactor (`multiGrid`)
  - Client-layer fixes and end-of-sprint polish
  - Purchasing, wall-hanging, and moving paint/pictures mechanics
- Team size appears small and iterative; commits are functional (“Arreglado los clientes por falta del layer”) rather than large merges.

## 7. Asset & Scene Layout
- **Scenes**: `MainMenu`, `PreparationScene`, `GameScene`, `ZonaDePruebas`.
- **Old scenes**: `SegundaEntregaOld`, `TerceraEntregaOld` with milestone builds (`PepePruebas`, `SebasTest`, etc.).
- **Art assets**: Placeables likely in `Assets/Models` and `Assets/Prefabs`; fonts in `Assets/Fonts/Malacitana`; UI theme appears gothic/urban (`lletraferida` font).

## 8. Summary
GuirisTrap is a well-structured student/small-team Unity 6 prototype for a restaurant management game. Its biggest strengths are the clean separation between the room-editor (PreparationScene) and the service loop (GameScene), and the data-driven approach to placeable objects. The main risk areas for scaling are singleton-heavy architecture and some direct `Find`/`Debug.Log` usage. The codebase is in active development with clear milestones tied to client mechanics, grid systems, and environment interaction.

