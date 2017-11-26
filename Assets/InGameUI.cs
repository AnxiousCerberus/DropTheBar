using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour {

    #region other objects to Monitor
    Player player;
    #endregion

    public Text Health;

    // Use this for initialization
    void Start () {
        player = GameObject.FindObjectOfType<Player>();

        Health = transform.Find("HEALTH").GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        UIInfosUpdate();
    }

    void UIInfosUpdate()
    {
        Health.text = "HEALTH = " + player.currentHealth.ToString() + " / " + player.maxHealth.ToString();
    }

}
