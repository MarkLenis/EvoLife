using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Closed creature lifetime record. Built by Analytics from read-only contracts.
    /// Food/water/kills are omitted unless a producer later exposes them.
    /// </summary>
    public sealed class CreatureLifetimeRecord
    {
        public string CreatureId;
        public string Species;
        public string Role;
        public string PolicyKind;
        public int Generation;
        public float BirthTime;
        public float DeathTime;
        public float Lifetime;
        public string CauseOfDeath;
        public string ParentId1;
        public string ParentId2;
        public int OffspringCount;
        public Dictionary<string, float> GenomeTraits = new Dictionary<string, float>();
        public bool HasEpisodeReturn;
        public float EpisodeReturn;
        public float EpisodeSurvivalSeconds;
        public int CompletedEpisodeCount;
    }

    public static class CreatureLifetimeFactory
    {
        public static CreatureLifetimeRecord Create(
            IAnalyticsCreatureView view,
            CreatureDeathNotice notice,
            float birthTime,
            float deathTime,
            int offspringCount = 0)
        {
            var identity = view != null ? view.Identity : null;
            var lineage = view != null ? view.Lineage : null;
            var vitals = view != null ? view.Vitals : null;
            var episode = view != null ? view.EpisodeMetrics : null;

            var id = identity != null ? identity.Id : notice.Id;
            var lifetime = vitals != null ? vitals.Age : notice.Age;
            if (lifetime <= 0f && deathTime >= birthTime)
            {
                lifetime = deathTime - birthTime;
            }

            var record = new CreatureLifetimeRecord
            {
                CreatureId = id.Value.ToString(),
                Species = identity != null ? identity.SpeciesId : "unspecified",
                Role = identity != null ? CreatureRoleNames.ToWireName(identity.Role) : CreatureRoleNames.ToWireName(CreatureRole.Herbivore),
                PolicyKind = PolicyClassifier.Classify(view),
                Generation = lineage != null ? lineage.Generation : 0,
                BirthTime = birthTime < 0f ? 0f : birthTime,
                DeathTime = deathTime < 0f ? 0f : deathTime,
                Lifetime = lifetime < 0f ? 0f : lifetime,
                CauseOfDeath = DeathCauseNames.ToWireName(notice.Cause),
                ParentId1 = lineage?.ParentA.HasValue == true ? lineage.ParentA.Value.Value.ToString() : null,
                ParentId2 = lineage?.ParentB.HasValue == true ? lineage.ParentB.Value.Value.ToString() : null,
                OffspringCount = offspringCount < 0 ? 0 : offspringCount,
                GenomeTraits = GenomeTraitMaps.Copy(view != null ? view.GenomeTraits : null),
                HasEpisodeReturn = episode != null && episode.HasEpisodeReturn,
                EpisodeReturn = episode != null && episode.HasEpisodeReturn ? episode.EpisodeReturn : 0f,
                EpisodeSurvivalSeconds = episode != null ? episode.EpisodeSurvivalSeconds : lifetime,
                CompletedEpisodeCount = episode != null ? episode.CompletedEpisodeCount : 0
            };

            return record;
        }
    }
}
