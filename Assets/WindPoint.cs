using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindPoint : MonoBehaviour {

    public enum WindDirection
    {
        up, down, left, right
    }

    public WindDirection currentWindDirection = WindDirection.right;
    public float windForce = 2f;
    Vector2 windAxis = Vector3.zero;
    Player player;
    Collider2D thisCollider;
    public Transform nextPoint;
    Vector3 windDirection = Vector3.zero;

    // Use this for initialization
    void Start () {
        player = GameObject.FindObjectOfType<Player>();

        //Find Next Point
        if (transform.GetSiblingIndex() + 1 < transform.parent.childCount)
            nextPoint = transform.parent.GetChild(transform.GetSiblingIndex() + 1);
        else
            nextPoint = transform.parent.GetChild(0);
        

        windDirection = nextPoint.transform.position - transform.position;
        windDirection = windDirection.normalized;

        float angle = Vector3.SignedAngle(Vector3.up, windDirection, Vector3.forward);

        windDirection *= windForce;



        gameObject.GetComponentInChildren<ParticleSystem>().transform.eulerAngles = new Vector3 (0,0,angle);
        gameObject.GetComponentInChildren<ParticleSystem>().startSpeed *= windForce;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            Debug.Log("Player in wind area");
            //thisCollider.bounds.clo
            player.targetWindDirection = windDirection;
        }
    }

   /* private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log(collision.transform.name);
        if (collision.transform.CompareTag("Player"))
        {
            Debug.Log("Player in wind area");
            player.playerWindDirection -= windAxis;
        }
    }*/
}
