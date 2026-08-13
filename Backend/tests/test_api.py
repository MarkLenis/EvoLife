def test_health(client) -> None:
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] == "ok"


def test_create_experiment_and_post_stats(client) -> None:
    created = client.post(
        "/api/v1/experiments",
        json={
            "name": "smoke",
            "policy_herbivore": "scripted_baseline",
            "policy_predator": "learned_ppo",
            "seed": 1,
        },
    )
    assert created.status_code == 200
    experiment_id = created.json()["id"]

    stats = client.post(
        "/api/v1/stats",
        json={
            "experimentId": experiment_id,
            "simulationTimeSeconds": 12.5,
            "herbivoreCount": 10,
            "predatorCount": 2,
            "totalAlive": 12,
            "timestampUtcUnix": 1_700_000_000.0,
        },
    )
    assert stats.status_code == 200
    assert stats.json()["herbivoreCount"] == 10

    listed = client.get("/api/v1/stats", params={"experiment_id": experiment_id})
    assert listed.status_code == 200
    assert len(listed.json()) == 1
