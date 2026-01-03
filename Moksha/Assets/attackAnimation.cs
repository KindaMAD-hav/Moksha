using System.Collections;
using UnityEngine;

public class AttackLayerController : MonoBehaviour
{
    Animator animator;

    [Tooltip("Layer index that contains the Attack state")]
    public int attackLayerIndex = 1;
    [Tooltip("State name in the Animator (exact) to play on attack layer")]
    public string attackStateName = "Attack";

    public bool useFallbackTimeout = true;

    bool isAttacking = false;
    Coroutine endCoroutine = null;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("AttackLayerController: no Animator found on the GameObject.");
            enabled = false;
            return;
        }

        // Ensure layer is invisible by default
        animator.SetLayerWeight(attackLayerIndex, 0f);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;

        // 1. Set weight to 1 FIRST
        animator.SetLayerWeight(attackLayerIndex, 1f);

        // 2. Force play the attack state from the beginning (normalized time 0)
        animator.Play(attackStateName, attackLayerIndex, 0f);

        // 3. Force the animator to evaluate immediately so the state change takes effect
        animator.Update(0f);

        // Start fallback that ends the attack automatically
        if (useFallbackTimeout)
        {
            if (endCoroutine != null) StopCoroutine(endCoroutine);
            endCoroutine = StartCoroutine(WaitAndEndAttack());
        }
    }

    public void EndAttack()
    {
        if (endCoroutine != null)
        {
            StopCoroutine(endCoroutine);
            endCoroutine = null;
        }

        animator.SetLayerWeight(attackLayerIndex, 0f);
        isAttacking = false;
    }

    IEnumerator WaitAndEndAttack()
    {
        // Wait one frame for the animator to update
        yield return null;

        var clips = animator.GetCurrentAnimatorClipInfo(attackLayerIndex);
        if (clips == null || clips.Length == 0)
        {
            yield return new WaitForSeconds(0.5f);
            EndAttack();
            yield break;
        }

        var clip = clips[0].clip;
        if (clip == null || clip.isLooping)
        {
            yield return new WaitForSeconds(0.5f);
            EndAttack();
            yield break;
        }

        // Wait for the clip duration
        yield return new WaitForSeconds(clip.length);

        if (isAttacking)
            EndAttack();
    }
}
