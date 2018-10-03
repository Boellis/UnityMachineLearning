using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//Stores memories
public class Replay
{
    
    public List<double> states;
    //What reward happened at the particular state?
    public double reward;

    public Replay(double xr, double ballz, double ballvx, double r)
    {
        states = new List<double>();
        //X rotation of platform
        states.Add(xr);
        //Ball z position
        states.Add(ballz);
        //Ball velocity
        states.Add(ballvx);
        reward = r;
    }
}

public class Brain : MonoBehaviour {

    public GameObject ball;

    ANN ann;

    float reward = 0.0f;                                //reawrd to associate with actons
    List<Replay> replayMemory = new List<Replay>();     //memory - list of past actions and rewards
    int mCapacity = 10000;                              //memory capacity

    float discount = 0.99f;                             //how much future states affect rewards
    float exploreRate = 100.0f;                         //chance of pcking random action
    float maxExploreRate = 100.0f;                      //max chance value
    float minExploreRate = 0.01f;                       //min chance value
    float exploreDecay = 0.0001f;                       //chance deacy amount for each update

    Vector3 ballStartPos;                               //record start position of object
    int failCount = 0;                                  //count when the ball is dropped
    float tiltSpeed = 0.5f;                             //max angle to apply to tilting each update
    /*
    Make sure tilt speed is large enough so that the q value multiplied by it is enough to recover
    balance when the ball gets a good speed up
    */
    float timer = 0;                                    //Timer to keep track of balancing
    float maxBalanceTime = 0;                           //Record time ball is kept balanced

	// Use this for initialization
	void Start () {
        //Create Neural Network
        ann = new ANN(3, 2, 1, 6, 0.3f);
        //Set ball position
        ballStartPos = ball.transform.position;
        //5x as fast
        Time.timeScale = 3.0f;
	}

    GUIStyle guiStyle = new GUIStyle();
    void OnGUI()
    {
        guiStyle.fontSize = 25;
        guiStyle.normal.textColor = Color.white;
        GUI.BeginGroup(new Rect(10, 10, 600, 150));
        GUI.Box(new Rect(0, 0, 140, 140), "Stats", guiStyle);
        GUI.Label(new Rect(10,25,500,30), "Fails: " + failCount, guiStyle);
        GUI.Label(new Rect(10, 50, 500, 30), "Decay Rate: " + exploreRate, guiStyle);
        GUI.Label(new Rect(10, 75, 500, 30), "Last Best Balance: " + maxBalanceTime, guiStyle);
        GUI.Label(new Rect(10, 100, 500, 30), "This Balance: " + timer, guiStyle);
        GUI.EndGroup();




    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown("space"))
            ResetBall();
	}

    void FixedUpdate()
    {
        timer += Time.deltaTime;
        List<double> states = new List<double>();
        List<double> qs = new List<double>();
        //Inputs for Neural Network
        states.Add(this.transform.rotation.x);
        states.Add(ball.transform.position.z);
        states.Add(ball.GetComponent<Rigidbody>().angularVelocity.x);

        qs = SoftMax(ann.CalcOutput(states));
        double maxQ = qs.Max();
        int maxQIndex = qs.ToList().IndexOf(maxQ);
        //Chance of doing random action
        exploreRate = Mathf.Clamp(exploreRate - exploreDecay, minExploreRate, maxExploreRate);

        //if (Random.Range(0, 100) < exploreRate)
            //maxQIndex = Random.Range(0, 2);
        if (maxQIndex == 0)
            this.transform.Rotate(Vector3.right, tiltSpeed * (float)qs[maxQIndex]);
        else if (maxQIndex == 1)
            this.transform.Rotate(Vector3.right, -tiltSpeed * (float)qs[maxQIndex]);
        if (ball.GetComponent<BallState>().dropped)
            reward = -1.0f;
        else
            reward = 0.1f;

        //Trains through the memory whenever we drop the bll
        Replay lastMemory = new Replay(this.transform.rotation.x,
                                       ball.transform.position.z,
                                       ball.GetComponent<Rigidbody>().angularVelocity.x,
                                       reward);
        if (replayMemory.Count > mCapacity)
            replayMemory.RemoveAt(0);

        replayMemory.Add(lastMemory);
        //Q-Learning
        if (ball.GetComponent<BallState>().dropped)
        {
            //Loop through memory backward
            for(int i = replayMemory.Count - 1; i >= 0; i--)
            {
                //What the q values are with current memory
                List<double> toutputsOld = new List<double>();
                //Q values for next memory
                List<double> toutputsNew = new List<double>();
                toutputsOld = SoftMax(ann.CalcOutput(replayMemory[i].states));
                //Maxmium q values of old memories and which is the best action accordingly
                double maxQOld = toutputsOld.Max();
                int action = toutputsOld.ToList().IndexOf(maxQOld);

                //Bellman Equation
                double feedback;
                if (i == replayMemory.Count - 1 || replayMemory[i].reward == -1)
                    feedback = replayMemory[i].reward;
                else
                {
                    toutputsNew = SoftMax(ann.CalcOutput(replayMemory[i + 1].states));
                    maxQ = toutputsNew.Max();
                    feedback = (replayMemory[i].reward + discount * maxQ);
                }
                toutputsOld[action] = feedback;
                //Train again with updated values
                ann.Train(replayMemory[i].states, toutputsOld);
            }
            if(timer >maxBalanceTime)
            {
                maxBalanceTime = timer;
            }
            timer = 0;

            ball.GetComponent<BallState>().dropped = false;
            this.transform.rotation = Quaternion.identity;
            ResetBall();
            replayMemory.Clear();
            failCount++;
        }
    }

    void ResetBall()
    {

        ball.transform.position = ballStartPos;
        ball.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        ball.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
    }

    //Normalizing output values
    //Take all values in a vector and reduces them down to values between 0 and 1 and sums them up to where all values equals either 0 or 1
    List<double> SoftMax(List<double> values)
    {
        double max = values.Max();

        float scale = 0.0f;
        for(int i = 0; i < values.Count; i++)
        {
            scale += Mathf.Exp((float)(values[i] - max));
        }
        List<double> result = new List<double>();
        for(int i = 0; i < values.Count; i++)
        {
            result.Add(Mathf.Exp((float)(values[i] - max)) / scale);
        }
        return result;
    }
}
