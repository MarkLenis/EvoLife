def test_create_and_finish_run(client, run_id):
    response = client.post(f"/api/v1/runs/{run_id}/finish", json={"status": "completed"})
    assert response.status_code == 200
    assert response.json()["status"] == "completed"


def test_invalid_run_id_returns_404(client):
    response = client.get("/api/v1/runs/not-a-real-run-id")
    assert response.status_code == 404
