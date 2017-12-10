using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingCollisionTester : MonoBehaviour {

    public float Speed = 5;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.Translate(Vector3.right * Speed * Time.deltaTime);
	}
}
