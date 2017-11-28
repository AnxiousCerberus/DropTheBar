using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikyRing : MonoBehaviour {

    public GameObject attachedBar;
    public GrabbingBar currentBar;
    bool aimUp = false;

    // Use this for initialization
    void Start () {
        currentBar = attachedBar.GetComponent<GrabbingBar>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Vector3.Magnitude(transform.position - currentBar.up) < .01f)
            aimUp = false;
        else if (Vector3.Magnitude(transform.position - currentBar.down) < .01f)
            aimUp = true;
            
        if(aimUp)
            transform.position = Vector3.Lerp(transform.position, currentBar.up, .1f);
        else
            transform.position = Vector3.Lerp(transform.position, currentBar.down, .1f);
    }

}
