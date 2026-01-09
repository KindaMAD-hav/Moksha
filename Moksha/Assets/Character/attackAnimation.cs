using System.Collections;
using UnityEngine;

public class AttackLayerController : MonoBehaviour
{
    private Animator animator;

    [Tooltip("Layer index that contains the Attack 1 state")]
    public int attackLayerIndex = 1;
    [Tooltip("State name in the Animator (exact) to play on attack 1 layer")]
    public string attackStateName = "Attack";

    [Tooltip("Layer index that contains the Attack 2 state (right-click)")]
    public int attack2LayerIndex = 1;
    [Tooltip("State name in the Animator (exact) to play on attack 2 layer")]
    public string attack2StateName = "Attack2";

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

    void Update()
    {
        if (!isAttacking && Input.GetMouseButtonDown(0))
        {
            StartAttack(attackLayerIndex, attackStateName);
        }
        else if (!isAttacking && Input.GetMouseButtonDown(1))
        {
            StartAttack(attack2LayerIndex, attack2StateName);
        }
    }

    void StartAttack(int layerIndex, string stateName)
    {
        if (!IsLayerValid(layerIndex))
            return;

        isAttacking = true;
        activeLayerIndex = layerIndex;

        animator.SetLayerWeight(layerIndex, 1f);
        animator.Play(stateName, layerIndex, 0f);
        animator.Update(0f); // sample immediately

        if (useFallbackTimeout)
        {
            if (endCoroutine != null) StopCoroutine(endCoroutine);
            endCoroutine = StartCoroutine(WaitAndEndAttack(layerIndex));
        }
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

        isAttacking = false;
        activeLayerIndex = -1;
    }

    IEnumerator WaitAndEndAttack(int layerIndex)
    {
        // Wait one frame for the animator to update
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
