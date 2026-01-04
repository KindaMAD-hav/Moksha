using System.Collections;
using UnityEngine;

/// <summary>
/// Handles dissolve effect for enemies on death.
/// Creates material instances to avoid affecting other enemies with shared materials.
/// </summary>
public class EnemyDissolve : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [SerializeField] private float dissolveDuration = 1.5f;
    [SerializeField] private float dissolveStartValue = 0f;
    [SerializeField] private float dissolveEndValue = 1f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Timing")]
    [SerializeField] private float delayBeforeDissolve = 0.2f;
    
    [Header("Shader Property")]
    [Tooltip("The shader property name for dissolve offset. This shader uses a Vector3.")]
    [SerializeField] private string dissolvePropertyName = "_DissolveOffest"; // Note: typo in shader
    [Tooltip("Which axis to animate: 0=X, 1=Y, 2=Z")]
    [SerializeField] private int dissolveAxis = 1; // Y axis by default
    
    // Cached
    private Renderer[] renderers;
    private Material[][] materialInstances; // Per-renderer material arrays
    private int dissolvePropertyID;
    private bool hasInitialized;
    private bool isDissolving;
    private Coroutine dissolveCoroutine;
    private Vector4 dissolveVector; // Reusable vector to avoid allocations

    private void Awake()
    {
        dissolvePropertyID = Shader.PropertyToID(dissolvePropertyName);
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        materialInstances = new Material[renderers.Length][];
        hasInitialized = true;
    }

    /// <summary>
    /// Creates material instances for this enemy (call once when enemy spawns or on first dissolve).
    /// This ensures modifying materials doesn't affect other enemies.
    /// </summary>
    public void CreateMaterialInstances()
    {
        if (!hasInitialized) CacheRenderers();
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            
            // Using .materials creates instances automatically (not sharedMaterials)
            materialInstances[i] = renderers[i].materials;
        }
    }

    /// <summary>
    /// Starts the dissolve effect. Call this when the enemy dies.
    /// </summary>
    public void StartDissolve(System.Action onComplete = null)
    {
        if (isDissolving) return;
        
        // Ensure we have material instances
        if (materialInstances == null || materialInstances[0] == null)
        {
            CreateMaterialInstances();
        }
        
        if (dissolveCoroutine != null)
            StopCoroutine(dissolveCoroutine);
            
        dissolveCoroutine = StartCoroutine(DissolveRoutine(onComplete));
    }

    private IEnumerator DissolveRoutine(System.Action onComplete)
    {
        isDissolving = true;
        
        // Optional delay before starting dissolve
        if (delayBeforeDissolve > 0f)
            yield return new WaitForSeconds(delayBeforeDissolve);
        
        float elapsed = 0f;
        
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = dissolveCurve.Evaluate(elapsed / dissolveDuration);
            float dissolveValue = Mathf.Lerp(dissolveStartValue, dissolveEndValue, t);
            
            SetDissolveValue(dissolveValue);
            
            yield return null;
        }
        
        // Ensure final value
        SetDissolveValue(dissolveEndValue);
        
        isDissolving = false;
        onComplete?.Invoke();
    }

    private void SetDissolveValue(float value)
    {
        // Set the appropriate axis of the vector
        dissolveVector = Vector4.zero;
        dissolveVector[dissolveAxis] = value;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (materialInstances[i] == null) continue;
            
            for (int j = 0; j < materialInstances[i].Length; j++)
            {
                if (materialInstances[i][j] != null && materialInstances[i][j].HasProperty(dissolvePropertyID))
                {
                    materialInstances[i][j].SetVector(dissolvePropertyID, dissolveVector);
                }
            }
        }
    }

    /// <summary>
    /// Resets the dissolve effect (call when enemy is recycled from pool).
    /// </summary>
    public void ResetDissolve()
    {
        if (dissolveCoroutine != null)
        {
            StopCoroutine(dissolveCoroutine);
            dissolveCoroutine = null;
        }
        
        isDissolving = false;
        SetDissolveValue(dissolveStartValue);
    }

    /// <summary>
    /// Cleans up material instances to prevent memory leaks.
    /// Call when enemy is destroyed (not pooled).
    /// </summary>
    public void CleanupMaterials()
    {
        if (materialInstances == null) return;
        
        for (int i = 0; i < materialInstances.Length; i++)
        {
            if (materialInstances[i] == null) continue;
            
            for (int j = 0; j < materialInstances[i].Length; j++)
            {
                if (materialInstances[i][j] != null)
                {
                    Destroy(materialInstances[i][j]);
                }
            }
        }
        
        materialInstances = null;
    }

    private void OnDestroy()
    {
        CleanupMaterials();
    }
}
