# Performance

Notes for a desktop demo with a large living population (on the order of tens of herbivores, a dozen predators, and many plants). **These counts are not profiled.** Do not treat 80 / 12 as a measured budget.

Related: [PRESENTATION.md](PRESENTATION.md), [ENVIRONMENT.md](ENVIRONMENT.md), [ARCHITECTURE.md](ARCHITECTURE.md).

## Training vs demo

| | Training | Demo (`EvoLifeDemo`) |
|--|----------|----------------------|
| Goal | PPO / scripted experience | readable ecosystem |
| Rendering | minimal | stylized biomes, plants, lighting |
| Effects | off | optional event cues |
| Scene | `Bootstrap.unity` (or a dedicated training scene) | `EvoLifeDemo.unity` |
| Observation/debug | normal AI sensors | same AI; no extra debug UI in this PR |

Do not parent presentation builders onto the training scene.

## Census cache

`ResourceManager` implements `IReadOnlyResourceCensus`. Before this change, `PlantCount`, `PlantAbundance`, and related properties each called `CaptureCensus()` and walked every resource node.

**Current contract:**

- Properties (`PlantCount`, `WaterSourceCount`, `TotalPlantFoodRemaining`, `TotalPlantCapacity`, `PlantAbundance`, `PlantDensity`) share one cached `ResourceCensus`.
- Cache lifetime: the current Unity frame (`Time.frameCount`), plus explicit invalidation on `Tick`, `PlaceResources`, `TrackPlant` / `TrackWater`, `BoostPlantAvailability`, and `DepletePlants`.
- `CaptureCensus()` **always** rebuilds the snapshot and refreshes the cache.

Analytics and experiment code should keep calling `CaptureCensus()` (or `CaptureState`) when they need a live remaining-food total. In-frame `TryConsume` on a plant does **not** invalidate the property cache; `CaptureCensus()` sees the new remaining immediately.

`WorldArea` and `TemperatureNormalized` are not census walks.

## Presentation choices that stay cheap

- Curated Kenney CC0 low-poly FBX (trees, rocks, bushes, grass, deer, fox), **not** high-poly film assets
- Primitive colliders on creatures (`CapsuleCollider`); trigger spheres on plants/water
- No `MeshCollider` on population or décor; imported colliders are stripped on spawn; no NavMesh
- `sharedMaterial` + `MaterialPropertyBlock` for depletion / lushness (no `renderer.material` instancing)
- Shadows on trees / large rocks / creatures only; grass/reeds do not cast
- GPU instancing enabled on opaque stylized ground materials
- Event cues are a few scaled primitives, not particle-heavy VFX (and can be disabled)
- Decorative forest trees are a fixed set of Kenney meshes, not `PlantResource` instances

## Reviewed and left alone

Low-risk, behavior-preserving only. The following were inspected and **not** rewritten:

- `PhysicsCreatureProximitySensor` / `LocalCreatureInteractor` already use `OverlapSphereNonAlloc` with a size-32 buffer. Caching `GetComponentInParent` would touch AI, which this PR does not own.
- `PlantResource` / `WaterSource` `FindFirstObjectByType<ResourceRegistry>` runs on enable (spawn), not per frame. `ResourceManager` still binds the registry after instantiate.
- `CreatureBrain` / `EvoLifeCreatureAgent` `FindFirstObjectByType` is spawn-time.
- `EnvironmentalEventManager.ActiveEvents` allocates a copy list. Visuals use `HasActiveEvent` instead. Changing `ActiveEvents` was skipped so Agent 10 event UI can keep the current snapshot contract.
- Creature vitals ticking remains a Simulation concern (`SimulationRunner` tick list). This PR does not add a second biology clock.

## Demo scale guidance (unprofiled)

Default demo spawn counts (editable on `PresentationDemoBootstrap`): **24 herbivores**, **6 predators**. Caps remain 80 / 24. These are presentation defaults, not proven performance ceilings. The demo world footprint is ~150m so ~80 herbivores have room to spread at overview scale.

If a demo hitch appears:

1. Disable `EnvironmentalEventVisualAdapter.enableEffects`
2. Lower plant density on `DemoBiomeLayout` / `PlantSpawnSettings` (does change resource abundance — that is an experiment knob, not a silent visual-only trick)
3. Keep training on `Bootstrap.unity`
