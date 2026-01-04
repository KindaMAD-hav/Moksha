using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace CoreBit.DestructibleSprite
{
    [DisallowMultipleComponent]
    public class BreakableItem : MonoBehaviour
    {
        [Header("Parts & Components")]
        [SerializeField] private BreakableItemLayer[] layers;

        [Tooltip("Number of hits required before the object breaks.")]
        [SerializeField] private int hitsToBreak = 1;

        [Tooltip("Strength of the explosion force applied to the parts.")]
        [SerializeField] private float explosionForce = 20f;

        [Tooltip("Disable collision between all parts after breaking.")]
        [SerializeField] private bool ignoreInternalCollisions = false;

        [Tooltip("Direction in which the parts will mainly explode.")]
        [SerializeField] private BreakDirection breakDirection = BreakDirection.Up;

        [Tooltip("If true, the parts will fade out after breaking.")]
        [SerializeField] private bool enableFadeOut = false;

        [Tooltip("Time before fade out starts (seconds).")]
        [SerializeField] private float fadeOutDelay = 2f;

        [Tooltip("How long the fade out takes (seconds).")]
        [SerializeField] private float fadeOutDuration = 1f;

        [Tooltip("If true, the object can reset itself after breaking.")]
        [SerializeField] private bool enableReset = false;

        [Tooltip("Time (in seconds) before the object resets itself.")]
        [SerializeField] private float resetDelay = 5f;

        [SerializeField] private UnityEvent onBreak;

        [SerializeField][HideInInspector] private SpriteRenderer mainSpriteRenderer;
        private Vector3[] partPositions;
        private Quaternion[] partQuaternions;

        // runtime state
        private int currentHits = 0;
        private bool isBroken = false;

        // public accessors
        public int HitsToBreak { set => hitsToBreak = value; }
        public float ExplosionForce { set => explosionForce = value; }
        public bool IgnoreInternalCollisions { set => ignoreInternalCollisions = value; }
        public BreakDirection BreakDirection { set => breakDirection = value; }
        public bool EnableFadeOut { set => enableFadeOut = value; }
        public float FadeOutDelay { set => fadeOutDelay = value; }
        public float FadeOutDuration { set => fadeOutDuration = value; }
        public bool EnableReset { set => enableReset = value; }
        public float ResetDelay { set => resetDelay = value; }

        public SpriteRenderer MainSpriteRenderer { set => mainSpriteRenderer = value; }
        public BreakableItemLayer[] Layers { set => layers = value; }


        private void Awake()
        {
            if (ignoreInternalCollisions && layers != null)
            {
                foreach (var layerA in layers)
                {
                    foreach (var layerB in layers)
                    {
                        if (layerA != layerB)
                            Physics2D.IgnoreCollision(layerA.Collider, layerB.Collider);
                    }
                }
            }

            if (enableReset)
            {
                partPositions = new Vector3[layers.Length];
                partQuaternions = new Quaternion[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    partPositions[i] = layers[i].Collider.transform.position;
                    partQuaternions[i] = layers[i].Collider.transform.rotation;
                }
            }
        }

        public void Break(Transform instigator = null) // instigator
        {
            if (isBroken) return;

            currentHits++;
            if (currentHits < hitsToBreak) return;

            isBroken = true;
            onBreak?.Invoke();

            ActivatePhysics(instigator);
            EnableColliders();

            if (enableFadeOut)
                StartCoroutine(FadeOutSprites());

            if (enableReset)
                StartCoroutine(ResetItem());
        }

        private void ActivatePhysics(Transform instigator)
        {
            mainSpriteRenderer.gameObject.SetActive(false);
            foreach (var layer in layers)
            {
                if (layer.Rigidbody2D == null) continue;

                layer.Rigidbody2D.gameObject.SetActive(true);
                layer.Rigidbody2D.simulated = true;

                Vector2 direction;

                switch (breakDirection)
                {
                    case BreakDirection.Up:
                        direction = new Vector2(Random.Range(-explosionForce, explosionForce),
                                                Random.Range(explosionForce * 0.5f, explosionForce));
                        break;

                    case BreakDirection.Down:
                        direction = new Vector2(Random.Range(-explosionForce, explosionForce),
                                                Random.Range(-explosionForce, -explosionForce * 0.5f));
                        break;

                    case BreakDirection.Left:
                        direction = new Vector2(Random.Range(-explosionForce, -explosionForce * 0.5f),
                                                Random.Range(-explosionForce * 0.5f, explosionForce * 0.5f));
                        break;

                    case BreakDirection.Right:
                        direction = new Vector2(Random.Range(explosionForce * 0.5f, explosionForce),
                                                Random.Range(-explosionForce * 0.5f, explosionForce * 0.5f));
                        break;

                    case BreakDirection.None:
                        if (instigator == null)
                        {
                            direction = new Vector2(Random.Range(-explosionForce, explosionForce),
                                                    Random.Range(0, explosionForce));
                        }
                        else
                        {
                            direction = (instigator.position.x > transform.position.x)
                                        ? new Vector2(Random.Range(-explosionForce, 0), Random.Range(0, explosionForce))
                                        : new Vector2(Random.Range(0, explosionForce), Random.Range(0, explosionForce));
                        }
                        break;

                    default:
                        direction = Vector2.zero;
                        break;
                }

                layer.Rigidbody2D.AddForceAtPosition(
                    direction * (1f / layer.Mass),
                    layer.Rigidbody2D.position,
                    ForceMode2D.Impulse
                );
            }
        }

        private void EnableColliders()
        {
            foreach (var layer in layers)
            {
                if (layer.Collider != null)
                    layer.Collider.enabled = true;
            }
        }

        private IEnumerator FadeOutSprites()
        {
            foreach (var layer in layers)
            {
                if (layer.SpriteRenderer != null)
                    StartCoroutine(FadeOutCoroutine(layer.SpriteRenderer, fadeOutDuration, fadeOutDelay));
            }
            yield return new WaitForSeconds(fadeOutDelay);
            foreach (var layer in layers)
            {
                if (layer.Rigidbody2D != null)
                    layer.Rigidbody2D.simulated = false;

                if (layer.Collider != null)
                    layer.Collider.enabled = false;
            }

        }

        private IEnumerator FadeOutCoroutine(SpriteRenderer sr, float duration, float delay)
        {
            if (sr == null) yield break;

            yield return new WaitForSeconds(delay);

            Color originalColor = sr.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / duration);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }

        private IEnumerator ResetItem()
        {
            yield return new WaitForSeconds(resetDelay);

            mainSpriteRenderer.gameObject.SetActive(true);
            currentHits = 0;
            isBroken = false;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].Rigidbody2D != null)
                {
                    layers[i].Rigidbody2D.simulated = false;
                    layers[i].Rigidbody2D.gameObject.SetActive(false);
                    layers[i].Rigidbody2D.transform.position = partPositions[i];
                    layers[i].Rigidbody2D.transform.rotation = partQuaternions[i];
                }

                if (layers[i].Collider != null)
                    layers[i].Collider.enabled = false;
            }
        }
    }

    public enum BreakDirection { Left, Right, Up, Down, None }

}