def test_submit_generation_summaries(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/generations",
        json={
            "summaries": [
                {
                    "species": "herbivore",
                    "generation": 1,
                    "population_count": 40,
                    "average_genome_traits": {"speed": 1.1},
                }
            ]
        },
    )
    assert response.status_code == 201
    assert response.json()["inserted"] == 1


def test_evolution_time_series(client, run_id):
    client.post(
        f"/api/v1/runs/{run_id}/generations",
        json={
            "summaries": [
                {
                    "species": "herbivore",
                    "generation": 1,
                    "population_count": 40,
                    "average_genome_traits": {"speed": 1.1},
                },
                {
                    "species": "herbivore",
                    "generation": 2,
                    "population_count": 38,
                    "average_genome_traits": {"speed": 1.3},
                },
            ]
        },
    )

    response = client.get(f"/api/v1/runs/{run_id}/evolution-series")
    assert response.status_code == 200
    assert len(response.json()["points"]) == 2
