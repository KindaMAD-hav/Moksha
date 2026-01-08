/// <summary>
/// Theme-friendly "damage" interface:
/// Attacks apply purification to trapped souls.
/// </summary>
public interface IPurifiable
{
    void Purify(float amount);
}
