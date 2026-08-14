using UnityEngine;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Destroys a generated mesh with its GameObject so presentation discs do not leak.
    /// </summary>
    public sealed class PresentationMeshOwner : MonoBehaviour
    {
        Mesh owned;

        public void Own(Mesh mesh) => owned = mesh;

        void OnDestroy()
        {
            if (owned == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(owned);
            }
            else
            {
                DestroyImmediate(owned);
            }

            owned = null;
        }
    }
}
