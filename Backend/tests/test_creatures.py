def test_submit_creature_records(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/creatures",
        json={
            "records": [
                {
                    "creature_id": "herb-001",
                    "species": "herbivore",
                    "generation": 3,
                    "birth_time": 12.5,
                    "death_time": 88.0,
                    "cause_of_death": "predation",
                    "genome_traits": {"speed": 1.4},
                }
            ]
        },
    )
    assert response.status_code == 201
    assert response.json()["inserted"] == 1
