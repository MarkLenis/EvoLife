def test_v1_experiment_visible_as_run(client):
    created = client.post(
        "/api/v1/experiments",
        json={"name": "linked", "seed": 99},
    )
    experiment_id = created.json()["id"]

    run = client.get(f"/api/v1/runs/{experiment_id}")
    assert run.status_code == 200
    assert run.json()["experiment_name"] == "linked"
    assert run.json()["random_seed"] == 99


def test_v1_stats_visible_in_population_series(client):
    created = client.post("/api/v1/experiments", json={"name": "series"})
    experiment_id = created.json()["id"]

    client.post(
        "/api/v1/stats",
        json={
            "experimentId": experiment_id,
            "simulationTimeSeconds": 5.0,
            "herbivoreCount": 20,
            "predatorCount": 5,
            "totalAlive": 25,
            "timestampUtcUnix": 1.0,
        },
    )

    series = client.get(f"/api/v1/runs/{experiment_id}/population-series")
    assert series.status_code == 200
    assert len(series.json()["points"]) == 1
    assert series.json()["points"][0]["herbivore_population"] == 20
