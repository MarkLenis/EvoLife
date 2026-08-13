using EvoLife.Common;
using EvoLife.Genetics;

namespace EvoLife.AI
{
    /// <summary>
    /// Concatenates vitals, role, genetics, and optional local sensors into
    /// <see cref="CreatureObservationSchema"/> v2 order. Does not mutate any source.
    /// </summary>
    public sealed class CompositeObservationSource : IObservationSource
    {
        readonly IReadOnlyVitalState vitals;
        readonly ICreatureIdentity identity;
        readonly Genome genome;
        readonly IResourceProximitySensor resourceSensor;
        readonly ICreatureProximitySensor creatureSensor;
        readonly float[] geneticScratch = new float[CreatureObservationSchema.GeneticCount];

        public CompositeObservationSource(
            IReadOnlyVitalState vitals,
            ICreatureIdentity identity = null,
            Genome genome = null,
            IResourceProximitySensor resourceSensor = null,
            ICreatureProximitySensor creatureSensor = null)
        {
            this.vitals = vitals;
            this.identity = identity;
            this.genome = genome;
            this.resourceSensor = resourceSensor;
            this.creatureSensor = creatureSensor;
        }

        public int ObservationSize => CreatureObservationSchema.Size;

        public void WriteObservations(float[] buffer)
        {
            if (buffer == null || buffer.Length < ObservationSize)
            {
                return;
            }

            WriteVitals(buffer);
            buffer[CreatureObservationSchema.IndexRole] =
                identity != null ? ObservationMath.RoleToObservation(identity.Role) : 0f;

            GeneticObservationProvider.WriteObservations(genome, geneticScratch);
            for (var i = 0; i < CreatureObservationSchema.GeneticCount; i++)
            {
                buffer[CreatureObservationSchema.IndexGenetics + i] = geneticScratch[i];
            }

            if (resourceSensor != null)
            {
                resourceSensor.WriteNearestFood(buffer, CreatureObservationSchema.IndexFood);
                resourceSensor.WriteNearestWater(buffer, CreatureObservationSchema.IndexWater);
            }
            else
            {
                ObservationMath.WriteZeros(
                    buffer,
                    CreatureObservationSchema.IndexFood,
                    CreatureObservationSchema.ResourceCount);
            }

            if (creatureSensor != null)
            {
                creatureSensor.WriteNearestRoles(
                    buffer,
                    CreatureObservationSchema.IndexHerbivore,
                    CreatureObservationSchema.IndexPredator);
            }
            else
            {
                ObservationMath.WriteZeros(
                    buffer,
                    CreatureObservationSchema.IndexHerbivore,
                    CreatureObservationSchema.NearbyCreatureCount);
            }
        }

        void WriteVitals(float[] buffer)
        {
            if (vitals == null)
            {
                ObservationMath.WriteZeros(buffer, 0, CreatureObservationSchema.VitalCount);
                return;
            }

            buffer[CreatureObservationSchema.IndexHealth] =
                ObservationMath.Normalize(vitals.Health, vitals.MaxHealth);
            buffer[CreatureObservationSchema.IndexHunger] =
                ObservationMath.Normalize(vitals.Hunger, vitals.MaxHunger);
            buffer[CreatureObservationSchema.IndexThirst] =
                ObservationMath.Normalize(vitals.Thirst, vitals.MaxThirst);
            buffer[CreatureObservationSchema.IndexEnergy] =
                ObservationMath.Normalize(vitals.Energy, vitals.MaxEnergy);
            buffer[CreatureObservationSchema.IndexAge] =
                ObservationMath.Normalize(vitals.Age, vitals.MaxAge);
        }
    }
}
