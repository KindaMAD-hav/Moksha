using System.Collections;
using UnityEngine;

public class AttackLayerController : MonoBehaviour
{
    private Animator animator;

    [Tooltip("Layer index that contains the Attack 1 state")]
    public int attackLayerIndex = 1;
    [Tooltip("State name in the Animator (exact) to play on attack 1 layer")]
    public string attackStateName = "Attack";

    [Tooltip("Layer index that contains the Attack 2 state")]
    public int attack2LayerIndex = 1;
    [Tooltip("State name in the Animator (exact) to play on attack 2 layer")]
    public string attack2StateName = "Attack2";

    [Tooltip("If true, will try to auto-end the attack by clip length.")]
    public bool useFallbackTimeout = true;

    private bool isAttacking = false;
    private Coroutine endCoroutine = null;
    private int activeLayerIndex = -1;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("AttackLayerController: no Animator found on the GameObject.");
            enabled = false;
            return;
        }

        // Ensure both layers are invisible by default
        SafeSetLayerWeight(attackLayerIndex, 0f);
        if (attack2LayerIndex != attackLayerIndex)
            SafeSetLayerWeight(attack2LayerIndex, 0f);
    }

    /// <summary>
    /// Call this every time the weapon fires.
    /// Picks Attack 1 or Attack 2 randomly and restarts the state from time 0.
    /// </summary>
    /// <param name="animSpeed">Optional animator speed scaling (1 = normal).</param>
    public void PlayRandomAttack(float animSpeed = 1f)
    {
        // Avoid 'Random is ambiguous' issues by fully qualifying
        bool pickSecond = UnityEngine.Random.Range(0, 2) == 1;
        if (pickSecond) PlayAttack2(animSpeed);
        else PlayAttack1(animSpeed);
    }

    public void PlayAttack1(float animSpeed = 1f) => StartAttack(attackLayerIndex, attackStateName, animSpeed);
    public void PlayAttack2(float animSpeed = 1f) => StartAttack(attack2LayerIndex, attack2StateName, animSpeed);

    void StartAttack(int layerIndex, string stateName, float animSpeed)
    {
        if (!IsLayerValid(layerIndex))
            return;

        // Allow retrigger per shot: stop previous end coroutine and clear previous layer weight
        if (endCoroutine != null)
        {
            StopCoroutine(endCoroutine);
            endCoroutine = null;
        }

        if (IsLayerValid(activeLayerIndex))
            animator.SetLayerWeight(activeLayerIndex, 0f);

        // If using two different layers, ensure the other one is off too
        if (attack2LayerIndex != attackLayerIndex)
        {
            SafeSetLayerWeight(attackLayerIndex, 0f);
            SafeSetLayerWeight(attack2LayerIndex, 0f);
        }

        isAttacking = true;
        activeLayerIndex = layerIndex;

        animator.speed = Mathf.Max(0.01f, animSpeed);

        animator.SetLayerWeight(layerIndex, 1f);
        animator.Play(stateName, layerIndex, 0f);
        animator.Update(0f); // sample immediately

        if (useFallbackTimeout)
            endCoroutine = StartCoroutine(WaitAndEndAttack(layerIndex));
    }

    public void EndAttack()
    {
        if (endCoroutine != null)
        {
            StopCoroutine(endCoroutine);
            endCoroutine = null;
        }

        if (IsLayerValid(activeLayerIndex))
            animator.SetLayerWeight(activeLayerIndex, 0f);

        animator.speed = 1f;

        isAttacking = false;
        activeLayerIndex = -1;
    }

    IEnumerator WaitAndEndAttack(int layerIndex)
    {
        yield return null;

        var clips = animator.GetCurrentAnimatorClipInfo(layerIndex);
        if (clips == null || clips.Length == 0 || clips[0].clip == null || clips[0].clip.isLooping)
        {
            yield return new WaitForSeconds(0.5f);
            EndAttack();
            yield break;
        }

        var clip = clips[0].clip;
        yield return new WaitForSeconds(clip.length);

        if (isAttacking)
            EndAttack();
    }


    bool IsLayerValid(int layerIndex)
    {
        return animator != null && layerIndex >= 0 && layerIndex < animator.layerCount;
    }

    void SafeSetLayerWeight(int layerIndex, float weight)
    {
        if (IsLayerValid(layerIndex))
            animator.SetLayerWeight(layerIndex, weight);
    }
}
