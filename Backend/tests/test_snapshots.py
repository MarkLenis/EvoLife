def test_submit_snapshot(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/snapshots",
        json={
            "simulation_time": 10.0,
            "herbivore_population": 42,
            "predator_population": 8,
            "plant_count": 300,
        },
    )
    assert response.status_code == 201
    assert response.json()["run_id"] == run_id


def test_batch_submit_snapshots(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/snapshots/batch",
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
                },
            ]
        },
    )
    assert response.status_code == 201
    assert response.json()["inserted"] == 2


def test_population_time_series(client, run_id):
    client.post(
        f"/api/v1/runs/{run_id}/snapshots/batch",
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

    response = client.get(f"/api/v1/runs/{run_id}/population-series")
    assert response.status_code == 200
    assert len(response.json()["points"]) == 2


def test_snapshot_validation(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/snapshots",
        json={
            "simulation_time": 1.0,
            "herbivore_population": -1,
            "predator_population": 1,
            "plant_count": 1,
        },
    )
    assert response.status_code == 422
