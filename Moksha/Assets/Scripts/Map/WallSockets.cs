using UnityEngine;

public class WallSockets : MonoBehaviour
{
    public Transform start;
    public Transform end;

    public float Length => Vector3.Distance(start.position, end.position);
}
