# Project Technical Overview: Bro Bots

> Updated for Unity 6000.3.23f1 and the September 2026 gameplay/scene modernization pass.

## 1. Project Description

**Bro Bots** is a fast-paced 3D top-down action arena combat game developed in Unity 6.3.23f1 using the Universal Render Pipeline (URP). Players control a customizable combat robot equipped with a detachable hand-head weapon, maneuvering through hazardous arena environments populated by enemy robots, industrial traps, scrap pickups, and collectible body components.

The game is targeted at players who enjoy arcade physics combat, arena survival, and mechanical upgrade loops.

### Core Pillars
- **Scrap Economy as Health and Power**: Scrap metal is the universal currency, acting simultaneously as player health, attack leveling fuel, and death penalty stakes.
- **Physics & Environmental Hazard Combat**: Direct combat revolves around AoE knockback impulses, pushing enemies into environmental death traps like rotating saw blades, pulsating laser beams, and lava pools.
- **Modular Robot Assembly & Progression**: Players advance stages by collecting modular robot body parts (legs, torso, left hand) and upgrading weapon stages by hoovering scrap debris.
- **Snappy Top-Down Controls**: Camera-relative omnidirectional movement combined with physics dashing and cooldown-driven area attacks.

---

## 2. Gameplay Flow / User Loop

1. **Boot / Main Menu**:
   - `Menu.unity` plays `"MenuMusic"` through the persistent `AudioManager` singleton.
   - `Menu.Play()` plays the click SFX, stops menu music, and loads build index `1`.
2. **Player Controls**:
   - Movement uses the **new Unity Input System** through `InputHandler`.
   - Keyboard: WASD or arrow keys for movement, left mouse button for attack, `Space` for dash.
   - Gamepad: left stick for movement, right trigger for attack, south face button for dash.
   - Movement is camera-relative and the controller resolves a valid camera reference at runtime if one is not serialized.
3. **Level_01 Progression**:
   - `Level01ProgressionBootstrap` creates `Level01ProgressionController` automatically when `Level_01` loads.
   - Intended progression is:
     `RoomTutorial -> Room2 -> Room3 -> Room4 -> LaserRoom / Final -> Complete`.
   - Tutorial completion opens access to Room2.
   - Room2, Room3, and Room4 use **finite enemy waves**. Clearing all configured spawners in the current room opens its exit Laser Wall and starts the next room.
   - `Room2/Cube (10)` is an additional proximity gate controlled by `ProximityDropGate`; it lowers to local `Y = -3.499` when the player approaches.
4. **Combat / Scrap Loop**:
   - AoE attacks query nearby colliders and call `EnemyMovement.Push()` on valid enemies.
   - Enemy knockback is Rigidbody-safe and temporarily disables the NavMeshAgent until the push finishes.
   - Enemies and the player can drop scrap through `ScrapExplosion`.
   - Scrap spawning is optimized so one logical drop creates one actual scrap child object rather than instantiating the old multi-piece container for every piece.
   - `ScrapAttractor` and `Scrap` move pickups through Rigidbody physics rather than transform teleporting.
5. **Body Parts / Victory**:
   - `Part1`, `Part2`, and `Part3` remain active at their manually placed scene positions and may be collected before Final.
   - Collecting 3/3 early does **not** immediately win the level.
   - `BodyParts.SetWinEnabled(false)` suppresses victory until progression reaches Final.
   - When Final begins, victory is enabled; if 3/3 are already collected, Win triggers immediately, otherwise the last remaining pickup ends the level.
6. **Loss / Testing**:
   - `ScrapManager.EndGame(false)` calls `PlayerController.Die()` for normal death flow.
   - `PlayerController` exposes an Inspector **God Mode** toggle. With it enabled, death is blocked while Win still works normally.
   - `LosePanel` pauses gameplay on Win using `Time.timeScale = 0` and an unscaled UI Animator.

---

## 3. Architecture

The architecture follows a decoupled component-based model where player state, enemy AI, obstacle interactions, and scrap economics communicate through direct references, triggers, and singletons.

### Architectural Patterns & Data Flow
- **Singleton Pattern**: `AudioManager.AM` persists through scene transitions with `DontDestroyOnLoad`. `ScrapManager.Instance` provides scene-local access to scrap/health state.
- **Runtime Bootstrap**: `Level01ProgressionBootstrap` registers for scene loads and creates the Level_01 progression controller without requiring a manually serialized controller object.
- **Event-driven Progression**: `TutorialCore.StateChanged`, `EnemySpawner.WaveCleared`, and `BodyParts.PartCollected` drive level progression without polling every object globally.
- **Finite Wave State**: each `EnemySpawner` tracks active enemies, total spawned enemies, finite-wave completion, and exposes `Total Enemies To Spawn` in the Inspector.
- **Physics / Navigation Hybrid**: enemies navigate with `NavMeshAgent`, temporarily switch to Rigidbody-based movement during knockback, then return to the nearest valid NavMesh position.
- **Scene-safe Reference Resolution**: modernized gameplay scripts fall back to scene lookups for player, camera, `ScrapManager`, and `AudioManager` when old serialized references point to prefab assets or are missing.

---

## 4. Game Systems & Domain Concepts

### Player Controller & Weapon System
Handles camera-relative omnidirectional locomotion, dash physics, animator state triggering, AoE knockback combat, and debug survivability.
- `NewPlayerBody.prefab`: canonical modern player setup used as the reference implementation across gameplay/test scenes.
- `PlayerController`: movement, dash, attack cooldown, weapon-stage radii, animation references, death handling, and Inspector `God Mode`.
- `InputHandler`: runtime-created new Input System actions for keyboard/mouse and gamepad.
- `WeaponRotation`: pointer-driven weapon orientation using the new Input System rather than `Input.mousePosition`.
- `UtilityClass`: pointer/screen helpers migrated away from legacy `Input.*` APIs.

**Important**: old serialized `firstWeaponPushPower`, `secondWeaponPushPower`, etc. still exist on `PlayerController`, but actual enemy displacement currently comes from `EnemyMovement.pushPower` when `enemy.Push()` is called.

**Location:** `Assets/Scripts/Player/`

---

### Scrap & Economy System
Governs health, score, level-up milestones, scrap attraction, and debris drops.
- `ScrapManager`: tracks points, thresholds, UI, audio, death state, and exposes `Instance` / `IsPlayerDead`.
- `Scrap`: Rigidbody-based pickup movement and collection.
- `ScrapAttractor`: Rigidbody velocity attraction in `FixedUpdate`; old per-frame transform movement and log spam were removed.
- `ScrapExplosion`: caches actual scrap-piece child templates and instantiates one physical scrap object per logical piece.

Current drop tuning is prefab-driven through `ScrapExplosion.Min Pieces / Max Pieces`. The ordinary `Enemy.prefab` currently uses `1..8`; `TutorialEnemy.prefab` uses `1..3`.

**Location:** `Assets/Scripts/`

---

### Body Parts Assembly System
Manages robot reconstruction objectives and Level_01 victory gating.
- `BodyParts`: tracks true collected-state flags, exposes `CollectedParts`, `TotalParts`, `HasAllParts`, `PartCollected`, and `SetWinEnabled(bool)`.
- `BodyItem`: world pickup that reports its configured part index, updates `BodyParts`, then destroys itself.
- The old bug where Win was effectively based on non-null UI `Image` references has been fixed.
- Level_01 deliberately decouples **collection time** from **win time**: parts may be collected anywhere, but victory is enabled only in Final.

**Location:** `Assets/Scripts/`

---

### Enemy AI & Spawner System
Controls NavMesh pursuit, melee attacks, knockback, and room wave completion.
- `EnemyMovement`: resolves the current scene player, samples back onto NavMesh when necessary, and performs Rigidbody-based knockback before restoring the NavMeshAgent.
- `EnemyManager`: randomizes visual GFX, handles tutorial hooks, and notifies its owning spawner through `EnemySpawner.EnemyDestroyed()` on destruction.
- `EnemySpawner`: supports both continuous spawning and finite waves.
- Level_01 tuning is Inspector-driven through `Total Enemies To Spawn`, `Max Spawned Enemies`, `Enemies Per Spawn`, `Seconds To Spawn Enemy`, and `Spawn Radius`.
- Spawn candidates are sampled onto the NavMesh before instantiation.
- Legacy `OnOffSpawners` objects may still exist in scenes, but Level_01 progression is now controlled by `Level01ProgressionController`.

**Location:** `Assets/Scripts/`

---

### Obstacle & Hazard System
Provides active industrial arena hazards that damage players and destroy enemies.
- `ObstacleManager`: multipurpose hazard script for saws, lasers, oil, and lava. Saw push movement is Rigidbody-safe and stops cleanly if the player becomes kinematic/dead, preventing the old `Setting linear velocity of a kinematic body is not supported` warning path.
- `DoorTrigger`: Lerp-based sliding door trigger opening and closing upon player entry.
- `ProximityDropGate`: opens the extra Room2 gate based on closest-point distance to the player's collider position.

**Extending the System**: Add new hazard variants by defining a new entry in the `ObstacleType` enum and implementing corresponding collision and update behaviors inside `ObstacleManager.cs`.

**Location:** `Assets/Scripts/`

---

### Audio System
Centralized audio management providing persistent background music playback and sound effect pooling.
- `AudioManager`: Persistent singleton managing an array of configurable sound sources with volume, pitch, audio mixer routing, and loop parameters.
- `Sound`: Serializable configuration container representing a sound effect or music track with an attached `AudioClip` and runtime `AudioSource`.

**Extending the System**: Add new audio clips by adding entries to the `sounds` array on the `AudioManager` prefab in the inspector and triggering them in code via `AudioManager.AM.Play("SoundName")`.

**Location:** `Assets/Scripts/`

---

### Tutorial & Flow System
Handles guided onboarding and explicit Level_01 room progression.
- `TutorialCore`: tutorial state machine (`Kamikaze`, `Play`, `Fail`) with `StateChanged` notifications.
- `Level01ProgressionController`: discovers `RoomTutorial`, `Room2`, `Room3`, `Room4`, and `LaserRoom`; assigns entry/exit Laser Walls from scene geometry; enables finite waves; and advances stages after wave completion.
- `Level01ProgressionBootstrap`: automatically creates the controller only for `Level_01`.

**Location:** `Assets/Scripts/`

---

## 5. Scene Overview

- `Assets/Scenes/Menu.unity`: main menu, Input System UI module, camera, and audio startup.
- `Assets/Scenes/Level_01.unity`: primary progression arena with tutorial, finite Room2/3/4 waves, progression Laser Walls, proximity gate, hazards, and three body-part pickups.
- `Assets/Scenes/Level_02.unity`: modernized gameplay scene with canonical player, current NavMeshSurface/data, audio/listener cleanup, and updated Unity serialization.
- `Assets/Scenes/Level_03.unity`: modernized gameplay scene with the same player/navigation/audio baseline.
- `Assets/Scenes/Test_*.unity`: retained development scenes. They were intentionally **updated rather than deleted** and are used for movement, combat, animation, camera, room, saw, and sample-scene verification.
- Modernized scenes now store regenerated NavMesh data as scene-adjacent `*-NavMesh.asset` files rather than the old nested `SceneName/NavMesh-NavMesh.asset` layout.

**Scene Flow / Build Caveat**:
1. `Menu.Play()` still loads **build index 1**, so the active Build Profile / build-scene list must place `Level_01` at index 1.
2. The checked-in `ProjectSettings/EditorBuildSettings.asset` currently lists `Menu` and test scenes but does not list `Level_01`; verify the active Unity Build Profile before producing a standalone build.
3. Death/Win UI may reload scenes or return to the menu through `LosePanel`.

---

## 6. UI System

The UI system is built using standard Unity UGUI combined with TextMeshPro components.

### UI Structure & Screens
- **Menu Canvas** (`Menu.unity`): Contains start buttons, game logo, and background graphic. Driven by `Menu.cs`.
- **HUD Canvas** (`Player / Arena HUD`):
  - **Scrap Counter & Level Progress**: TextMeshPro scrap count display and level progress slider managed by `ScrapManager.cs`.
  - **AoE Attack Cooldown Bar**: Floating world-space/screen-space slider above player managed directly by `PlayerController.aoeSlider`.
  - **Robot Body Parts Blueprint**: Array of UI `Image` components showing assembled robot limbs managed by `BodyParts.cs`.
- **Tutorial Overlay Panel**: Modal dialogue overlay driven by `TutorialCore.cs` with timescale-pause confirmation.
- **End Game Panel (`LosePanel.prefab`)**: Animated win/loss modal driven by `LosePanel.cs` and Animator (`LosePanel.controller`), providing restart and menu navigation buttons.

---

## 7. Asset & Data Model

- **Scriptable Objects & Volume Assets**:
  - `DefaultVolumeProfile.asset` / `UniversalRenderPipelineGlobalSettings.asset`: Defines global post-processing (bloom, tone mapping, color grading) and URP rendering configurations.
- **Prefabs (`Assets/Prefabs/`)**:
  - `Player.prefab`, `NewPlayerBody.prefab`: Complete player hierarchies including rig, animators, colliders, and weapon attachments.
  - `Enemy.prefab`, `FirstEnemy.prefab`, `TutorialEnemy.prefab`: Enemy NavMesh agent prefabs with swappable GFX meshes.
  - `Scrap.prefab`, `ScrapFromPlayer.prefab`: Physics-driven scrap pickup objects.
  - `Saw.prefab`, `Laser.prefab`, `Laser wall.prefab`, `Lava.prefab`, `Oil.prefab`: Modular arena hazard prefabs.
  - `AudioManager.prefab`: Persistent audio system prefab.
- **Naming & Organization Conventions**:
  - Scripts reside in `Assets/Scripts/` with player-specific code grouped in `Assets/Scripts/Player/` and utilities in `Assets/Scripts/Utils/`.
  - Audio files reside in `Assets/Audio/` with audio routing through `Main.mixer`.
  - 3D meshes, textures, and shader graphs are organized under domain folders in `Assets/Graphics/`.

---

## 8. Notes, Caveats & Gotchas

- **New Input System only in gameplay code**: do not reintroduce `Input.Get*`, `Input.mousePosition`, or `KeyCode`. `ProjectSettings.asset` uses `activeInputHandler: 1`.
- **Canonical player**: `NewPlayerBody` is the reference player setup. Its root Animator uses the robot controller/avatar, the nested legacy robot Animator is disabled, and the hand-head uses its dedicated controller/avatar.
- **Body parts must stay where authored**: do not disable `Part1/2/3` at startup and do not teleport them into `LaserRoom`.
- **Final victory gate**: 3/3 collected before Final is valid and intentional, but must not end the level until `BodyParts.SetWinEnabled(true)` is called at Final.
- **Knockback tuning**: actual enemy displacement is currently controlled by `EnemyMovement.pushPower`; the old push-power fields on `PlayerController` are not wired into `EnemyMovement.Push()`.
- **Unity-generated YAML whitespace**: many Unity assets serialize empty scalar values with trailing spaces. Do not hand-normalize these purely to satisfy `git diff --check` if Unity will immediately regenerate them.
- **Binary Level_01**: `Level_01.unity` is stored as a binary Unity SerializedFile in this repository. Prefer Unity/editor tooling for modifications rather than line-oriented text editing.
- **Audio lifetime**: `AudioManager` is persistent. Stop/replace tracks deliberately during scene changes to avoid overlap.
- **Scene reference fallback**: `EnemySpawner` and `EnemyMovement` can resolve `Player` by tag or `NewPlayerBody` by name when serialized references are missing or point outside the loaded scene.
- **Unity AI Assistant**: the editor-only `com.unity.ai.assistant` package is not required by gameplay and has been removed. `com.unity.ai.inference` remains a separate explicit package dependency.
