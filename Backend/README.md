# EvoLife Analytics Backend

Python + FastAPI backend that receives ecosystem statistics from the Unity EvoLife simulator, stores experiment history locally, and exposes REST endpoints for historical graphs and AI evaluation.

## Project layout

```
Backend/
├── app/
│   ├── api/            # FastAPI routes and dependencies
│   ├── config.py       # Environment-driven settings
│   ├── main.py         # Application entrypoint
│   ├── persistence/    # SQLAlchemy models, database, repositories
│   ├── schemas/        # Pydantic request/response models
│   └── services/       # Business logic
├── tests/              # Pytest suite (isolated SQLite database)
├── openapi.json        # Generated OpenAPI schema artifact
└── requirements.txt
```

## Environment setup

Create and activate a virtual environment:

```bash
cd Backend
python3 -m venv .venv
source .venv/bin/activate
```

Optional environment variables (defaults are fine for local development):

```bash
export EVOLIFE_DATABASE_URL="sqlite:///./data/evolife_analytics.db"
export EVOLIFE_DEBUG="false"
```

Do not commit secrets. This project does not require authentication for university development use.

## Installation

```bash
pip install -r requirements.txt
```

## Start FastAPI

From the `Backend` directory:

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Interactive API documentation:

- Swagger UI: http://127.0.0.1:8000/docs
- ReDoc: http://127.0.0.1:8000/redoc

## Run tests

Tests use a temporary SQLite file under pytest's `tmp_path` fixture, isolated from development data.

```bash
pytest
```

## Generate OpenAPI schema artifact

```bash
python scripts/generate_openapi.py
```

This writes `openapi.json` in the `Backend` directory.

## REST endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Service health check |
| `POST` | `/api/runs` | Create a simulation run |
| `POST` | `/api/runs/{run_id}/finish` | Mark a run complete or failed |
| `GET` | `/api/runs/{run_id}` | Retrieve run details and counts |
| `GET` | `/api/runs` | List run history |
| `POST` | `/api/runs/{run_id}/snapshots` | Submit one population snapshot |
| `POST` | `/api/runs/{run_id}/snapshots/batch` | Batch-submit snapshots |
| `POST` | `/api/runs/{run_id}/creatures` | Submit creature lifetime/death records |
| `POST` | `/api/runs/{run_id}/generations` | Submit generation summaries |
| `GET` | `/api/runs/{run_id}/population-series` | Population time series for graphs |
| `GET` | `/api/runs/{run_id}/evolution-series` | Generation/evolution time series |

Schemas accept extra JSON fields for forward-compatible Unity payloads.

## Sample Unity integration flow

1. **Start a run** when the simulation begins and store the returned `run_id` in Unity.

```json
POST /api/runs
{
  "experiment_name": "predator_prey_lab_1",
  "random_seed": 1337,
  "configuration": {
    "map_size": 128,
    "initial_herbivores": 60,
    "initial_predators": 12,
    "plant_regrowth_rate": 0.4
  },
  "metadata": {
    "unity_scene": "EcosystemDemo",
    "build_version": "0.3.0"
  }
}
```

2. **Stream snapshots** every N simulation ticks (batch for efficiency).

```json
POST /api/runs/{run_id}/snapshots/batch
{
  "snapshots": [
    {
      "simulation_time": 0.0,
      "herbivore_population": 60,
      "predator_population": 12,
      "plant_count": 500,
      "births": 0,
      "deaths": 0,
      "average_herbivore_speed": 1.0,
      "average_predator_speed": 1.8,
      "average_vision": 14.0
    },
    {
      "simulation_time": 10.0,
      "herbivore_population": 58,
      "predator_population": 13,
      "plant_count": 480,
      "births": 4,
      "deaths": 2,
      "average_lifespan": 102.5,
      "average_energy": 67.2
    }
  ]
}
```

3. **Submit generation summaries** when a generation completes.

```json
POST /api/runs/{run_id}/generations
{
  "summaries": [
    {
      "species": "herbivore",
      "generation": 4,
      "population_count": 52,
      "average_genome_traits": {
        "speed": 1.25,
        "vision": 15.2,
        "metabolism": 0.9
      },
      "average_lifespan": 98.4,
      "reproduction_rate": 0.31,
      "offspring_per_parent": 1.8
    }
  ]
}
```

4. **Submit creature records** when individuals die (or in batches at checkpoints).

```json
POST /api/runs/{run_id}/creatures
{
  "records": [
    {
      "creature_id": "pred-0042",
      "species": "predator",
      "generation": 4,
      "birth_time": 210.0,
      "death_time": 355.5,
      "cause_of_death": "starvation",
      "parent_id_1": "pred-0011",
      "parent_id_2": "pred-0033",
      "offspring_count": 1,
      "genome_traits": {
        "speed": 2.4,
        "vision": 19.0,
        "attack_power": 1.1
      }
    }
  ]
}
```

5. **Finish the run** when the experiment ends.

```json
POST /api/runs/{run_id}/finish
{
  "status": "completed"
}
```

6. **Query graphs / AI evaluation data** from Unity or external tools.

```http
GET /api/runs/{run_id}/population-series
GET /api/runs/{run_id}/evolution-series?species=herbivore
GET /api/runs/{run_id}
GET /api/runs?experiment_name=predator_prey_lab_1
```

## Persistence

The default store is **SQLite** (`./data/evolife_analytics.db`). SQLAlchemy repositories isolate database access from API routes, so switching to PostgreSQL later is mostly a configuration change:

```bash
export EVOLIFE_DATABASE_URL="postgresql+psycopg://user:pass@localhost/evolife"
```

## C# helper sketch (Unity)

Use `UnityWebRequest` or `HttpClient` against the endpoints above. Keep the backend base URL configurable in a ScriptableObject or scene config, for example `http://127.0.0.1:8000`.
