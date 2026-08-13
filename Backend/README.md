# EvoLife Analytics Backend

FastAPI service for experiment records and simulation statistics.

## Setup

```bash
cd Backend
python -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

## Run

```bash
uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

Open docs: http://127.0.0.1:8000/docs

## Tests

```bash
cd Backend
pytest -q
```
