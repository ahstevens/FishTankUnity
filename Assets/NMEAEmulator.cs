using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using DotSpatial.Projections;
using CCOM.NMEA;


[AddComponentMenu("GPS/NMEA Emulator")]
public class NMEAEmulator : MonoBehaviour
{
    UdpClient client;

    [Header("UDP Server Settings")]
    public int port = 9900;
    [Space(10)]
    [Range(0, 50)]
    public int updateRateHz = 50;

    [Header("Live Pose from GameObject")]
    public bool sendObjectPosition;
    public bool sendObjectHeading;

    [Header("Manual Coordinate")]
    [Space(10)]
    public float altitude = 0f;
    [Space(10)]
    public string latString;
    public string lonString;
    [Space(10)]
    [Range(0f, 360f)]
    public float heading = 0f;

    float updateDelta = 0f;

    GEOReference georef;

    //Geospatial.Location location = null;

    DateTime clock;

    Transform lastPose; // for speed calcs

    GGA nmeaPosition;
    HDT nmeaHeading;
    VTG nmeaSpeed;

    // Start is called before the first frame update
    void Start()
    {
        client = new UdpClient();

        client.Connect(new IPEndPoint(IPAddress.Broadcast, port));

        nmeaPosition = new GGA();
        nmeaHeading = new HDT();
        nmeaSpeed = new VTG(); // TODO: Implement speed update
    }

    // Update is called once per frame
    void Update()
    {
        if (georef == null)
            georef = (GEOReference)FindObjectOfType(typeof(GEOReference));

        clock = DateTime.UtcNow;

        List<NMEASentence> sentences = new List<NMEASentence>();

        float updateTime = 1f / (float)updateRateHz;

        updateDelta += Time.deltaTime;

        if (updateDelta >= updateTime)
        {            
            UpdatePosition();
            UpdateHeading();

            sentences.Add(nmeaPosition);
            sentences.Add(nmeaHeading);

            updateDelta = updateDelta % updateTime;
        }

        PrepareAndSend(sentences);
    }

    void UpdatePosition()
    {
        if (pointCloudManager.instance.getPointCloudsInScene()[0].EPSG == 0 ||
            pointCloudManager.instance.isWaitingToLoad ||
            pointCloudManager.instance.getPointCloudsInScene().Length == 0)
            return; 

        if (georef != null && sendObjectPosition)
        {
            double[] point = new double[2];
            point[0] = georef.realWorldX + (double)this.transform.position.x;
            point[1] = georef.realWorldZ + (double)this.transform.position.z;

            // heights don't matter
            double[] elev = { 0 };

            // reproject the 3 coords to GPS coords (WGS84) for OnlineMaps
            ProjectionInfo src = ProjectionInfo.FromEpsgCode(pointCloudManager.instance.getPointCloudsInScene()[0].EPSG);
            
            ProjectionInfo dest = ProjectionInfo.FromEpsgCode(4326);

            Reproject.ReprojectPoints(point, elev, src, dest, 0, 1);

            //var latlon = LatLonConversions.ConvertUTMtoLatLon(p[0], p[1], 15, true, LatLonConversions.wgs84);
            //var latlon = LatLonConversions.ConvertUTMtoLatLon(785997.22, 3316376.84, 15, true, LatLonConversions.wgs84);

            nmeaPosition.SetPosition(point[1], point[0]);

            nmeaPosition.SetAltitude(elev[0]);
        }
        //else if (latString != null && latString != "" && lonString != null && lonString != "")
        //{
        //    try
        //    {
        //        location = Geospatial.Location.Parse(latString + " " + lonString);
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Log("Geospatial Location Parse Error: " + e.Message);
        //        location = null;
        //    }
        //
        //    if (location != null)
        //    {
        //        nmeaPosition.SetPosition(location.Latitude.TotalDegrees, location.Longitude.TotalDegrees);
        //
        //        if (location.Altitude.HasValue)
        //            nmeaPosition.SetAltitude(location.Altitude.Value);
        //    }
        //}
    }

    void UpdateHeading()
    {
        if (sendObjectHeading)
            nmeaHeading.SetHeading(this.transform.eulerAngles.y);
        else
            nmeaHeading.SetHeading(heading);
    }

    void PrepareAndSend(NMEASentence sentence)
    {
        //Debug.Log(sentence.ToString());

        byte[] sendBytes = Encoding.ASCII.GetBytes(sentence.ToString());

        client.Send(sendBytes, sendBytes.Length);
    }

    void PrepareAndSend(List<NMEASentence> sentences)
    {
        foreach (var s in sentences)        
            PrepareAndSend(s);        
    }
}
