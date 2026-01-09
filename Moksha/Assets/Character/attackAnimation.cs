using UnityEngine;

public class AttackAnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("Attack Layer")]
    [SerializeField] private int attackLayerIndex = 1;

    [Header("Attack States (Exact Names)")]
    [SerializeField] private string attack1State = "Attack1";
    [SerializeField] private string attack2State = "Attack2";

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetLayerWeight(attackLayerIndex, 1f);
    }

    /// <summary>
    /// Call this ONCE PER BULLET FIRED
    /// </summary>
    public void PlayRandomAttack()
    {
        string state = Random.value < 0.5f ? attack1State : attack2State;
        Debug.Log($"Playing {state} at time {Time.time}");
        animator.Play(state, attackLayerIndex, 0f);
        animator.Update(0f); // forces immediate restart
    }
}
