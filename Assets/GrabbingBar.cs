using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbingBar : MonoBehaviour {

    //[HideInInspector]
    public Collider2D thisCollider;
    //[HideInInspector]
    public SpriteRenderer thisSprite;
    //[HideInInspector]
    public Vector2 barNormalizedDirection;
    //[HideInInspector]
    public float barLength;
    //[HideInInspector]
    public Vector2 barCenter;
    //[HideInInspector]
    public Vector2 upperPointOnBar;
    //[HideInInspector]
    public Vector2 lowerPointOnBar;

    public GameObject barPath;
    public Transform currentPathPoint;
    public float barMoveSpeed = 1f;
    public int pathPointIDNumber = 0;
    List<Transform> pathPoints = new List<Transform>();

    public bool postDropIgnore = false;

    Player player;

    // Use this for initialization
    void Start ()
    {
        thisCollider = gameObject.GetComponent<Collider2D>();
        thisSprite = gameObject.GetComponent<SpriteRenderer>();
        player = GameObject.Find("Player").GetComponent<Player>();

        UpdateVars();
    }

    public void UpdateVars()
    {
        barNormalizedDirection = Quaternion.Euler(transform.rotation.eulerAngles) * Vector3.up;
        //barLength = gameObject.GetComponent<BoxCollider2D>().size.y * transform.localScale.y / 2;
        //barCenter = thisCollider.bounds.center;
        //upperPointOnBar = barCenter + barNormalizedDirection * barLength;
        //lowerPointOnBar = barCenter - barNormalizedDirection * barLength;

        //Debug.DrawLine(upperPointOnBar, lowerPointOnBar, Color.green, 5f);
        //Debug.Log(transform.name + " UPDATED!");
    }

    /*function RotatePointAroundPivot(point: Vector3, pivot: Vector3, angles: Vector3): Vector3 {
   var dir: Vector3 = point - pivot; // get point direction relative to pivot
   dir = Quaternion.Euler(angles) * dir; // rotate it
    point = dir + pivot; // calculate rotated point
   return point; // return it*/

    public IEnumerator PostDropIgnoreTimer ()
    {
        yield return new WaitForSeconds(.5f);
        postDropIgnore = false;
    }

private void FixedUpdate()
    {
        //Get real up point and down point of the bar
        Vector3 size = gameObject.GetComponent<SpriteRenderer>().size;
        Vector3 up = new Vector2(transform.position.x, transform.position.y + size.y * transform.localScale.y / 2);
        Vector3 down = new Vector2(transform.position.x, transform.position.y - size.y * transform.localScale.y / 2);

        //Doing some rotation wizardry to get the two points
        Vector3 dir = up - transform.position;
        dir = transform.rotation * dir;
        up = dir + transform.position;

        dir = down - transform.position;
        dir = transform.rotation * dir;
        down = dir + transform.position;

        Debug.DrawLine(up, down, Color.green);

        RaycastHit2D barHit = Physics2D.Linecast(up, down);

        //Did we hit something on the bar ?
        if (barHit.transform != null)
        {
            Debug.Log("Hit = " + barHit.transform.name);

            //Was it THE PLAYER??? Hope they weren't already on this bar, because else IT WOULD GLITCH
            if (barHit.transform.tag == "Player" && player.currentBar != this && !postDropIgnore)
            {
                player.JustGrabbedBar(this, barHit.point); //OH YIS IT WUZ
            }
                
        }

        //Get first path point
        if (barPath != null)
        {

            pathPoints.AddRange(barPath.transform.GetComponentsInChildren<Transform>());

            //Removing the parent path transform
            pathPoints.Remove(barPath.transform);

                foreach (Transform pathPoint in pathPoints)
                {
                    if (currentPathPoint == null)
                    {
                        currentPathPoint = pathPoint;
                        pathPointIDNumber = pathPoints.IndexOf(pathPoint);
                        return;
                    }
                }
        }

        //Move to current path point
        if (currentPathPoint != null)
        {
            transform.position = Vector3.Lerp(transform.position, currentPathPoint.position, .05f * barMoveSpeed);
            Debug.DrawRay(transform.position, currentPathPoint.position, Color.red);
        }

        //Check arrived at path point
        if (currentPathPoint != null && Vector3.SqrMagnitude(transform.position - currentPathPoint.position) < .5f)
        {
            //Get Next Path Point (Or Loop to first)
            int nextPointNumber = pathPointIDNumber++;

            if (nextPointNumber > pathPoints.Count)
                nextPointNumber = 0;

            currentPathPoint = pathPoints[nextPointNumber];
        }

    }
}
