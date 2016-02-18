﻿using UnityEngine;
using System.Collections;

//Enum to lean the code a bit.
public enum CameraBounds
{
    TOP,
    BOT,
    RIGHT,
    LEFT,
    END
}

public class Camera_Controller : MonoBehaviour {

    //Currently set as a click and drag. Can change to assign on start.
    public Transform player1;
    public Transform player2;

    #region Camera Control Variables
    [SerializeField][Tooltip("The factor by which the Y position of the camera get multiplied by. Lower number means a more zoomed in environment.")]
    float zoomFactor = 5f;
    [SerializeField][Tooltip("Speed of movement of the Camera.")]
    float smoothing = 2f;
    [SerializeField][Tooltip("Angle of the Camera on the X axis. Adjust this value to match the camera Transform. WARNING: Low angles may cause the camera to skip or jitter!") ]
    float cameraAngle = 80f;
    [Tooltip("Layer mask we hit with screen side raycasts. Bounds is our default and only layer we want to hit.")]
    public LayerMask bounds;//use this layer for level bounds.
   
    Ray[] cameraBounds = new Ray[4];//Quad rays that delimit the camera view. (top, bot, right, left)
    bool[] boundHitting = new bool[4]{false, false, false, false};//The result of the raycast from cameraBounds[]. True means we have hit a wall.
    
    //Vector to manipulate to set camera new target position.
    Vector3 newMeanPosition;
    #endregion
    // Update is called once per frame
	void Update () 
    {
        CameraUpdate();
	}

    #region Camera Functions

    void CameraUpdate() 
    {
        newMeanPosition = GetMeanPosition(player1.position, player2.position);
        //Quad cast to check for bounds.
        cameraBounds[0] = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height, 0));//top
        cameraBounds[1] = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, 0, 0));//bot
        cameraBounds[2] = Camera.main.ScreenPointToRay(new Vector3(Screen.width, Screen.height / 2f, 0));//right
        cameraBounds[3] = Camera.main.ScreenPointToRay(new Vector3(0, Screen.height / 2f, 0));//left

        for (CameraBounds i = CameraBounds.TOP; i < CameraBounds.END; i++)
        {
            RaycastHit hit;
            //Check if we are hitting a level bound.
            if (Physics.Raycast(cameraBounds[(int)i], out hit, 1000f, bounds))
            {
                boundHitting[(int)i] = true;

            }
            else
            {
                boundHitting[(int)i] = false;
            }

        }

        bool goingUp = newMeanPosition.z > transform.position.z;
        bool goingRight = newMeanPosition.x > transform.position.x;

        if (goingUp && boundHitting[0])
        {
            newMeanPosition = new Vector3(newMeanPosition.x, transform.position.y, transform.position.z);
        }
        else if (!goingUp && boundHitting[1])
        {
            newMeanPosition = new Vector3(newMeanPosition.x, transform.position.y, transform.position.z);
        }


        if (goingRight && boundHitting[2])
        {
            newMeanPosition = new Vector3(transform.position.x, transform.position.y, newMeanPosition.z);
        }
        else if (!goingRight && boundHitting[3])
        {
            newMeanPosition = new Vector3(transform.position.x, transform.position.y, newMeanPosition.z);
        }

        //Stop the camera from going higher up if we are hitting a bound to prevent seeing over it.
        if (boundHitting[0] || boundHitting[1] || boundHitting[2] || boundHitting[3])
        {
            newMeanPosition = new Vector3(newMeanPosition.x, transform.position.y, newMeanPosition.z);
        }
        //Our actual camera movement.
        transform.position = Vector3.Lerp(transform.position, newMeanPosition, smoothing * Time.deltaTime);
        
    }

    //Returns the average position between player 1 and 2 with an offset on the Z axis for the camera angle
    Vector3 GetMeanPosition(Vector3 p1, Vector3 p2) 
    {
        //Averages of our 2 player positions.
        float xMean = (p1.x + p2.x) / 2f;
        float yMean = (p1.y + p2.y) / 2f;
        float zMean = (p1.z + p2.z) / 2f;
        //returned values.
        float xReturned = xMean;
        float zReturned = zMean;
        
        //Calculate the x distance and z distance between our 2 players. Vertical is 2X because of camera angle and screen size. May need a small adjustment is we change the camera angle or resolution a lot.
        float horizontalFactor = Mathf.Abs( Vector3.Dot(p1, new Vector3(1f, 0f, 0f)) - Vector3.Dot(p2, new Vector3(1f, 0f, 0f)));
        float verticalFactor = 2f * Mathf.Abs( Vector3.Dot(p1, new Vector3(0f, 0f, 1f)) - Vector3.Dot(p2, new Vector3(0f, 0f, 1f)));

        float[] planeValues = new float[] { horizontalFactor, verticalFactor };
        //We set the Y position by taking our biggest distance between X and Z and adding a small bonus base on the Y distance of our players.
        float yReturned = Mathf.Clamp(((Mathf.Max(planeValues)+ (yMean*2f)) * zoomFactor), 10f, 100f);

        //figure the correct z offset with fancy math.
        float zOffset = -Mathf.Sqrt((Mathf.Pow((yMean - yReturned), 2) * Mathf.Pow((Mathf.Cos(cameraAngle * Mathf.Deg2Rad)), 2) + Mathf.Pow((xMean - xReturned), 2) * Mathf.Pow(Mathf.Cos(cameraAngle * Mathf.Deg2Rad), 2))
            / (1f - Mathf.Pow(Mathf.Cos(cameraAngle * Mathf.Deg2Rad), 2)));

        return new Vector3(xReturned, yReturned, zReturned + zOffset);
    }
    #endregion
}
