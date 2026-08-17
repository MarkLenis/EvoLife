using UnityEngine;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Empty Transform anchors for Agent 10 camera presets. No camera controller lives here.
    /// </summary>
    public static class PresentationCameraAnchors
    {
        public const string RootName = "PresentationAnchors";

        public static Transform Ensure(Transform parent = null)
        {
            Transform root = null;
            if (parent != null)
            {
                root = parent.Find(RootName);
            }

            if (root == null)
            {
                var existing = GameObject.Find(RootName);
                root = existing != null ? existing.transform : new GameObject(RootName).transform;
                if (parent != null)
                {
                    root.SetParent(parent, false);
                }
            }

            Place(root, "CameraAnchor_Overview", DemoBiomeLayout.OverviewCameraPosition);
            Place(root, "CameraAnchor_Grassland", new Vector3(0f, 28f, -36f));
            Place(root, "CameraAnchor_ForestEdge", DemoBiomeLayout.ForestCenter + new Vector3(18f, 22f, -24f));
            Place(root, "CameraAnchor_Wetland", DemoBiomeLayout.WetlandCenter + new Vector3(12f, 18f, -18f));
            Place(root, "CameraAnchor_Rocky", DemoBiomeLayout.RockyCenter + new Vector3(-10f, 20f, -22f));
            Place(root, "CameraAnchor_LowAngleDemo", new Vector3(8f, 6f, -18f));
            return root;
        }

        static void Place(Transform root, string name, Vector3 position)
        {
            var t = root.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                t = go.transform;
                t.SetParent(root, false);
            }

            t.position = position;
            t.LookAt(DemoBiomeLayout.OverviewCameraLookAt);
        }
    }
}
