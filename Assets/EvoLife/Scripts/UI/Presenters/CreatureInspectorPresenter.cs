using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EvoLife.Common;

namespace EvoLife.UI
{
    /// <summary>
    /// Read-only bundle the inspector presenter consumes. Callers gather Common contracts
    /// from the selected host; this type does not reach into private fields.
    /// </summary>
    public sealed class SelectedCreatureSnapshot
    {
        public bool HasSelection;
        public bool HostDestroyed;
        public ICreatureIdentity Identity;
        public IReadOnlyVitalState Vitals;
        public ICreatureLineage Lineage;
        public IPolicyKindOwner Policy;
        public IReadOnlyGenomeTraits Genome;
        public IEpisodeMetrics Episode;
        public IReadOnlyCreatureActivity Activity;
        public IReadOnlyCreatureAiDebug AiDebug;
        public int? LivingOffspringCount;
        public string ExperimentModelId;
    }

    public sealed class NamedTraitDisplay
    {
        public string Name;
        public string Value;
        public bool Found;
    }

    public sealed class CreatureInspectorModel
    {
        public bool HasSelection;
        public string EmptyReason;
        public string CreatureId;
        public string Species;
        public string Role;
        public string Generation;
        public string ParentA;
        public string ParentB;
        public string OffspringCount;
        public string PolicyKind;
        public string PolicyWireName;
        public string ModelId;
        public string Alive;
        public string DeathCause;
        public string Age;
        public string Health;
        public string Hunger;
        public string Thirst;
        public string Energy;
        public string CurrentActivity;
        public NamedTraitDisplay[] Traits;
        public string ControlMode;
        public string BehaviorName;
        public string Forward;
        public string Turn;
        public string SprintOrEffort;
        public string InteractionRequest;
        public string EpisodeReturn;
        public string ScriptedMotive;
        public string SummaryText;
    }

    /// <summary>
    /// Transforms selected-creature contracts into inspector display strings.
    /// </summary>
    public static class CreatureInspectorPresenter
    {
        public const string NoSelection = "No creature selected.";
        public const string SelectionCleared = "Selection cleared.";
        public const string Unavailable = "unavailable";

        public static CreatureInspectorModel Build(SelectedCreatureSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.HasSelection || snapshot.HostDestroyed)
            {
                var empty = Empty(snapshot != null && snapshot.HostDestroyed
                    ? SelectionCleared
                    : NoSelection);
                return empty;
            }

            var model = new CreatureInspectorModel
            {
                HasSelection = true,
                EmptyReason = "",
                CreatureId = snapshot.Identity != null
                    ? snapshot.Identity.Id.ToString()
                    : Unavailable,
                Species = snapshot.Identity != null
                    ? NullToUnavailable(snapshot.Identity.SpeciesId)
                    : Unavailable,
                Role = snapshot.Identity != null
                    ? CreatureRoleNames.ToWireName(snapshot.Identity.Role)
                    : Unavailable,
                Generation = snapshot.Lineage != null
                    ? snapshot.Lineage.Generation.ToString(CultureInfo.InvariantCulture)
                    : Unavailable,
                ParentA = FormatParent(snapshot.Lineage != null ? snapshot.Lineage.ParentA : null),
                ParentB = FormatParent(snapshot.Lineage != null ? snapshot.Lineage.ParentB : null),
                OffspringCount = snapshot.LivingOffspringCount.HasValue
                    ? snapshot.LivingOffspringCount.Value.ToString(CultureInfo.InvariantCulture)
                    : Unavailable,
                PolicyKind = PolicyDisplayFormatter.FormatKind(snapshot.Policy),
                PolicyWireName = snapshot.Policy != null
                    ? PolicyDisplayFormatter.FormatWireName(snapshot.Policy.PolicyKind)
                    : Unavailable,
                ModelId = snapshot.Policy != null && snapshot.Policy.PolicyKind == AgentPolicyKind.LearnedPpo
                    ? PolicyDisplayFormatter.FormatModelId(snapshot.ExperimentModelId)
                    : Unavailable,
                Alive = FormatAlive(snapshot.Vitals),
                DeathCause = FormatDeathCause(snapshot.Vitals),
                Age = FormatPair(snapshot.Vitals?.Age, snapshot.Vitals?.MaxAge),
                Health = FormatPair(snapshot.Vitals?.Health, snapshot.Vitals?.MaxHealth),
                Hunger = FormatPair(snapshot.Vitals?.Hunger, snapshot.Vitals?.MaxHunger),
                Thirst = FormatPair(snapshot.Vitals?.Thirst, snapshot.Vitals?.MaxThirst),
                Energy = FormatPair(snapshot.Vitals?.Energy, snapshot.Vitals?.MaxEnergy),
                CurrentActivity = snapshot.Activity != null
                    ? NullToUnavailable(snapshot.Activity.CurrentActivity)
                    : Unavailable,
                Traits = BuildTraits(snapshot.Genome),
                ControlMode = snapshot.AiDebug != null
                    ? NullToUnavailable(snapshot.AiDebug.ControlMode)
                    : Unavailable,
                BehaviorName = snapshot.AiDebug != null
                    ? NullToUnavailable(snapshot.AiDebug.BehaviorName)
                    : Unavailable,
                Forward = snapshot.AiDebug != null
                    ? snapshot.AiDebug.Forward.ToString("0.00", CultureInfo.InvariantCulture)
                    : Unavailable,
                Turn = snapshot.AiDebug != null
                    ? snapshot.AiDebug.Turn.ToString("0.00", CultureInfo.InvariantCulture)
                    : Unavailable,
                SprintOrEffort = snapshot.AiDebug != null
                    ? snapshot.AiDebug.SprintOrEffort.ToString("0.00", CultureInfo.InvariantCulture)
                    : Unavailable,
                InteractionRequest = snapshot.AiDebug != null
                    ? NullToUnavailable(snapshot.AiDebug.InteractionRequest)
                    : Unavailable,
                EpisodeReturn = FormatEpisodeReturn(snapshot.Episode),
                ScriptedMotive = FormatMotive(snapshot.AiDebug)
            };
            model.SummaryText = BuildSummary(model);
            return model;
        }

        public static int CountLivingOffspring(
            CreatureId parentId,
            IReadOnlyList<IAnalyticsCreatureView> liveViews)
        {
            if (liveViews == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < liveViews.Count; i++)
            {
                var lineage = liveViews[i]?.Lineage;
                if (lineage == null)
                {
                    continue;
                }

                if ((lineage.ParentA.HasValue && lineage.ParentA.Value == parentId)
                    || (lineage.ParentB.HasValue && lineage.ParentB.Value == parentId))
                {
                    count++;
                }
            }

            return count;
        }

        public static NamedTraitDisplay[] BuildTraits(IReadOnlyGenomeTraits genome)
        {
            var names = CanonicalTraitNames.InSchemaOrder;
            var result = new NamedTraitDisplay[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                float value = 0f;
                var found = genome != null && genome.TryGetTrait(names[i], out value);
                result[i] = new NamedTraitDisplay
                {
                    Name = names[i],
                    Found = found,
                    Value = found ? value.ToString("0.###", CultureInfo.InvariantCulture) : Unavailable
                };
            }

            return result;
        }

        static CreatureInspectorModel Empty(string reason)
        {
            return new CreatureInspectorModel
            {
                HasSelection = false,
                EmptyReason = reason,
                SummaryText = reason,
                Traits = BuildTraits(null),
                CreatureId = Unavailable,
                Species = Unavailable,
                Role = Unavailable,
                Generation = Unavailable,
                ParentA = Unavailable,
                ParentB = Unavailable,
                OffspringCount = Unavailable,
                PolicyKind = Unavailable,
                PolicyWireName = Unavailable,
                ModelId = Unavailable,
                Alive = Unavailable,
                DeathCause = Unavailable,
                Age = Unavailable,
                Health = Unavailable,
                Hunger = Unavailable,
                Thirst = Unavailable,
                Energy = Unavailable,
                CurrentActivity = Unavailable,
                ControlMode = Unavailable,
                BehaviorName = Unavailable,
                Forward = Unavailable,
                Turn = Unavailable,
                SprintOrEffort = Unavailable,
                InteractionRequest = Unavailable,
                EpisodeReturn = Unavailable,
                ScriptedMotive = Unavailable
            };
        }

        static string FormatAlive(IReadOnlyVitalState vitals)
        {
            if (vitals == null)
            {
                return Unavailable;
            }

            return vitals.IsAlive ? "alive" : "dead";
        }

        static string FormatDeathCause(IReadOnlyVitalState vitals)
        {
            if (vitals == null)
            {
                return Unavailable;
            }

            if (vitals.IsAlive || !vitals.CauseOfDeath.HasValue)
            {
                return vitals.IsAlive ? "—" : DeathCauseNames.ToWireName(DeathCause.Unknown);
            }

            return DeathCauseNames.ToWireName(vitals.CauseOfDeath.Value);
        }

        static string FormatPair(float? current, float? max)
        {
            if (!current.HasValue || !max.HasValue)
            {
                return Unavailable;
            }

            return current.Value.ToString("0.##", CultureInfo.InvariantCulture)
                + " / "
                + max.Value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        static string FormatParent(CreatureId? parent)
        {
            if (!parent.HasValue)
            {
                return "—";
            }

            return parent.Value.ToString();
        }

        static string FormatEpisodeReturn(IEpisodeMetrics episode)
        {
            if (episode == null)
            {
                return Unavailable;
            }

            if (!episode.HasEpisodeReturn)
            {
                return Unavailable;
            }

            return episode.EpisodeReturn.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static string FormatMotive(IReadOnlyCreatureAiDebug debug)
        {
            if (debug == null || !debug.HasScriptedMotive)
            {
                return Unavailable;
            }

            return NullToUnavailable(debug.ScriptedMotive);
        }

        static string NullToUnavailable(string value) =>
            string.IsNullOrEmpty(value) ? Unavailable : value;

        static string BuildSummary(CreatureInspectorModel model)
        {
            var builder = new StringBuilder(512);
            builder.AppendLine("IDENTITY");
            builder.AppendLine("  ID: " + model.CreatureId);
            builder.AppendLine("  Species: " + model.Species);
            builder.AppendLine("  Role: " + model.Role);
            builder.AppendLine("  Generation: " + model.Generation);
            builder.AppendLine("  Parent A: " + model.ParentA);
            builder.AppendLine("  Parent B: " + model.ParentB);
            builder.AppendLine("  Offspring (living): " + model.OffspringCount);
            builder.AppendLine("  Policy: " + model.PolicyKind + " (" + model.PolicyWireName + ")");
            if (model.ModelId != Unavailable)
            {
                builder.AppendLine("  PPO model: " + model.ModelId);
            }

            builder.AppendLine("BIOLOGY");
            builder.AppendLine("  State: " + model.Alive);
            builder.AppendLine("  Death cause: " + model.DeathCause);
            builder.AppendLine("  Age: " + model.Age);
            builder.AppendLine("  Health: " + model.Health);
            builder.AppendLine("  Hunger: " + model.Hunger);
            builder.AppendLine("  Thirst: " + model.Thirst);
            builder.AppendLine("  Energy: " + model.Energy);
            builder.AppendLine("  Activity: " + model.CurrentActivity);
            builder.AppendLine("GENETICS");
            if (model.Traits != null)
            {
                for (var i = 0; i < model.Traits.Length; i++)
                {
                    var trait = model.Traits[i];
                    builder.AppendLine("  " + trait.Name + ": " + trait.Value);
                }
            }

            builder.AppendLine("AI / DECISION");
            builder.AppendLine("  Control: " + model.ControlMode);
            builder.AppendLine("  Behavior: " + model.BehaviorName);
            builder.AppendLine("  Forward: " + model.Forward);
            builder.AppendLine("  Turn: " + model.Turn);
            builder.AppendLine("  Sprint/effort: " + model.SprintOrEffort);
            builder.AppendLine("  Interaction: " + model.InteractionRequest);
            builder.AppendLine("  Episode return: " + model.EpisodeReturn);
            builder.AppendLine("  Baseline motive: " + model.ScriptedMotive);
            return builder.ToString();
        }
    }
}
