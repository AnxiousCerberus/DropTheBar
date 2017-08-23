using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters
{
    #region Inspector Customization
    //Basic moves Params
    [Header("Moves acceleration")]
    [SerializeField]
    float accelerationTimeAir = .2f;
    [SerializeField]
    float accelerationTimeGrounded = .1f;
    #endregion

    #region Status Vars
    bool jump = false;
    public Vector2 playerWindDirection = Vector2.zero;

    //Misc state
    bool climbingDropDownPlatform = false;
    bool droppingDropDownPlatform = false;
    #endregion

    #region Processing Vars
    [HideInInspector]
    public Vector3 _moveDirection;
    float velocityXSmoothing;
    Vector3 input;

    //Hehehe...
    Transform mdr;
    #endregion

    private void Start()
    {
        //Collisions & Physics base calculation
        CalculateRaySpacing();
    }

    private void Update()
    {
        UpdateAnimator();

        //TODO : Normalize wind direction, maybe? Not sure how we can do this with other forces but...


        //Walkin' and strollin' (but not on the beach) + some other basic moves
        float targetVelocityX = input.x * speed;

        jump = Input.GetButtonDown("Jump");
        Debug.Log("Above = " + collisions.above + " Below = " + collisions.below + " Jump = " + jump);

        if (playerWindDirection == Vector2.zero)
        {
            //Neutralizing Y moves when grounded, or head hitting ceiling or dashing
            if (collisions.above || collisions.below)
            {
                _moveDirection.y = 0;
            }

            //Regular Jump
            if (jump && collisions.below)
            {
                Debug.Log("Jumped");
                _moveDirection.y = calculatedJumpForce;
                jumping = true;
            }

            _moveDirection.x = Input.GetAxisRaw("Horizontal");

            //Applying Gravity
            if (GravityOn)
                _moveDirection.y += calculatedGravity * Time.deltaTime;
        }
        else
        {
            _moveDirection = playerWindDirection;
        }

        //And finally, let's call the final method that will process collision before moving the player =D
        ApplyMoveAndCollisions(_moveDirection * Time.deltaTime);
    }

  
    #region Misc Methods
    void UpdateAnimator()
    {
       /* animator.SetBool("Crouching", crouching);
        animator.SetBool("Dashing", dashing);
        animator.SetFloat("OutSpeed", Mathf.Abs(_moveDirection.x));
        animator.SetBool("Grounded", collisions.below);
        animator.SetFloat("InputDir", Mathf.Abs(Input.GetAxisRaw("Horizontal")));
        animator.SetFloat("DashDuration", dashDuration / 100);
        */
    }

    void CancelJump()
    {
        jumping = false;
        _moveDirection.y = 0f;
    }
    #endregion
}
