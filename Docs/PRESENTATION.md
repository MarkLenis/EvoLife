# Presentation

Stylized desktop demo visuals for EvoLife — a **research diorama** meant to make AI experiment behavior readable in a university demo. Simulation, AI, biology, genetics, reproduction, analytics, and experiment lifecycle stay in their owning modules. This layer only reads those contracts and draws the world.

Related: [PERFORMANCE.md](PERFORMANCE.md), [ENVIRONMENT.md](ENVIRONMENT.md), [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md), [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md).

## Design principle

If forced to choose between “more beautiful” and “easier to understand AI behavior,” choose **understandability**.

Visual hierarchy: creatures → resources → biome boundaries → event cues → decoration.

## Scenes

| Scene | Purpose |
|-------|---------|
| `Assets/EvoLife/Scenes/EvoLifeDemo.unity` | Presentation / demo world (~150m basin). |
| `Assets/EvoLife/Scenes/Bootstrap.unity` | Lightweight bootstrap / training-oriented placeholder. |

`EvoLifeDemo` is **not** the default ML-Agents training environment.

## World layout (basin, not quadrants)

Active footprint ≈ **150m** diameter (`DemoBiomeLayout.WorldRadius = 75`) plus ~15m outer visual buffer.

| Region | Placement | Role |
|--------|-----------|------|
| Grassland | Center basin | Main feeding / pursuit readability |
| Forest | North / northwest crescent | Darker habitat, sparse non-colliding trees |
| Wetland | West / southwest | Shallow ponds + reeds; drinkable `WaterSource`s |
| Rocky / dry | South / southeast | Resource-poor contrast; drought cue |

Logical zones remain circular `BiomeZone`s (specialized first). Visual biome edges are **irregular ground patches**, not concentric transition rings. The painted boundary can be more organic than the logical circle.

Plant densities on the demo layout are **lower than BiomeMap defaults** so a large map does not explode plant count. ResourceManager remains authoritative.

Default demo spawn: **24 herbivores / 6 predators** (caps 80 / 24). Caps are visual/performance targets, not profiled guarantees.

## What the demo communicates

- Food concentration (edible leafy clusters vs thin decorative tufts)
- Water (shallow teal ponds; multiple wetland sources)
- Biome differences (palette + landmarks)
- Herbivore vs predator (**Kenney deer vs fox meshes**, plus facing locators)
- Population motion / clustering in the open basin
- Event stress (drought desaturation, concentrated wildfire cue, heat tint). Food boom is communicated by actual extra vegetation.

## Creature prefab hierarchy

```
Creature root          simulation + AI + CapsuleCollider + kinematic Rigidbody
├─ … required runtime components …
├─ CreaturePresentation
├─ PhenotypeVisual
└─ Visual/             meshes only; body_size scales THIS child (0.80–1.25)
   ├─ Model            Kenney Cube Pets deer (herbivore) or fox (predator)
   └─ Head / Snout / Tail / EarL locators for facing tests
```

Forward = **+Z**. Colliders stay on the root — genetics visualization does not change sensing fairness.

Imported Kenney CC0 meshes live under `Assets/EvoLife/Resources/EvoLifeModels/` (Nature Kit trees/rocks/plants, Cube Pets deer/fox). `PresentationModelLibrary` loads them at runtime and falls back to primitives if a mesh is missing. Licenses: `Assets/EvoLife/Models/THIRD_PARTY_NOTICES.md`.

## Plants / water / décor

| Object | Collider | Notes |
|--------|----------|-------|
| Edible `PlantResource` | trigger sphere (unchanged radius) | Kenney bush mesh on `Visual/`; depletion scales that child |
| `WaterSource` | trigger sphere (unchanged radius) | Irregular shallow disc + Kenney lily pads; mesh does not block movement |
| Decorative grass / reeds / stones / trees | **none** | Kenney Nature Kit meshes; colliders stripped on spawn |

## Day / night / events

`DayNightManager` is simulation authority. `DayNightLightingPresenter` rotates/intensifies the sun and keeps **night readable** (not near-black).

`EnvironmentalEventVisualAdapter` only **reads** event state. Drought → ground desaturation; wildfire → limited glow/smoke near forest edge; heat → warm ground tint; food boom → slightly lusher ground. No second resource lifecycle.

## Camera anchors (no controller)

Runtime anchors under `PresentationAnchors` (Agent 10 may bind a controller later):

- `CameraAnchor_Overview` ≈ `(0, 52, -102)` looking at origin
- `CameraAnchor_Grassland` / `ForestEdge` / `Wetland` / `Rocky` / `LowAngleDemo`

Main Camera is positioned to the overview on Play for demo convenience. **Do not add a second enabled camera.**

## Integration hooks for Agent 10

| Hook / owner object | Use |
|---------------------|-----|
| `Main Camera` / `Agent10_CameraRigHook` | Attach `DesktopCameraController` + selection |
| `Agent10_UiCanvasHook` | Parent desktop debug Canvas |
| `SimulationSystems` | `SimulationClock`, `PopulationTracker`, spawn/reproduction |
| `Environment` | `ResourceManager`, `DayNightManager`, `EnvironmentalEventManager` |
| `PresentationSystems` | Bootstrap / lighting / event visuals |

Shared files Presentation intentionally leaves to UI: everything under `Assets/EvoLife/Scripts/UI/`.

## Shader / materials

Built-in RP stylized shaders (`EvoLifeStylizedColor` / `EvoLifeStylizedWater`) with Unlit fallback. Shared materials + `MaterialPropertyBlock` only.

## Manual inspection

See [MANUAL_UNITY_VERIFICATION.md](MANUAL_UNITY_VERIFICATION.md) presentation checklist. Automated tests cover contracts, not pixels.
