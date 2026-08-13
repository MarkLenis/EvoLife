using UnityEngine;

namespace EvoLife.UI
{
    /// <summary>
    /// Optional selection seam. Adds a fallback collider when visual prefabs have none.
    /// Does not change AI sensing. Presentation agents should prefer existing colliders.
    /// </summary>
    public sealed class CreatureSelectable : MonoBehaviour
    {
        [SerializeField] bool addFallbackColliderIfMissing = true;
        [SerializeField] float fallbackRadius = 0.75f;

        void Awake()
        {
            if (!addFallbackColliderIfMissing)
            {
                return;
            }

            if (GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = fallbackRadius;
            sphere.isTrigger = true;
        }
    }
}
