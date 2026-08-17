using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Genetics;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Optional visual-only phenotype hooks. Scales the Visual child from body_size.
    /// Root collider / sensing stay unchanged — body_size is not a physics multiplier.
    /// </summary>
    public sealed class PhenotypeVisual : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] bool applyBodySize = true;
        [SerializeField] bool applyGenerationTint;

        CreatureGenome genome;
        CreatureIdentity identity;
        MaterialPropertyBlock propertyBlock;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        Renderer[] renderers = System.Array.Empty<Renderer>();

        MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

        public Transform VisualRoot => visualRoot;
        public float AppliedScale { get; private set; } = 1f;

        void Awake()
        {
            ResolveBindings();
            CacheRenderers();
        }

        void LateUpdate() => Apply();

        public void Apply()
        {
            ResolveBindings();
            if (applyBodySize)
            {
                ApplyScale();
            }

            if (applyGenerationTint)
            {
                ApplyGenerationTint();
            }
        }

        void ResolveBindings()
        {
            if (genome == null)
            {
                genome = GetComponent<CreatureGenome>();
            }

            if (identity == null)
            {
                identity = GetComponent<CreatureIdentity>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform.Find(PresentationPrimitives.VisualRootName);
            }
        }

        void ApplyScale()
        {
            if (visualRoot == null)
            {
                visualRoot = PresentationPrimitives.EnsureVisualRoot(transform);
            }

            if (visualRoot == null)
            {
                AppliedScale = PhenotypeVisualScale.Neutral;
                return;
            }

            var multiplier = genome != null ? genome.BodySizeMultiplier : PhenotypeVisualScale.Neutral;
            AppliedScale = PhenotypeVisualScale.ForBodySize(multiplier);
            visualRoot.localScale = new Vector3(AppliedScale, AppliedScale, AppliedScale);
        }

        void ApplyGenerationTint()
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var generation = identity != null ? identity.Generation : 0;
            var shift = Mathf.Clamp(generation, 0, 8) * 0.03f;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var shared = renderer.sharedMaterial;
                var baseColor = shared != null && shared.HasProperty("_Color")
                    ? shared.GetColor("_Color")
                    : Color.white;
                var tinted = Color.Lerp(baseColor, Color.white, shift);
                var block = PropertyBlock;
                renderer.GetPropertyBlock(block);
                block.SetColor(ColorId, tinted);
                renderer.SetPropertyBlock(block);
            }
        }

        void CacheRenderers()
        {
            if (visualRoot == null)
            {
                renderers = System.Array.Empty<Renderer>();
                return;
            }

            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        }
    }
}
