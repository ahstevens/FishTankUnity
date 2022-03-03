using System;
using UnityEngine;

public class pointCloud : MonoBehaviour
{
    public String pathToRawData;
    public String ID;

    public double adjustmentX;
    public double adjustmentY;
    public double adjustmentZ;

    public double initialXShift;
    public double initialZShift;

    public string spatialInfo;
    public int UTMZone;
    public bool North;

    //void OnDestroy()
    //{
    //    Debug.Log("pointCloud OnDestroy!");
    //    pointCloudManager.UnLoad(ID);
    //}
}
