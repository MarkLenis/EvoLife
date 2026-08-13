# EvoLife Analytics Backend

Python + FastAPI service for experiment records and simulation statistics. Unity posts population snapshots via the **v1 API**; extended endpoints support richer analytics for graphs and AI evaluation.

## Project layout

```
Backend/
├── app/
│   ├── api/            # Routes and dependencies
│   ├── config.py       # Environment-driven settings
│   ├── main.py         # Application entrypoint
│   ├── persistence/    # SQLAlchemy models, database, repositories
│   ├── schemas/        # Pydantic request/response models
│   └── services/       # v1 + extended business logic
├── tests/              # Pytest (isolated SQLite per test)
├── openapi.json        # Generated OpenAPI artifact
└── requirements.txt
```

## Environment setup

```bash
cd Backend
python3 -m venv .venv
source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

Optional environment variables:

```bash
export EVOLIFE_DATABASE_URL="sqlite:///./data/evolife_analytics.db"
```

Do not commit secrets. No authentication is required for university development use.

## Start FastAPI

```bash
uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

- Swagger UI: http://127.0.0.1:8000/docs
- ReDoc: http://127.0.0.1:8000/redoc

## Run tests

Tests use a temporary SQLite database under pytest's `tmp_path`, isolated from development data.

```bash
pytest -q
```

## Generate OpenAPI schema

```bash
python scripts/generate_openapi.py
```

## API overview

### Unity v1 (unchanged contracts)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check |
| `POST` | `/api/v1/experiments` | Create experiment record |
| `GET` | `/api/v1/experiments` | List experiments |
| `POST` | `/api/v1/stats` | Submit population snapshot |
| `GET` | `/api/v1/stats` | List stats (`?experiment_id=` optional) |

v1 experiments and stats share the same SQLite persistence as extended runs. An experiment `id` equals a run `run_id`.

### Extended analytics

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/v1/runs` | Create simulation run |
| `POST` | `/api/v1/runs/{run_id}/finish` | Finish run |
| `GET` | `/api/v1/runs/{run_id}` | Run details |
| `GET` | `/api/v1/runs` | Run history |
| `POST` | `/api/v1/runs/{run_id}/snapshots` | Submit snapshot |
| `POST` | `/api/v1/runs/{run_id}/snapshots/batch` | Batch snapshots |
| `POST` | `/api/v1/runs/{run_id}/creatures` | Creature lifetime records |
| `POST` | `/api/v1/runs/{run_id}/generations` | Generation summaries |
| `GET` | `/api/v1/runs/{run_id}/population-series` | Population time series |
| `GET` | `/api/v1/runs/{run_id}/evolution-series` | Evolution time series |

## Sample Unity payloads (v1)

**Create experiment** (optional — Unity can also use a fixed `experimentId` string):

```json
POST /api/v1/experiments
{
  "name": "predator_prey_lab_1",
  "policy_herbivore": "scripted_baseline",
  "policy_predator": "learned_ppo",
  "seed": 1337
}
```

**Post stats** (matches `SimulationStatsSnapshot` in Unity):

```json
POST /api/v1/stats
{
  "experimentId": "<experiment-uuid>",
  "simulationTimeSeconds": 12.5,
  "herbivoreCount": 10,
  "predatorCount": 2,
  "totalAlive": 12,
  "timestampUtcUnix": 1700000000.0
}
```

**Extended batch snapshots** (when richer metrics are available):

```json
POST /api/v1/runs/{run_id}/snapshots/batch
{
  "snapshots": [
    {
      "simulation_time": 0.0,
      "herbivore_population": 60,
      "predator_population": 12,
      "plant_count": 500,
      "average_herbivore_speed": 1.0,
      "average_vision": 14.0
    }
  ]
}
```

## Persistence

Default store: **SQLite** at `./data/evolife_analytics.db`. Repositories isolate database access from routes; migrating to PostgreSQL is primarily a configuration change:

```bash
export EVOLIFE_DATABASE_URL="postgresql+psycopg://user:pass@localhost/evolife"
```

## Unity integration flow

1. Start backend locally on port 8000.
2. Set `BackendClient.baseUrl` in Unity to `http://127.0.0.1:8000`.
3. Optionally create an experiment via `POST /api/v1/experiments` and set `PopulationStatisticCollector.experimentId` to the returned `id`.
4. `StatsExportLoop` POSTs snapshots to `/api/v1/stats` during simulation.
5. Query `/api/v1/runs/{run_id}/population-series` for historical graphs or AI evaluation data.
6. As genetics/evolution features land, POST creature and generation records to the extended endpoints.
