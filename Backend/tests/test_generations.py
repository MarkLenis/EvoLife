def test_submit_generation_summaries(client, run_id):
    response = client.post(
        f"/api/runs/{run_id}/generations",
        json={
            "summaries": [
                {
                    "species": "herbivore",
                    "generation": 1,
                    "population_count": 40,
                    "average_genome_traits": {"speed": 1.1, "vision": 14.0},
                    "average_lifespan": 95.0,
                    "reproduction_rate": 0.35,
                },
                {
                    "species": "predator",
                    "generation": 1,
                    "population_count": 10,
                    "average_genome_traits": {"speed": 2.0, "vision": 18.0},
                    "average_lifespan": 110.0,
                },
            ]
        },
    )
    assert response.status_code == 201
    payload = response.json()
    assert payload["inserted"] == 2


def test_evolution_time_series(client, run_id):
    client.post(
        f"/api/runs/{run_id}/generations",
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

    response = client.get(f"/api/runs/{run_id}/evolution-series")
    assert response.status_code == 200
    payload = response.json()
    assert len(payload["points"]) == 2
    assert payload["points"][1]["average_genome_traits"]["speed"] == 1.3


def test_evolution_series_filter_by_species(client, run_id):
    client.post(
        f"/api/runs/{run_id}/generations",
        json={
            "summaries": [
                {
                    "species": "herbivore",
                    "generation": 1,
                    "population_count": 40,
                },
                {
                    "species": "predator",
                    "generation": 1,
                    "population_count": 10,
                },
            ]
        },
    )

    response = client.get(f"/api/runs/{run_id}/evolution-series", params={"species": "predator"})
    assert response.status_code == 200
    points = response.json()["points"]
    assert len(points) == 1
    assert points[0]["species"] == "predator"
