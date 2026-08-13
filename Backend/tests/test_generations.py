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


def test_list_generations_and_upsert(client, run_id):
    first = client.post(
        f"/api/v1/runs/{run_id}/generations",
        json={
            "summaries": [
                {
                    "species": "herbivore",
                    "generation": 0,
                    "population_count": 20,
                    "average_genome_traits": {"base_movement_speed": 2.0},
                    "average_lifespan": 10.0,
                    "extra_statistics": {"trait_variance": {"base_movement_speed": 0.1}},
                }
            ]
        },
    )
    assert first.status_code == 201

    second = client.post(
        f"/api/v1/runs/{run_id}/generations",
        json={
            "summaries": [
                {
                    "species": "herbivore",
                    "generation": 0,
                    "population_count": 18,
                    "average_genome_traits": {"base_movement_speed": 2.2},
                    "average_lifespan": 12.0,
                    "extra_statistics": {
                        "trait_variance": {"base_movement_speed": 0.2},
                        "by_policy": {
                            "learned_ppo": {
                                "population_count": 8,
                                "average_genome_traits": {"base_movement_speed": 2.4},
                                "trait_variance": {"base_movement_speed": 0.05},
                            }
                        },
                    },
                }
            ]
        },
    )
    assert second.status_code == 201

    listed = client.get(f"/api/v1/runs/{run_id}/generations")
    assert listed.status_code == 200
    assert listed.json()["total"] == 1
    assert listed.json()["summaries"][0]["population_count"] == 18
    assert listed.json()["summaries"][0]["average_genome_traits"]["base_movement_speed"] == 2.2

    evolution = client.get(
        f"/api/v1/runs/{run_id}/trait-evolution",
        params={"trait": "base_movement_speed"},
    )
    assert evolution.status_code == 200
    assert evolution.json()["points"][0]["mean"] == 2.2
    assert evolution.json()["points"][0]["variance"] == 0.2

    ppo = client.get(
        f"/api/v1/runs/{run_id}/trait-evolution",
        params={"trait": "base_movement_speed", "policy_kind": "learned_ppo"},
    )
    assert ppo.json()["points"][0]["mean"] == 2.4
    assert ppo.json()["points"][0]["population_count"] == 8
