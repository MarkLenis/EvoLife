def test_submit_and_list_creature_records_with_policy(client, run_id):
    response = client.post(
        f"/api/v1/runs/{run_id}/creatures",
        json={
            "records": [
                {
                    "creature_id": "1",
                    "species": "herb",
                    "generation": 0,
                    "birth_time": 0.0,
                    "death_time": 12.0,
                    "cause_of_death": "starvation",
                    "genome_traits": {"base_movement_speed": 2.0},
                    "policy_kind": "scripted_baseline",
                    "extra_fields": {"lifetime": 12.0, "role": "herbivore"},
                },
                {
                    "creature_id": "2",
                    "species": "herb",
                    "generation": 0,
                    "birth_time": 0.0,
                    "death_time": 20.0,
                    "cause_of_death": "predation",
                    "genome_traits": {"base_movement_speed": 2.4},
                    "policy_kind": "learned_ppo",
                    "extra_fields": {"lifetime": 20.0, "episode_return": 1.5, "role": "herbivore"},
                },
            ]
        },
    )
    assert response.status_code == 201
    assert response.json()["inserted"] == 2

    listed = client.get(f"/api/v1/runs/{run_id}/creatures", params={"policy_kind": "learned_ppo"})
    assert listed.status_code == 200
    assert listed.json()["total"] == 1
    assert listed.json()["records"][0]["creature_id"] == "2"
    assert listed.json()["records"][0]["policy_kind"] == "learned_ppo"


def test_legacy_creature_payload_without_policy_kind(client, run_id):
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
    listed = client.get(f"/api/v1/runs/{run_id}/creatures")
    assert listed.json()["total"] == 1
    assert listed.json()["records"][0]["policy_kind"] is None


def test_survival_and_policy_comparison(client, run_id):
    client.post(
        f"/api/v1/runs/{run_id}/creatures",
        json={
            "records": [
                {
                    "creature_id": "s1",
                    "species": "herb",
                    "generation": 1,
                    "birth_time": 0.0,
                    "death_time": 10.0,
                    "cause_of_death": "starvation",
                    "policy_kind": "scripted_baseline",
                    "genome_traits": {"aggression": 0.2},
                    "extra_fields": {"lifetime": 10.0},
                },
                {
                    "creature_id": "p1",
                    "species": "herb",
                    "generation": 1,
                    "birth_time": 0.0,
                    "death_time": 30.0,
                    "cause_of_death": "old_age",
                    "policy_kind": "learned_ppo",
                    "genome_traits": {"aggression": 0.4},
                    "extra_fields": {"lifetime": 30.0, "episode_return": 2.0},
                },
            ]
        },
    )

    survival = client.get(f"/api/v1/runs/{run_id}/survival", params={"policy_kind": "learned_ppo"})
    assert survival.status_code == 200
    assert survival.json()["total"] == 1
    assert survival.json()["records"][0]["lifetime"] == 30.0

    comparison = client.get(f"/api/v1/runs/{run_id}/policy-comparison")
    assert comparison.status_code == 200
    groups = {item["policy_kind"]: item for item in comparison.json()["groups"]}
    assert groups["scripted_baseline"]["mean_lifetime"] == 10.0
    assert groups["learned_ppo"]["mean_lifetime"] == 30.0
    assert groups["learned_ppo"]["mean_episode_return"] == 2.0
    assert comparison.json()["total_creatures"] == 2


def test_empty_policy_comparison(client, run_id):
    response = client.get(f"/api/v1/runs/{run_id}/policy-comparison")
    assert response.status_code == 200
    assert response.json()["groups"] == []
    assert response.json()["total_creatures"] == 0
