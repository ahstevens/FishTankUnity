using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBoatCircuit : MonoBehaviour
{
    private List<Vector3> circuitMarkerPositions;
    [SerializeField]
    private int numMarkers;
    private int currentTargetMarker;

    public int startingMarker;

    public float stepSize = 10f;

    // Use this for initialization
    void Start()
    {

        circuitMarkerPositions = new List<Vector3>();
        for (int i = 0; i < numMarkers; i++)
        {
            string thisName = "BoatCircuit" + i;

            var pathObj = FindPathChild();

            if (pathObj != null)
            {
                var marker = pathObj.Find(thisName);

                if (marker != null)
                    circuitMarkerPositions.Add(marker.position);
                else
                    Debug.Log("Child object not found: " + thisName);
            }
            else
                Debug.Log("Path child object not found!");
        }
        currentTargetMarker = getNextMarkerIndexNum(startingMarker);
        gameObject.transform.position = circuitMarkerPositions[startingMarker];
        gameObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.position - circuitMarkerPositions[getNextMarkerIndexNum(startingMarker)]);
    }


    // Update is called once per frame
    void Update()
    {
        float distanceToTargetMarker = (circuitMarkerPositions[currentTargetMarker] - gameObject.transform.position).magnitude;
        if (distanceToTargetMarker < 1.0f) //if within a meter
        {
            currentTargetMarker = getNextMarkerIndexNum(currentTargetMarker);
        }

        float step = stepSize * Time.deltaTime;
        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, circuitMarkerPositions[currentTargetMarker], step);
        gameObject.transform.rotation = Quaternion.LookRotation(circuitMarkerPositions[currentTargetMarker] - gameObject.transform.position);
    }

    int getNextMarkerIndexNum(int currentMarkerIndexNum)
    {
        if (currentMarkerIndexNum >= numMarkers - 1)
            return 0;
        else
            return (currentMarkerIndexNum + 1);
    }

    public void ResetToStart()
    {
        currentTargetMarker = getNextMarkerIndexNum(startingMarker);
        gameObject.transform.position = circuitMarkerPositions[startingMarker];
        gameObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.position - circuitMarkerPositions[getNextMarkerIndexNum(startingMarker)]);
    }
    Transform FindPathChild()
    {
        var headList = transform.gameObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform it in headList)
            if (it.name == "Path")
                return it;
        return null;
    }
}