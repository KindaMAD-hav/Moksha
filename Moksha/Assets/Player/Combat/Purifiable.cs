using UnityEngine;

/// <summary>
/// Component form of IPurifiable so projectiles can reliably find it via GetComponentInParent.
/// </summary>
public abstract class Purifiable : MonoBehaviour, IPurifiable
{
    public abstract void Purify(float amount);
}
