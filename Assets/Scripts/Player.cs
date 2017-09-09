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
    public Vector2 currentWindDirection = Vector2.zero;
    public Vector2 targetWindDirection = Vector2.zero;
    bool onBar = false;
    bool currentEnabled = true;
    enum moveState { InWind, OnBar };
    moveState currentMoveState = moveState.InWind;
    GrabbingBar currentBar;
    Vector2 swipeOnBarPlayerStartPos = Vector3.zero;

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
    Vector2 dirFromStartToIntersection = Vector2.zero;
    #endregion

    private void Start()
    {
        //Collisions & Physics base calculation
        CalculateRaySpacing();
    }

    int touchReleaseCount = 0;

    private void Update()
    {
        currentWindDirection = Vector3.Lerp(currentWindDirection, targetWindDirection, .05f);

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
            {
                touchStartPos = Input.GetTouch(0).position;
                dirFromStartToIntersection = Vector3.zero;
                swipeOnBarPlayerStartPos = transform.position;
            }

            if (touchDuration > 0)
                debugText.text += "touch Duration = " + touchDuration;

            if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                touchEndPos = Input.GetTouch(0).position;
                dirFromStartToIntersection = Vector3.zero;
                touchPreviousPos = Vector2.zero;

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
                //Moving on bar
                if (touchPreviousPos != Vector2.zero && onBar)
                {
                    Vector2 touchStartWorld = Camera.main.ScreenToWorldPoint(touchStartPos);
                    Vector2 perpendicularBarDirection = Quaternion.Euler(0, 0, 90) * currentBar.barNormalizedDirection;
                    Vector2 touchIntersectPoint = LineIntersectionPoint(touchStartWorld - currentBar.barNormalizedDirection * 100, touchStartWorld + currentBar.barNormalizedDirection * 100
                        ,touchPreviousPos - perpendicularBarDirection * 100, touchPreviousPos + perpendicularBarDirection * 100);

                    dirFromStartToIntersection = touchIntersectPoint - touchStartWorld;
                    Debug.DrawLine(touchStartWorld, touchIntersectPoint, Color.white);
                }

                touchPreviousPos = Input.GetTouch(0).position;
                touchPreviousPos = Camera.main.ScreenToWorldPoint(touchPreviousPos);
            }

            if (dirFromStartToIntersection != Vector2.zero && onBar)
                transform.position = Vector3.Lerp(transform.position, swipeOnBarPlayerStartPos + dirFromStartToIntersection, .1f);
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

            if (currentWindDirection == Vector2.zero)
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
                _moveDirection = currentWindDirection;
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
            GrabbingBar bar = other.GetComponent<GrabbingBar>();
            onBar = true;
            _moveDirection = Vector3.zero; //Just grabbed bar, resetting current move dir to immobilize player

            bar.UpdateVars();

            Vector3 targetPoint = transform.position;

            if (Vector3.Dot (transform.up, bar.barNormalizedDirection) > .55f)
            {
                targetPoint = LineIntersectionPoint(transform.position - transform.right, transform.position + transform.right, bar.lowerPointOnBar, bar.upperPointOnBar);
                Debug.Log("Horizontal test");
            }
            else
            {
                targetPoint = LineIntersectionPoint(transform.position - transform.up, transform.position + transform.up, bar.lowerPointOnBar, bar.upperPointOnBar);
                Debug.Log("Vertical test");
            }

            transform.position = targetPoint;
            swipeOnBarPlayerStartPos = transform.position;

            if (Input.touchCount > 0)
            {
                touchStartPos = Input.GetTouch(0).position; //Avoid the player from moving when getting from bar to bar all while still having a touch input
                dirFromStartToIntersection = Vector3.zero;
                touchPreviousPos = Vector2.zero;
            }

            currentBar = bar;
        }
    }

    Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2)
    {
        // Get A,B,C of first line - points : ps1 to pe1
        float A1 = pe1.y - ps1.y;
        float B1 = ps1.x - pe1.x;
        float C1 = A1 * ps1.x + B1 * ps1.y;

        // Get A,B,C of second line - points : ps2 to pe2
        float A2 = pe2.y - ps2.y;
        float B2 = ps2.x - pe2.x;
        float C2 = A2 * ps2.x + B2 * ps2.y;

        // Get delta and check if the lines are parallel
        float delta = A1 * B2 - A2 * B1;
        if (delta == 0)
            throw new System.Exception("Lines are parallel");

        // now return the Vector2 intersection point
        return new Vector2(
            (B2 * C1 - B1 * C2) / delta,
            (A1 * C2 - A2 * C1) / delta
        );
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Bar"))
        {
            onBar = false;
            currentBar = null;
        }
    }
}
