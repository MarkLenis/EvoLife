import pytest
from fastapi.testclient import TestClient

from app.config import Settings
from app.main import create_app


@pytest.fixture
def settings(tmp_path):
    db_path = tmp_path / "test.db"
    return Settings(database_url=f"sqlite:///{db_path}")


@pytest.fixture
def client(settings):
    app = create_app(settings)
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def run_id(client):
    response = client.post(
        "/api/v1/runs",
        json={
            "experiment_name": "baseline_ecosystem",
            "random_seed": 42,
            "configuration": {"map_size": 100},
        },
    )
    assert response.status_code == 201
    return response.json()["run_id"]
