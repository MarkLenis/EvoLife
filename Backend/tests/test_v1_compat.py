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


def test_v1_stats_accepts_optional_analytics_fields(client):
    created = client.post("/api/v1/experiments", json={"name": "rich-v1", "seed": 7})
    experiment_id = created.json()["id"]

    posted = client.post(
        "/api/v1/stats",
        json={
            "experimentId": experiment_id,
            "simulationTimeSeconds": 4.0,
            "herbivoreCount": 9,
            "predatorCount": 1,
            "totalAlive": 10,
            "timestampUtcUnix": 2.0,
            "births": 11,
            "deaths": 1,
            "scriptedAlive": 6,
            "ppoAlive": 4,
        },
    )
    assert posted.status_code == 200
    listed = client.get("/api/v1/stats", params={"experiment_id": experiment_id})
    assert listed.json()[0]["births"] == 11
    assert listed.json()[0]["scriptedAlive"] == 6

    series = client.get(f"/api/v1/runs/{experiment_id}/population-series")
    assert series.json()["points"][0]["births"] == 11
    assert series.json()["points"][0]["extra_metrics"]["ppo_alive"] == 4
