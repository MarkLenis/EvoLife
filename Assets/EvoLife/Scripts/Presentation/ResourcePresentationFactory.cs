using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Builds plant and water presentation templates used by <see cref="ResourceManager"/>.
    /// </summary>
    public static class ResourcePresentationFactory
    {
        public static GameObject CreatePlantTemplate(Transform parent = null)
        {
            var go = new GameObject("Plant");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.AddComponent<PlantResource>();
            go.AddComponent<PlantPresentation>().EnsureVisuals();
            return go;
        }

        public static GameObject CreateWaterTemplate(Transform parent = null)
        {
            var go = new GameObject("WaterSource");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.AddComponent<WaterSource>();
            go.AddComponent<WaterPresentation>().EnsureVisuals();
            return go;
        }
    }
}
