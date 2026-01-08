using UnityEngine;

namespace CoreBit.DestructibleSprite
{
    [System.Serializable]
    public class BreakableItemLayer
    {
        [SerializeField] private Collider2D collider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rigidbody2D;
        [SerializeField] private float mass;

        public Collider2D Collider { get => collider; set => collider = value; }
        public SpriteRenderer SpriteRenderer { get => spriteRenderer; set => spriteRenderer = value; }
        public Rigidbody2D Rigidbody2D { get => rigidbody2D; set => rigidbody2D = value; }
        public float Mass { get => mass; set => mass = value; }
    }
}
