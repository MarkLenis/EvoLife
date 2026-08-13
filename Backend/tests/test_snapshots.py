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
    assert response.json()["points"][0]["total_alive"] == 60


def test_snapshot_with_births_and_policy_counts(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/snapshots",
        json={
            "simulation_time": 3.0,
            "herbivore_population": 10,
            "predator_population": 2,
            "births": 14,
            "deaths": 2,
            "extra_metrics": {
                "scripted_alive": 8,
                "ppo_alive": 4,
                "population_change": -1,
                "max_generation": 2,
            },
        },
    )
    assert response.status_code == 201

    series = client.get(f"/api/v1/runs/{run_id}/population-series")
    point = series.json()["points"][0]
    assert point["births"] == 14
    assert point["deaths"] == 2
    assert point["total_alive"] == 12
    assert point["extra_metrics"]["ppo_alive"] == 4


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
