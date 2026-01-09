using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Handles dissolve effect for enemies on death.
/// Creates material instances to avoid affecting other enemies with shared materials.
/// Optimized to avoid GC allocations during dissolve.
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
    private Material[][] materialInstances;
    private bool[][] hasDissolveProperty; // Cache which materials have the property
    private int dissolvePropertyID;
    private bool hasInitialized;
    private bool hasMaterialInstances;
    
    // Dissolve state (non-coroutine based)
    private bool isDissolving;
    private float dissolveTimer;
    private float delayTimer;
    private bool inDelayPhase;
    private Action onCompleteCallback;
    
    // Reusable vector
    private Vector4 dissolveVector;

    private void Awake()
    {
        dissolvePropertyID = Shader.PropertyToID(dissolvePropertyName);
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        materialInstances = new Material[renderers.Length][];
        hasDissolveProperty = new bool[renderers.Length][];
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
            Material[] mats = renderers[i].materials;
            materialInstances[i] = mats;
            
            // Cache which materials have the dissolve property
            hasDissolveProperty[i] = new bool[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                hasDissolveProperty[i][j] = mats[j] != null && mats[j].HasProperty(dissolvePropertyID);
            }
        }
        
        hasMaterialInstances = true;
    }

    /// <summary>
    /// Starts the dissolve effect. Call this when the enemy dies.
    /// Uses Update loop instead of coroutine to avoid GC allocations.
    /// </summary>
    public void StartDissolve(Action onComplete = null)
    {
        if (isDissolving) return;
        
        // Ensure we have material instances
        if (!hasMaterialInstances)
        {
            CreateMaterialInstances();
        }
        
        onCompleteCallback = onComplete;
        dissolveTimer = 0f;
        delayTimer = 0f;
        inDelayPhase = delayBeforeDissolve > 0f;
        isDissolving = true;
        enabled = true; // Enable Update
    }

    private void Update()
    {
        if (!isDissolving) return;
        
        float dt = Time.deltaTime;
        
        // Handle delay phase
        if (inDelayPhase)
        {
            delayTimer += dt;
            if (delayTimer >= delayBeforeDissolve)
            {
                inDelayPhase = false;
            }
            return;
        }
        
        // Dissolve phase
        dissolveTimer += dt;
        float t = dissolveTimer / dissolveDuration;
        
        if (t >= 1f)
        {
            // Complete
            SetDissolveValue(dissolveEndValue);
            isDissolving = false;
            enabled = false; // Disable Update
            
            onCompleteCallback?.Invoke();
            onCompleteCallback = null;
        }
        else
        {
            float curveT = dissolveCurve.Evaluate(t);
            float value = dissolveStartValue + (dissolveEndValue - dissolveStartValue) * curveT;
            SetDissolveValue(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetDissolveValue(float value)
    {
        // Set the appropriate axis of the vector
        dissolveVector.x = dissolveAxis == 0 ? value : 0f;
        dissolveVector.y = dissolveAxis == 1 ? value : 0f;
        dissolveVector.z = dissolveAxis == 2 ? value : 0f;
        dissolveVector.w = 0f;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = materialInstances[i];
            if (mats == null) continue;
            
            bool[] hasProp = hasDissolveProperty[i];
            for (int j = 0; j < mats.Length; j++)
            {
                // Use cached property check
                if (hasProp[j])
                {
                    mats[j].SetVector(dissolvePropertyID, dissolveVector);
                }
            }
        }
    }

    /// <summary>
    /// Resets the dissolve effect (call when enemy is recycled from pool).
    /// </summary>
    public void ResetDissolve()
    {
        isDissolving = false;
        inDelayPhase = false;
        dissolveTimer = 0f;
        delayTimer = 0f;
        onCompleteCallback = null;
        enabled = false;
        
        if (hasMaterialInstances)
        {
            SetDissolveValue(dissolveStartValue);
        }
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
            Material[] mats = materialInstances[i];
            if (mats == null) continue;
            
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null)
                {
                    Destroy(mats[j]);
                }
            }
        }
        
        materialInstances = null;
        hasDissolveProperty = null;
        hasMaterialInstances = false;
    }

    private void OnDestroy()
    {
        CleanupMaterials();
    }
    
    private void OnDisable()
    {
        // Don't run Update when disabled
    }
}
