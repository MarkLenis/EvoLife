using UnityEngine;
using UnityEngine.EventSystems;
using EvoLife.Common;

namespace EvoLife.UI
{
    /// <summary>
    /// Desktop click/raycast selection. Uses existing colliders when present.
    /// </summary>
    public sealed class CreatureSelectionController : MonoBehaviour
    {
        [SerializeField] Camera raycastCamera;
        [SerializeField] LayerMask selectableLayers = ~0;
        [SerializeField] float maxDistance = 500f;
        [SerializeField] KeyCode deselectKey = KeyCode.Escape;

        readonly CreatureSelectionState state = new CreatureSelectionState();
        readonly System.Collections.Generic.List<IAnalyticsCreatureView> liveBuffer =
            new System.Collections.Generic.List<IAnalyticsCreatureView>(64);

        ILiveCreatureCatalog catalog;
        string experimentModelId;

        public CreatureSelectionState State => state;

        public Transform SelectedTransform { get; private set; }

        public void BindCatalog(ILiveCreatureCatalog liveCatalog) => catalog = liveCatalog;

        public void SetExperimentModelId(string modelId) => experimentModelId = modelId;

        void Awake()
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }
        }

        void Update()
        {
            if (state.Host == null && state.Snapshot.HasSelection)
            {
                SelectedTransform = null;
                state.Clear();
            }

            if (Input.GetKeyDown(deselectKey))
            {
                SelectedTransform = null;
                state.Clear();
                return;
            }

            if (!Input.GetMouseButtonDown(0) || IsPointerOverUi())
            {
                return;
            }

            TrySelectUnderCursor();
        }

        public void TrySelectUnderCursor()
        {
            var cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, maxDistance, selectableLayers))
            {
                SelectedTransform = null;
                state.Clear();
                return;
            }

            var identity = hit.collider.GetComponentInParent<ICreatureIdentity>();
            if (identity == null)
            {
                SelectedTransform = null;
                state.Clear();
                return;
            }

            var root = ResolveHost(hit.collider, identity);
            if (root == null)
            {
                SelectedTransform = null;
                state.Clear();
                return;
            }

            SelectedTransform = root.transform;
            state.Select(root, BuildSnapshot(root, identity));
        }

        static GameObject ResolveHost(Collider collider, ICreatureIdentity identity)
        {
            if (collider == null)
            {
                return null;
            }

            var behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            if (behaviours != null)
            {
                for (var i = 0; i < behaviours.Length; i++)
                {
                    if (ReferenceEquals(behaviours[i], identity))
                    {
                        return behaviours[i].gameObject;
                    }
                }
            }

            return collider.transform.root != null ? collider.transform.root.gameObject : collider.gameObject;
        }

        public SelectedCreatureSnapshot BuildSnapshot(GameObject root, ICreatureIdentity identity)
        {
            int? offspring = null;
            if (identity != null && catalog != null)
            {
                liveBuffer.Clear();
                catalog.CopyLiveViews(liveBuffer);
                offspring = CreatureInspectorPresenter.CountLivingOffspring(identity.Id, liveBuffer);
            }

            return new SelectedCreatureSnapshot
            {
                HasSelection = root != null,
                HostDestroyed = root == null,
                Identity = identity ?? (root != null ? root.GetComponent<ICreatureIdentity>() : null),
                Vitals = root != null ? root.GetComponent<IReadOnlyVitalState>() : null,
                Lineage = root != null ? root.GetComponent<ICreatureLineage>() : null,
                Policy = root != null ? root.GetComponent<IPolicyKindOwner>() : null,
                Genome = root != null ? root.GetComponent<IReadOnlyGenomeTraits>() : null,
                Episode = root != null ? root.GetComponent<IEpisodeMetrics>() : null,
                Activity = root != null ? root.GetComponent<IReadOnlyCreatureActivity>() : null,
                AiDebug = root != null ? root.GetComponent<IReadOnlyCreatureAiDebug>() : null,
                LivingOffspringCount = offspring,
                ExperimentModelId = experimentModelId
            };
        }

        public SelectedCreatureSnapshot RefreshSnapshot()
        {
            if (SelectedTransform == null)
            {
                return state.Snapshot;
            }

            var root = SelectedTransform.gameObject;
            var snapshot = BuildSnapshot(root, root.GetComponent<ICreatureIdentity>());
            state.UpdateSnapshot(snapshot);
            return snapshot;
        }

        static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
