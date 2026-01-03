using UnityEngine;

public class characterstateanimation : MonoBehaviour
{

    Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
        bool isRunning = animator.GetBool("isRunning");

        if (!isRunning && Input.GetKeyDown(KeyCode.W))
        {
            animator.SetBool("isRunning", true);
        }
        if (isRunning && Input.GetKeyUp(KeyCode.W))
        {
            animator.SetBool("isRunning", false);
        }
        
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    animator.SetTrigger("isJumping");
        //}
    }
}
