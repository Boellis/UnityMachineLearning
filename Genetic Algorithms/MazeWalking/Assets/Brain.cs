using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brain : MonoBehaviour {

    int DNALength = 2;
    public DNA dna;
    public GameObject eyes;
    bool alive = true;
    bool seeWall = true;
    public float distanceTraveled = 0;
    Vector3 startPosition;

    public void Init()
    {
        //Initialize DNA
        //0 forward
        //1 Angle Turn
        dna = new DNA(DNALength, 360);
        startPosition = this.transform.position;
    }

    void OnCollisionEnter(Collision obj)
    {
        if (obj.gameObject.tag == "dead")
        {
            alive = false;
            distanceTraveled = 0;
        }

    }

    void Update()
    {
        if (!alive) return;

        Debug.DrawRay(eyes.transform.position, eyes.transform.forward * 0.5f, Color.red);
        seeWall = false;
        RaycastHit hit;
        if (Physics.SphereCast(eyes.transform.position, 0.1f,eyes.transform.forward, out hit, 0.5f))
        {
            if (hit.collider.gameObject.tag == "wall")
            {
                seeWall = true;
            }
        }
    }
    private void FixedUpdate()
    {
        if (!alive) return;

        //read DNA
        //Horizontal
        float h = 0;
        float v = dna.GetGene(0);

        if (seeWall)
        {
            h = dna.GetGene(1);
        }

        //Make this value smaller to reduce the speed of the bots
        this.transform.Translate(0, 0, v * 0.0006f);//0.001f

        this.transform.Rotate(0, h, 0);
        distanceTraveled = Vector3.Distance(startPosition, this.transform.position);
    }

}
