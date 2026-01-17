using UnityEngine;

[CreateAssetMenu(
    fileName = "TileDecayData",
    menuName = "Moksha/Decay/Tile Decay Data"
)]
public class TileDecayData : ScriptableObject
{
    [System.Serializable]
    public class DecayStage
    {
        [Tooltip("Material used for this decay stage")]
        public Material material;

        [Tooltip("Minimum decay value required to enter this stage")]
        public float decayThreshold;
    }


    [Tooltip("Decay stages must be ordered from lowest to highest threshold")]
    public DecayStage[] stages;

    public int GetStageIndex(float decayValue)
    {
        int result = 0;

        for (int i = 0; i < stages.Length; i++)
        {
            if (decayValue >= stages[i].decayThreshold)
                result = i;
            else
                break;
        }

        return result;
    }
    public bool IsCriticalStage(int stageIndex)
    {
        return stageIndex == stages.Length - 1;
    }

}
