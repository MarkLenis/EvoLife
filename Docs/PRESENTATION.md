# Presentation

Stylized desktop demo visuals for EvoLife. Simulation, AI, biology, genetics, reproduction, analytics, and experiment lifecycle stay in their owning modules. This layer only reads those contracts and draws the world.

Related: [PERFORMANCE.md](PERFORMANCE.md), [ENVIRONMENT.md](ENVIRONMENT.md), [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md), [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md).

## Scenes

| Scene | Purpose |
|-------|---------|
| `Assets/EvoLife/Scenes/EvoLifeDemo.unity` | Presentation / demo world. Stylized biomes, creature/resource visuals, optional lighting and event cues. |
| `Assets/EvoLife/Scenes/Bootstrap.unity` | Lightweight bootstrap / training-oriented placeholder. Do **not** treat it as the demo, and do not load presentation effects into ML-Agents training by default. |

`EvoLifeDemo` is **not** the default ML-Agents training environment. Train in a minimal scene (Bootstrap or a dedicated training scene). Demo numbers are not experiment results.

## What the demo communicates

- Ecosystem extent: a ground disc at `DemoBiomeLayout.WorldRadius` (28)
- Resource locations: plant meshes plus wetland ponds from `WaterSource`
- Biome differences: grassland / forest / wetland / rocky ground colors
- Herbivore vs predator: green rounded body vs rust elongated body, snout on +Z
- Population motion: existing `PlanarMoveActionExecutor` locomotion (no second controller)

Default demo spawn counts (editable on `PresentationDemoBootstrap`): **24 herbivores**, **6 predators**. Caps remain 80 / 24. These are presentation defaults, not proven performance ceilings.

## Biome visual mapping

Logical zones are unchanged (`BiomeMap`, first containing zone wins, else grassland). Visual discs match those circles:

| Biome | Center | Radius | Visual |
|-------|--------|--------|--------|
| Forest | (-16, 0, 14) | 13 | dark green disc + decorative trees (not food) |
| Wetland | (18, 0, 12) | 11 | teal disc; drinkable ponds from `WaterSource` |
| Rocky | (2, 0, -18) | 13 | tan disc |
| Grassland | origin / default | 28 | light green disc under the others |

Specialized zones are listed **before** the large grassland zone so `BiomeMap.ResolveKind` still prefers forest/wetland/rocky. Regen, density, and temperature use existing `BiomeMap.Default*` values. Shaders never own biome logic.

## Creature prefab hierarchy

```
Creature root          simulation + AI + collider live here
├─ CreatureIdentity
├─ CreatureVitals
├─ CreatureGenome
├─ CreatureCapabilityMotor
├─ CreatureBrain
├─ PlanarMoveActionExecutor
├─ EvoLifeCreatureAgent
├─ CreatureReproductionBridge   (also added at spawn if missing)
├─ CapsuleCollider              primitive, not MeshCollider
├─ Rigidbody (kinematic)        so OverlapSphere sees moving colliders
├─ CreaturePresentation
├─ PhenotypeVisual
└─ Visual/                      meshes only; scaled by body_size
   ├─ Body
   ├─ Head
   ├─ Snout                     marks forward (+Z)
   └─ EarL / EarR
```

`CreaturePresentationFactory` builds this at runtime for the demo. Checked-in prefabs under `Assets/EvoLife/Prefabs/Creatures/` are the same contract for manual assignment.

Do not put locomotion or vitals on visual children. Swap meshes under `Visual/` without touching the root.

### Phenotype visuals

- `body_size` scales **only** the `Visual` child via `PhenotypeVisualScale` (clamped to `[0.75, 1.35]`).
- Root collider, motor speeds, and sensing range are **not** changed. Body size is visual-only.
- Optional generation tint (disabled by default) lightens materials with `MaterialPropertyBlock`; it has no gameplay effect.
- Aggression is not visualized as a combat stat.

## Plant / water prefabs

| Prefab | Runtime | Collider |
|--------|---------|----------|
| `Prefabs/Environment/Plant.prefab` | `PlantResource` + `PlantPresentation` | trigger `SphereCollider` |
| `Prefabs/Environment/WaterSource.prefab` | `WaterSource` + `WaterPresentation` | trigger `SphereCollider` |

`ResourceManager` still owns placement and regen. If no prefab is assigned it creates empty nodes; `PresentationWorldBuilder` then attaches presentation components.

- Plants stay in place when depleted. Foliage scale/tint shows remaining / capacity.
- Regenerated plants are the same GameObjects.
- Water is a flat stylized disc. It does not replace drinking (`IResourceNode.TryConsume`).
- Visual mesh colliders are stripped so plants/water do not block movement.

Selection raycasts against plant/water triggers should use `QueryTriggerInteraction.Collide` (Agent 10).

## Day / night lighting

`DayNightManager` remains the simulation-time authority. `DayNightLightingPresenter` implements `IDayNightLightingHook`:

- rotates the directional light from `NormalizedTimeOfDay`
- eases intensity / sun color
- optionally sets flat ambient

Biology does not read Unity lighting. Training scenes can omit the presenter.

## Event visuals

`EnvironmentalEventVisualAdapter` **subscribes** to `EnvironmentalEventManager.EventStarted` / `EventEnded` and reads `HasActiveEvent`. It must not call `Trigger`, `DepletePlants`, `BoostPlantAvailability`, or creature ports.

| Event | Presentation cue |
|-------|------------------|
| Drought | drier ground (`SetLushness`) |
| Food boom | slightly lusher ground; plants already show extra food |
| Wildfire | cheap glow + smoke meshes |
| Heat wave | orange haze disc |
| Other kinds | no extra world mesh |

Toggle `enableEffects` off for cheap runs. Effects are presentation-only.

## ShaderGraph / materials

The project uses the **Built-in** render pipeline (no URP/HDRP in `Packages/manifest.json`). Shader Graph was **not** added, because that would force a pipeline conversion.

Instead:

- `Assets/EvoLife/Shaders/EvoLifeStylizedColor.shader` — opaque instanced color
- `Assets/EvoLife/Shaders/EvoLifeStylizedWater.shader` — transparent vertex wave, no grab-pass
- Fallback: `Unlit/Color` / `Standard` via `Shader.Find`
- Shared materials in `Assets/EvoLife/Materials/` and `PresentationMaterials` (assign `sharedMaterial`, never `renderer.material`)

Simulation remains functional if shaders fail to import.

## Integration with desktop UI

Presentation does **not** own camera controls, selection, inspector, dashboard, Canvas, event UI, or AI debug overlays.

Hooks in `EvoLifeDemo.unity`:

1. `Main Camera` / `Agent10_CameraRigHook` — attach `DesktopCameraController` + `CreatureSelectionController` on the existing camera. Do **not** add a second enabled camera.
2. `Agent10_UiCanvasHook` — parent or place `DesktopDebugUi` here (runtime Canvas).
3. Wire HUD/inspector to existing `SimulationClock`, `PopulationTracker`, `EcosystemManager`, `EnvironmentalEventManager`. See [UI_DEBUG.md](UI_DEBUG.md).
4. Keep `PresentationDemoBootstrap` / world visuals. Do not duplicate `DayNightLightingPresenter` as a second sun controller.
5. One audio listener on Main Camera.

Shared files Presentation avoided so UI can own them:

- `Assets/EvoLife/Scripts/UI/` (including `SimulationHud`, camera, selection, inspector, dashboard, debug overlay)

## Manual Unity inspection

Automated tests cover component contracts, not pixels. After opening the Editor, follow [MANUAL_UNITY_VERIFICATION.md](MANUAL_UNITY_VERIFICATION.md).
