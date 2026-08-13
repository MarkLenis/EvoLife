def test_submit_creature_records(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/creatures",
        json={
            "records": [
                {
                    "creature_id": "herb-001",
                    "species": "herbivore",
                    "generation": 3,
                    "birth_time": 12.5,
                    "death_time": 88.0,
                    "cause_of_death": "predation",
                    "parent_id_1": "herb-010",
                    "offspring_count": 2,
                    "genome_traits": {"speed": 1.4, "vision": 16.0},
                }
            ]
        },
    )
    assert response.status_code == 201
    payload = response.json()
    assert payload["inserted"] == 1
    assert payload["records"][0]["creature_id"] == "herb-001"


def test_creature_record_validation(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/creatures",
        json={"records": []},
    )
    assert response.status_code == 422
