using UnityEngine;
using UnityEngine.Rendering;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Low-poly primitive helpers. Colliders are stripped from visual children so
    /// AI/selection continue to use the authored root collider.
    /// </summary>
    public static class PresentationPrimitives
    {
        public const string VisualRootName = "Visual";

        public static Transform EnsureVisualRoot(Transform owner)
        {
            if (owner == null)
            {
                return null;
            }

            var existing = owner.Find(VisualRootName);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(VisualRootName);
            go.transform.SetParent(owner, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        public static GameObject CreateChild(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            StripCollider(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            ApplySharedMaterial(go, material);
            return go;
        }

        public static void ApplySharedMaterial(GameObject go, Material material)
        {
            if (go == null)
            {
                return;
            }

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        public static void StripCollider(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            Object.DestroyImmediate(collider);
        }
    }
}
