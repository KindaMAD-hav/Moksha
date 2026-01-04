using UnityEngine;
namespace CoreBit.DestructibleSprite
{
    public class TestExplosion : MonoBehaviour
    {
        [SerializeField] private ParticleSystem explosionParticle;
        [SerializeField] private float explosionTime;
        [SerializeField] private float explosionRadius;
        [SerializeField] private LayerMask layer;
        void Start()
        {
            Invoke(nameof(Explosion), explosionTime);
        }

        private void Explosion()
        {
            BreakableItem breakableItem = null;
            explosionParticle.Play();
            var colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, layer);

            foreach (var item in colliders)
            {
                Debug.Log("Break - " + item.name);

                item.TryGetComponent<BreakableItem>(out breakableItem);
                if (breakableItem != null)
                {
                    Debug.Log("Break");
                    breakableItem.Break(transform);
                }
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
