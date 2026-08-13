def test_create_simulation_run(client):
    response = client.post(
        "/api/runs",
        json={
            "experiment_name": "speed_selection",
            "random_seed": 7,
            "configuration": {"ticks_per_generation": 500},
            "metadata": {"unity_version": "2022.3"},
        },
    )
    assert response.status_code == 201
    payload = response.json()
    assert payload["experiment_name"] == "speed_selection"
    assert payload["random_seed"] == 7
    assert payload["status"] == "running"
    assert "run_id" in payload
    assert "started_at" in payload


def test_finish_simulation_run(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/finish",
        json={"status": "completed"},
    )
    assert response.status_code == 200
    payload = response.json()
    assert payload["status"] == "completed"
    assert payload["finished_at"] is not None


def test_get_run_and_history(client, run_id):
    detail = client.get(f"/api/runs/{run_id}")
    assert detail.status_code == 200
    assert detail.json()["run_id"] == run_id
    assert detail.json()["snapshot_count"] == 0

    history = client.get("/api/runs")
    assert history.status_code == 200
    body = history.json()
    assert body["total"] >= 1
    assert any(run["run_id"] == run_id for run in body["runs"])


def test_invalid_run_id_returns_404(client):
    response = client.get("/api/runs/not-a-real-run-id")
    assert response.status_code == 404
    assert "not found" in response.json()["detail"].lower()
