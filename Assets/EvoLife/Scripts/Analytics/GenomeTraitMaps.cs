using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Analytics
{
    public static class GenomeTraitMaps
    {
        public static Dictionary<string, float> Copy(IReadOnlyGenomeTraits traits)
        {
            var map = new Dictionary<string, float>();
            if (traits == null)
            {
                return map;
            }

            for (var i = 0; i < traits.TraitCount; i++)
            {
                var name = traits.GetTraitName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    map[name] = traits.GetTraitValue(i);
                }
            }

            return map;
        }
    }
}
