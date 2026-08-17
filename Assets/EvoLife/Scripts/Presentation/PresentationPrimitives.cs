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

        public static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        public static GameObject CreateChild(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEulerAngles = default,
            bool castShadows = false,
            bool receiveShadows = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            StripCollider(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEulerAngles);
            go.transform.localScale = localScale;
            ApplySharedMaterial(go, material, castShadows, receiveShadows);
            return go;
        }

        public static GameObject CreateMeshChild(
            Transform parent,
            string name,
            Mesh mesh,
            Vector3 localPosition,
            Material material,
            bool receiveShadows = true,
            bool castShadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            ApplyRenderer(renderer, material, castShadows, receiveShadows);
            var owner = go.AddComponent<PresentationMeshOwner>();
            owner.Own(mesh);
            return go;
        }

        public static void ApplySharedMaterial(
            GameObject go,
            Material material,
            bool castShadows = false,
            bool receiveShadows = false)
        {
            if (go == null)
            {
                return;
            }

            ApplyRenderer(go.GetComponent<MeshRenderer>(), material, castShadows, receiveShadows);
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

        public static void PrepareImportedVisual(GameObject go, bool castShadows)
        {
            if (go == null)
            {
                return;
            }

            var colliders = go.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }

            var animators = go.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                animators[i].enabled = false;
            }

            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        static void ApplyRenderer(MeshRenderer renderer, Material material, bool castShadows, bool receiveShadows)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
