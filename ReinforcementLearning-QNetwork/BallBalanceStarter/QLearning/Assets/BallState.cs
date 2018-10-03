using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used to detect collision
public class BallState : MonoBehaviour {
    public bool dropped = false;

    void OnCollisionEnter(Collision col)
    {
        if(col.gameObject.tag == "drop")
        {
            dropped = true;
        }
    }
}
