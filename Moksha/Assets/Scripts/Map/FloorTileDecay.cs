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


    private void Awake()
    {
        tileRenderer = GetComponentInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        ApplyDecayVisuals(force: true);
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

            tileRenderer.sharedMaterial = stage.material;
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
