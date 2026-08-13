# EvoLife

AI-driven 3D wildlife ecosystem simulator (Unity) with multi-agent RL (ML-Agents / PPO),
genetic inheritance, and a FastAPI analytics backend.

> **Scope of this repository revision:** architectural skeleton and module boundaries.
> No production terrain, art, tuned rewards, or trained models are included.

## Repository layout

| Path | Purpose |
|------|---------|
| `Assets/EvoLife/` | Unity simulation code, prefabs, scenes, tests |
| `Backend/` | FastAPI experiment/statistics API |
| `Training/` | ML-Agents configs and helper scripts |
| `Docs/` | Architecture and contributor guides |

## Quick start

### Unity

1. Install **Unity 2022.3 LTS** (see `ProjectSettings/ProjectVersion.txt`).
2. Open this repository root as a Unity project.
3. Allow Package Manager to resolve `com.unity.ml-agents` and test frameworks.
4. Open `Assets/EvoLife/Scenes/` (create/assign Bootstrap scene on first open if needed).

### Backend

```bash
cd Backend
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
pytest -q
```

## Documentation

- [Architecture](Docs/ARCHITECTURE.md)
- [Reproduction / ecosystem lifecycle](Docs/REPRODUCTION.md)
- [ML-Agents / PPO](Docs/AI_ML_AGENTS.md)
- [Scripted baseline](Docs/SCRIPTED_BASELINE.md)
- [Genetics](Docs/GENETICS.md)
- [Development](Docs/DEVELOPMENT.md)
- [Agent boundaries](Docs/AGENT_BOUNDARIES.md) — for parallel human/AI contributors

## Design principles

- Domain modules stay separated (Creatures, Genetics, AI, Simulation, Environment, Analytics).
- Prefer small components and interfaces over god-managers.
- AI reads creature state; Genetics modifies capabilities; Simulation owns time/population.
