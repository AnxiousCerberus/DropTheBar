using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindArea : MonoBehaviour {

    public enum WindDirection
    {
        up, down, left, right
    }

    public WindDirection currentWindDirection = WindDirection.right;
    public float windForce = 2f;
    Vector2 windAxis = Vector3.zero;
    Player player;
    Collider2D thisCollider;

	// Use this for initialization
	void Start () {

        player = GameObject.FindObjectOfType<Player>();
        thisCollider = gameObject.GetComponent<Collider2D>();

        if (currentWindDirection == WindDirection.left)
            windAxis = Vector2.left;
        if (currentWindDirection == WindDirection.right)
            windAxis = Vector2.right;
        if (currentWindDirection == WindDirection.up)
            windAxis = Vector2.up;
        if (currentWindDirection == WindDirection.down)
            windAxis = Vector2.down;

        windAxis = Quaternion.AngleAxis (transform.eulerAngles.z, Vector3.forward) * windAxis;
        windAxis = windAxis.normalized;

        windAxis *= windForce;

        Debug.DrawRay(thisCollider.bounds.center, windAxis * 5f, Color.red, 10f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.transform.name);
        if (collision.transform.CompareTag("Player"))
        {
            Debug.Log("Player in wind area");
            player.playerWindDirection += windAxis;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log(collision.transform.name);
        if (collision.transform.CompareTag("Player"))
        {
            Debug.Log("Player in wind area");
            player.playerWindDirection -= windAxis;
        }
    }
}
