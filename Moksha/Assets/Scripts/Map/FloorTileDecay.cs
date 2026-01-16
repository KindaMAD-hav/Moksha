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

    private MeshFilter meshFilter;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
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
        if (stage.mesh != null)
            meshFilter.sharedMesh = stage.mesh;

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
            meshFilter = GetComponent<MeshFilter>();
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
