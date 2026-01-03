using UnityEngine;

[CreateAssetMenu(menuName = "MOKSHA/ProcGen/Module Set")]
public class ModuleSet : ScriptableObject
{
    [Header("Wall Segments (all same length)")]
    public GameObject[] wallStraightVariants;

    [Header("Floor Tiles (all same size)")]
    public GameObject[] floorTileVariants;

    [Header("Sizing (match your prefab footprint)")]
    public float wallSegmentLength = 2f;   // distance between consecutive wall segments
    public float floorTileSize = 2f;       // tile grid spacing (x/z)

    [Header("Randomization")]
    public bool avoidSameVariantTwice = true;
}
