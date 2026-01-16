using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FloorTileDecay : MonoBehaviour
{
    [Header("Decay Data")]
    [SerializeField] private TileDecayData decayData;

    [Header("Runtime State")]
    [SerializeField] private float decayValue;
    [SerializeField] private int currentStageIndex;

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
    }

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
