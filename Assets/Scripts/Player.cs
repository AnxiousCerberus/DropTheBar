using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Characters
{
    public Text debugText;

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
    bool onBar = false;
    bool currentEnabled = true;
    enum moveState { InWind, OnBar };
    moveState currentMoveState = moveState.InWind;

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

    #region TouchStatus
    float touchDuration = 0f;
    Vector2 touchStartPos = Vector2.zero;
    Vector2 touchEndPos = Vector2.zero;
    Vector2 touchPreviousPos = Vector2.zero;
    #endregion

    private void Start()
    {
        //Collisions & Physics base calculation
        CalculateRaySpacing();
    }

    int touchReleaseCount = 0;

    private void Update()
    {
        bool simpleTap = false;
        Vector2 swipeDir = Vector2.zero;


        UpdateAnimator();

        //TODO : Normalize wind direction, maybe? Not sure how we can do this with other forces but...

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            touchReleaseCount++;
        }

        debugText.text = "touchCount = " + Input.touchCount + " - touch release count = " + touchReleaseCount + " ";

        if (Input.touchCount > 0)
        {
            touchDuration += Time.deltaTime;

            if (Input.GetTouch(0).phase == TouchPhase.Began)
                touchStartPos = Input.GetTouch(0).position;

            if (touchDuration > 0)
                debugText.text += "touch Duration = " + touchDuration;

            if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                touchEndPos = Input.GetTouch(0).position;

                //Simple tap check
                if (touchDuration <= .35f)
                {
                    if (Vector3.SqrMagnitude(touchStartPos - touchEndPos) < .75f)
                    {
                        debugText.text += " SIMPLE TAP! ";
                        this.GetComponent<SpriteRenderer>().color = Color.red;
                        simpleTap = true;
                    }
                }
                
                //Swipe check
                if(!simpleTap)
                {
                    debugText.text += "SWIPE!";
                    this.GetComponent<SpriteRenderer>().color = Color.blue;

                    swipeDir = (touchEndPos - touchStartPos).normalized;

                }
            }

            if (!simpleTap && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                if (touchPreviousPos != Vector2.zero && onBar)
                {
                    transform.Translate ( (new Vector2 (0, touchPreviousPos.y) - new Vector2 (0,  touchStartPos.y)) * .0001f);
                }

                touchPreviousPos = Input.GetTouch(0).position;
            }
        }
        else
        {
            this.GetComponent<SpriteRenderer>().color = Color.white;
            touchDuration = 0;
        }

        //Bar Behaviour
        if (onBar)
        {
            currentEnabled = false;
            GrabbingBar();

            if (simpleTap)
            {
                onBar = false;
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                debugText.text = "MOVING FINGER! Position = " + Input.GetTouch(0).position + " tapCount = " + Input.GetTouch(0).tapCount;

                //if (Input.GetTouch(0).position)
            }
        }
        else
        {
            currentEnabled = true;
        }

            //Walkin' and strollin' (but not on the beach) + some other basic moves
            float targetVelocityX = input.x * speed;

            jump = Input.GetButtonDown("Jump");
            Debug.Log("Above = " + collisions.above + " Below = " + collisions.below + " Jump = " + jump);

            if (playerWindDirection == Vector2.zero)
            {
                //Regular Jump
                if (jump && collisions.below)
                {
                    Debug.Log("Jumped");
                    _moveDirection.y = calculatedJumpForce;
                    jumping = true;
                }

                _moveDirection.x = Input.GetAxisRaw("Horizontal");

                //Applying Gravity
                if (GravityOn && !onBar)
                    _moveDirection.y += calculatedGravity * Time.deltaTime;
            }
            else if (currentEnabled)
            {
                _moveDirection = playerWindDirection;
                Debug.Log("Player took wind direction");
            }

        //Neutralizing Y moves when grounded, or head hitting ceiling or dashing
        if (collisions.above || collisions.below)
        {
            _moveDirection.y = 0;
        }

        //And finally, let's call the final method that will process collision before moving the player =D
        ApplyMoveAndCollisions(_moveDirection * Time.deltaTime);
    }

    private void GrabbingBar()
    {

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

    private void OnTriggerEnter2D(Collider2D other)
    {

        Debug.Log(other.transform.name);

        if (other.CompareTag("Bar"))
        {
            onBar = true;
            _moveDirection = Vector3.zero; //Just grabbed bar, resetting current move dir to immobilize player
            Debug.Log("Player on bar bitch");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Bar"))
        {
            onBar = false;
        }
    }
}
