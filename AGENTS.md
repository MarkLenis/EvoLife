# EvoLife

AI-driven 3D wildlife ecosystem simulator (Unity) with a Python FastAPI analytics
backend and offline `evolife` genetics/experiment modules. See `README.md` and `Docs/`
for full context.

## Cursor Cloud specific instructions

### What runs in the cloud Linux VM

- **Only the Python parts are runnable here.** The Unity editor/compiler cannot run in
  this headless Linux VM, and ML-Agents training requires a live Unity instance, so
  Unity scenes, Unity Test Runner, and `Training/scripts/*` cannot be exercised
  end-to-end. Treat Unity work as manual verification (see `Docs/MANUAL_UNITY_VERIFICATION.md`).
- Two independent Python components are fully runnable/testable:
  - `evolife/` package + root `tests/` (genetics + experiments).
  - `Backend/` FastAPI analytics service + `Backend/tests/`.

### Dependencies

- Installed by the update script: `pip install -e ".[dev]"` (root, editable + pytest)
  and `pip install -r Backend/requirements.txt`. Both install to the user site
  (`~/.local`); no virtualenv is used.
- Console scripts land in `~/.local/bin`, which is not on `PATH`. Invoke tools via the
  module form, e.g. `python3 -m pytest`, `python3 -m uvicorn`.
- `Backend/requirements.txt` pins `pytest<9`, so installing Backend deps after the root
  package downgrades pytest to 8.x. That version runs both suites fine.

### Tests

- evolife suite: run `python3 -m pytest -q` from the repo root (uses root `pyproject.toml`
  / `pytest.ini`, `testpaths=tests`).
- Backend suite: run `python3 -m pytest -q` from `Backend/` (isolated temp SQLite per test).
- These mirror the two GitHub Actions workflows (`.github/workflows/python-ci.yml`,
  `backend-ci.yml`). There is no lint/format tooling configured in this repo.

### Running the backend

- From `Backend/`: `python3 -m uvicorn app.main:app --host 127.0.0.1 --port 8000`
  (add `--reload` for dev hot-reload). Swagger UI at `/docs`, health at `/health`.
- On startup it auto-creates the SQLite store at `Backend/data/evolife_analytics.db`
  (dev data persists across restarts; override with `EVOLIFE_DATABASE_URL`). No auth
  required. Core flow: `POST /api/v1/experiments` → `POST /api/v1/stats` →
  `GET /api/v1/stats?experiment_id=...` (this is what Unity posts).
