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

    // Use this for initialization
    void Start ()
    {
        thisCollider = gameObject.GetComponent<Collider2D>();
        thisSprite = gameObject.GetComponent<SpriteRenderer>();

        UpdateVars();
    }

    public void UpdateVars()
    {
        barNormalizedDirection = Quaternion.Euler(transform.rotation.eulerAngles) * Vector3.up;
        barLength = gameObject.GetComponent<BoxCollider2D>().size.y * transform.localScale.y / 2;
        barCenter = thisCollider.bounds.center;
        upperPointOnBar = barCenter + barNormalizedDirection * barLength;
        lowerPointOnBar = barCenter - barNormalizedDirection * barLength;

        Debug.DrawLine(upperPointOnBar, lowerPointOnBar, Color.green, 5f);
        Debug.Log(transform.name + " UPDATED!");
    }
}
