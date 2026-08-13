def test_submit_snapshot(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/snapshots",
        json={
            "simulation_time": 10.0,
            "herbivore_population": 42,
            "predator_population": 8,
            "plant_count": 300,
            "births": 3,
            "deaths": 1,
            "average_herbivore_speed": 1.2,
            "average_predator_speed": 2.1,
            "average_vision": 15.5,
        },
    )
    assert response.status_code == 201
    payload = response.json()
    assert payload["run_id"] == run_id
    assert payload["herbivore_population"] == 42


def test_batch_submit_snapshots(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/snapshots/batch",
        json={
            "snapshots": [
                {
                    "simulation_time": 0.0,
                    "herbivore_population": 50,
                    "predator_population": 10,
                    "plant_count": 400,
                },
                {
                    "simulation_time": 5.0,
                    "herbivore_population": 48,
                    "predator_population": 11,
                    "plant_count": 390,
                    "average_lifespan": 120.5,
                },
            ]
        },
    )
    assert response.status_code == 201
    payload = response.json()
    assert payload["inserted"] == 2
    assert len(payload["snapshots"]) == 2


def test_population_time_series(client, run_id):
    client.post(
        f"/api/runs/{run_id}/snapshots/batch",
        json={
            "snapshots": [
                {
                    "simulation_time": 0.0,
                    "herbivore_population": 50,
                    "predator_population": 10,
                    "plant_count": 400,
                },
                {
                    "simulation_time": 10.0,
                    "herbivore_population": 45,
                    "predator_population": 12,
                    "plant_count": 380,
                },
            ]
        },
    )

    response = client.get(f"/api/runs/{run_id}/population-series")
    assert response.status_code == 200
    payload = response.json()
    assert payload["run_id"] == run_id
    assert len(payload["points"]) == 2
    assert payload["points"][0]["simulation_time"] == 0.0
    assert payload["points"][1]["herbivore_population"] == 45


def test_snapshot_validation_rejects_negative_population(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/snapshots",
        json={
            "simulation_time": 1.0,
            "herbivore_population": -1,
            "predator_population": 1,
            "plant_count": 1,
        },
    )
    assert response.status_code == 422


def test_snapshot_for_invalid_run_returns_404(client):
    response = client.post(
        "/api/runs/missing-run/snapshots",
        json={
            "simulation_time": 1.0,
            "herbivore_population": 1,
            "predator_population": 1,
            "plant_count": 1,
        },
    )
    assert response.status_code == 404
