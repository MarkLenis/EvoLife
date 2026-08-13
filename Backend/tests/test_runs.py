def test_create_and_finish_run(client, run_id):
    response = client.post(f"/api/v1/runs/{run_id}/finish", json={"status": "completed"})
    assert response.status_code == 200
    assert response.json()["status"] == "completed"


def test_finish_run_records_stop_reason_metadata(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/finish",
        json={"status": "completed", "stop_reason": "max_simulation_time"},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "completed"
    assert body["metadata"]["stop_reason"] == "max_simulation_time"


def test_invalid_run_id_returns_404(client):
    response = client.get("/api/v1/runs/not-a-real-run-id")
    assert response.status_code == 404
