using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Characters
{
    public Text debugText;
    public float HurtRecoveryTimer = 0;

    #region Status Vars
    //Wind
    bool airStreamEnabled = true;
    public Vector2 currentWindDirection = Vector2.zero;
    public Vector2 targetWindDirection = Vector2.zero;

    //Bar
    bool onBar = false;
    [HideInInspector]
    public GrabbingBar currentBar;
    Vector3 grabOffset = Vector2.zero; //Offset used to move player along bar
    #endregion

    #region Processing Vars
    [HideInInspector]
    public Vector3 _moveDirection; //Final move direction, used to move player
    #endregion

    #region TouchStatus
    float touchDuration = 0f; //mainly used to know if the user swiped or just tapped the screen
    Vector2 touchStartPos = Vector2.zero;
    Vector2 touchEndPos = Vector2.zero;
    Vector2 touchPreviousPos = Vector2.zero; //Used to store the touch pos of the previous frame
    Vector3 dirFromStartToIntersection = Vector2.zero;
    bool simpleTap = false;
    #endregion

    private void Start()
    {
        //Collisions & Physics base calculation
        CalculateRaySpacing();
    }

    private void Update()
    {
        simpleTap = false; //Resetting simpleTap for the beginning of this frame.

        HurtRecovery();
        currentWindDirection = Vector3.Lerp(currentWindDirection, targetWindDirection, .05f);

        //If attached to bar, force position to be relative to the bar
        if (currentBar != null)
        {
            //Applying bar grab offset. This simulates the player being attached to the bar without using a transform parenting.
            transform.position =  currentBar.transform.position + grabOffset;
        }

        //TODO : Normalize wind direction, maybe? Not sure how we can do this with other forces but...

        if (Input.touchCount > 0)
        {
            //Collecting infos on touch input...
            GetTouchInputStats();

            //Translating touch inputs into character moves
            MoveAlongBar();
        }
        else
        {
            this.GetComponent<SpriteRenderer>().color = Color.white;
            touchDuration = 0;
        }

        //Bar Behaviour
        if (onBar)
        {
            airStreamEnabled = false;

            if (simpleTap)
            {
                JustDroppedBar();
                onBar = false;
            }
        }
        else
        {
            airStreamEnabled = true;
        }

        //Gravity and wind!
        if (currentWindDirection == Vector2.zero)
        {
            //Applying Gravity
            if (GravityOn && !onBar)
                _moveDirection.y += calculatedGravity * Time.deltaTime;
        }
        else if (airStreamEnabled)
        {
            _moveDirection = currentWindDirection;
        }

        //Neutralizing Y moves when grounded, or head hitting ceiling or dashing
        if (collisions.above || collisions.below)
        {
            _moveDirection.y = 0;
        }

        //And finally, let's call the final method that will process collision before moving the player =D
        ApplyMoveAndCollisions(_moveDirection * Time.deltaTime);
    }

    void GetTouchInputStats ()
    {
        touchDuration += Time.deltaTime;

        if (Input.GetTouch(0).phase == TouchPhase.Began)
        {
            touchStartPos = Input.GetTouch(0).position; //Used for translating relative touch movement to character's movement on a bar
            dirFromStartToIntersection = Vector3.zero;
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
                if (Vector3.SqrMagnitude(touchStartPos - touchEndPos) < .75f)
                    simpleTap = true;
        }
    }

    #region Bar Methods
    void MoveAlongBar ()
    {
        //Moving on bar
        //Translating touch inputs into character moves
        if (!simpleTap && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            if (touchPreviousPos != Vector2.zero && onBar)
            {
                //Touch start point in world coordinates
                Vector2 touchStartWorld = Camera.main.ScreenToWorldPoint(touchStartPos);
                //Get the perpendicular direction of the bar...
                Vector2 perpendicularBarDirection = Quaternion.Euler(0, 0, 90) * currentBar.barNormalizedDirection;
                //...used here to get the intersection point between a bar that begins on the touch point and goes towards the bar
                Vector2 touchIntersectPoint = LineIntersectionPoint(touchStartWorld - currentBar.barNormalizedDirection * 100, touchStartWorld + currentBar.barNormalizedDirection * 100
                    , touchPreviousPos - perpendicularBarDirection * 100, touchPreviousPos + perpendicularBarDirection * 100);

                //Now we have a direction that we can use later to translate it onto the character, so that they will move along the bar =)
                dirFromStartToIntersection = touchIntersectPoint - touchStartWorld;
                Debug.DrawLine(touchStartWorld, touchIntersectPoint, Color.green);
            }

            //Used to collect some infos that will be used in the next frame
            touchPreviousPos = Input.GetTouch(0).position;
            touchPreviousPos = Camera.main.ScreenToWorldPoint(touchPreviousPos);
        }

        //Okay, so if the touch direction we got is superior to zero...
        if (dirFromStartToIntersection != Vector3.zero && onBar)
        {
            //GrabOffset is the character's target position on bar. It's anchor point is the bar's center
            Vector3 targetGrabOffset = Vector3.Lerp(grabOffset, grabOffset + dirFromStartToIntersection, .1f);

            //Prevent character to go beyond bar's ends
            if (Vector3.SqrMagnitude(currentBar.up - currentBar.transform.position) < Vector3.SqrMagnitude(targetGrabOffset))
                targetGrabOffset = grabOffset;
            else
                grabOffset = targetGrabOffset;
        }
    }
    public void JustGrabbedBar (GrabbingBar bar, Vector2 targetPoint)
    {
        onBar = true;
        _moveDirection = Vector3.zero; //Just grabbed bar, resetting current move dir to immobilize player

        //Make sure the player will go full wind speed when dropping the bar
        currentWindDirection = targetWindDirection;

        bar.UpdateVars();

        transform.position = targetPoint;

        if (Input.touchCount > 0)
        {
            touchStartPos = Input.GetTouch(0).position; //Avoid the player from moving when getting from bar to bar all while still having a touch input
            dirFromStartToIntersection = Vector3.zero;
            touchPreviousPos = Vector2.zero;
        }

        grabOffset = transform.position - bar.transform.position;
        currentBar = bar;
    }
    public void JustDroppedBar ()
    {
        currentBar.postDropIgnore = true;
        StartCoroutine (currentBar.PostDropIgnoreTimer());
        currentBar = null;
    }
    #endregion

    #region Get Hurt
    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("HurtObstacle") && HurtRecoveryTimer <= 0)
        {
            currentHealth--;
            HurtRecoil();
        }
    }
    void HurtRecoil()
    {
        currentWindDirection = -_moveDirection * 1.5f;
        HurtRecoveryTimer = 2f;
    }
    void HurtRecovery ()
    {
        //HurtRecoveryTimer
        if (HurtRecoveryTimer >= 0)
            HurtRecoveryTimer -= Time.deltaTime;
    }
    #endregion


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
