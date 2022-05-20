using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CaveManager))]
public class CameraVisualizer : MonoBehaviour
{
    CaveManager cm;

    // Start is called before the first frame update
    void Start()
    {
        cm = GetComponent<CaveManager>();    
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmosSelected()
    {
        if (cm == null || cm.GetCaveScreens() == null)
            return;

        //foreach (var s in caveScreens)
        //{
        //    ///draw debug screen indicators
        //    var fts = s.GetComponent<FishTankSurface>();
        //
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawLine(fts.bottomLeft, fts.bottomRight);
        //    Gizmos.DrawLine(fts.bottomLeft, fts.topLeft);
        //    Gizmos.DrawLine(fts.topRight, fts.bottomRight);
        //    Gizmos.DrawLine(fts.topLeft, fts.topRight);
        //    //Draw direction towards eye
        //    Gizmos.color = Color.cyan;
        //    Gizmos.DrawLine(fts.center, fts.center + fts.normal);
        //}

        foreach (var c in cm.GetScreenCameras())
        {
            DrawFrustum(c.GetComponent<Camera>());
        }
    }//end onDrawGizmos

    void DrawFrustum(Camera cam)
    {
        Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
        Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes(cam); //get planes from matrix
        Plane temp = camPlanes[1];
        camPlanes[1] = camPlanes[2];
        camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop

        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = Plane3Intersect(camPlanes[4], camPlanes[i], camPlanes[(i + 1) % 4]); //near corners on the created projection matrix
            farCorners[i] = Plane3Intersect(camPlanes[5], camPlanes[i], camPlanes[(i + 1) % 4]); //far corners on the created projection matrix
        }

        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(nearCorners[i], nearCorners[(i + 1) % 4], Color.red, Time.deltaTime, true); //near corners on the created projection matrix
            Debug.DrawLine(farCorners[i], farCorners[(i + 1) % 4], Color.blue, Time.deltaTime, true); //far corners on the created projection matrix
            Debug.DrawLine(nearCorners[i], farCorners[i], Color.green, Time.deltaTime, true); //sides of the created projection matrix
        }
    }

    Vector3 Plane3Intersect(Plane p1, Plane p2, Plane p3)
    { //get the intersection point of 3 planes
        return ((-p1.distance * Vector3.Cross(p2.normal, p3.normal)) +
                (-p2.distance * Vector3.Cross(p3.normal, p1.normal)) +
                (-p3.distance * Vector3.Cross(p1.normal, p2.normal))) /
            (Vector3.Dot(p1.normal, Vector3.Cross(p2.normal, p3.normal)));
    }
}
