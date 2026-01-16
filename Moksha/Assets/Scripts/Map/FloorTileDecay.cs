using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FloorTileDecay : MonoBehaviour
{
    [Header("Decay Data")]
    [SerializeField] private TileDecayData decayData;

    [Header("Runtime State")]
    [SerializeField] private float decayValue;
    [SerializeField] private int currentStageIndex;

    public System.Action<FloorTileDecay> OnCriticalDecayReached;
    private bool collapseSignaled;

    private Renderer tileRenderer;
    private MaterialPropertyBlock propertyBlock;

    // Store ALL original materials from prefab to restore on reset
    // Serialized so it persists when cloned
    [SerializeField, HideInInspector] private Material[] originalMaterials;
    [SerializeField, HideInInspector] private bool hasStoredOriginal;

    // Which material index to swap for decay stages (usually 0)
    private const int DECAY_MATERIAL_INDEX = 0;


    private void Awake()
    {
        tileRenderer = GetComponentInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        // Capture ALL original materials on FIRST Awake only (from prefab)
        // hasStoredOriginal is serialized, so clones will already have it set to true
        if (!hasStoredOriginal && tileRenderer != null)
        {
            originalMaterials = tileRenderer.sharedMaterials;
            hasStoredOriginal = true;

            // Store the FIRST tile's materials as the template in FloorManager
            // This captures the correct materials from the scene (first floor)
            // FloorManager persists across all floors, so this works for floors 2, 3, 4, etc.
            if (FloorManager.Instance != null && !FloorManager.Instance.HasTemplateMaterials)
            {
                FloorManager.Instance.SetTemplateMaterials(originalMaterials);
            }
        }

        ApplyDecayVisuals(force: true);
    }

    /// <summary>
    /// Gets the template materials from FloorManager, or falls back to stored originals.
    /// </summary>
    private Material[] GetTemplateMaterials()
    {
        if (FloorManager.Instance != null && FloorManager.Instance.HasTemplateMaterials)
        {
            return FloorManager.Instance.GetTemplateMaterials();
        }
        return originalMaterials;
    }

    /// <summary>
    /// Resets the tile to its pristine state. Call this on cloned floors.
    /// Uses the template materials captured from the first floor.
    /// </summary>
    public void ResetDecay()
    {
        decayValue = 0f;
        currentStageIndex = 0;
        collapseSignaled = false;

        // Ensure we have a renderer reference
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<Renderer>();

        // Use template materials from FloorManager if available, otherwise use stored originals
        Material[] materialsToApply = GetTemplateMaterials();

        if (tileRenderer != null && materialsToApply != null && materialsToApply.Length > 0)
        {
            // Create a copy of the materials array to avoid modifying the template
            Material[] newMats = new Material[materialsToApply.Length];
            for (int i = 0; i < materialsToApply.Length; i++)
            {
                newMats[i] = materialsToApply[i];
            }
            tileRenderer.sharedMaterials = newMats;

            // Also update our stored originals to match the template
            originalMaterials = newMats;
        }

        // Reset property block
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        if (tileRenderer != null)
        {
            tileRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_DecayStage", 0);
            tileRenderer.SetPropertyBlock(propertyBlock);
        }

        // Don't call ApplyDecayVisuals here - we already set the materials
        // ApplyDecayVisuals would try to apply stage 0 material which might be null
    }

    /// <summary>
    /// Forces the tile to use the template materials from FloorManager.
    /// Call this on newly instantiated tiles from prefabs.
    /// </summary>
    public void ApplyTemplateMaterials()
    {
        Material[] templateMats = GetTemplateMaterials();
        
        if (templateMats == null || templateMats.Length == 0)
            return;

        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<Renderer>();

        if (tileRenderer == null)
            return;

        // Create a copy of the template materials
        Material[] newMats = new Material[templateMats.Length];
        for (int i = 0; i < templateMats.Length; i++)
        {
            newMats[i] = templateMats[i];
        }
        
        tileRenderer.sharedMaterials = newMats;
        originalMaterials = newMats;
        hasStoredOriginal = true;
    }

    /// <summary>
    /// Stores the current materials as the originals. Call before any decay happens.
    /// </summary>
    public void CaptureOriginalMaterials()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<Renderer>();

        if (tileRenderer != null)
        {
            originalMaterials = tileRenderer.sharedMaterials;
            hasStoredOriginal = true;
        }
    }


    public void AddDecay(float amount)
    {
        decayValue += amount;
        ApplyDecayVisuals();
    }

    public void SetDecay(float value)
    {
        decayValue = value;
        ApplyDecayVisuals();
    }

    private void ApplyDecayVisuals(bool force = false)
    {
        if (decayData == null || decayData.stages == null || decayData.stages.Length == 0)
            return;

        int newStage = decayData.GetStageIndex(decayValue);

        if (!force && newStage == currentStageIndex)
            return;

        currentStageIndex = newStage;

        var stage = decayData.stages[currentStageIndex];
        if (stage.material != null && tileRenderer != null)
        {
            tileRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_DecayStage", currentStageIndex);
            tileRenderer.SetPropertyBlock(propertyBlock);

            // Build material array: slot 0 = decay stage material, other slots = from template or originals
            Material[] currentMats = tileRenderer.sharedMaterials;
            if (currentMats != null && currentMats.Length > 0)
            {
                // Set decay material at slot 0
                currentMats[DECAY_MATERIAL_INDEX] = stage.material;
                
                // CRITICAL: Preserve other material slots from template (fixes prefab material issues)
                // Use template from FloorManager if available, otherwise use stored originals
                Material[] sourceForOtherSlots = GetTemplateMaterials();
                
                if (sourceForOtherSlots != null)
                {
                    for (int i = 1; i < currentMats.Length && i < sourceForOtherSlots.Length; i++)
                    {
                        currentMats[i] = sourceForOtherSlots[i];
                    }
                }
                
                tileRenderer.sharedMaterials = currentMats;
            }
            else
            {
                // Fallback: if only one material slot, just set it
                tileRenderer.sharedMaterial = stage.material;
            }
        }


        if (!collapseSignaled && decayData.IsCriticalStage(currentStageIndex))
        {
            collapseSignaled = true;
            OnCriticalDecayReached?.Invoke(this);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test / Force Critical")]
    private void TestForceCritical()
    {
        if (decayData == null || decayData.stages.Length == 0)
            return;

        decayValue = decayData.stages[^1].decayThreshold;
        ApplyDecayVisuals(force: true);
    }
#endif


#if UNITY_EDITOR
    [Header("Editor Testing")]
    [SerializeField] private bool testControls;
    [SerializeField] private float testDecayStep = 1f;

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            tileRenderer = GetComponentInChildren<Renderer>();
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            ApplyDecayVisuals(force: true);
        }
    }

    [ContextMenu("Test / Increase Decay")]
    private void TestIncreaseDecay()
    {
        AddDecay(testDecayStep);
    }

    [ContextMenu("Test / Reset Decay")]
    private void TestResetDecay()
    {
        decayValue = 0f;
        ApplyDecayVisuals(force: true);
    }
#endif
}
