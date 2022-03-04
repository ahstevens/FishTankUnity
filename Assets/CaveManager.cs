using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

public class CaveManager : MonoBehaviour
{
    public GameObject ViewerCameraPrefab;
    public GameObject CompositingQuadPrefab;
    public GameObject viewer;
    public GameObject compositingCamera;

    public string calibrationFilename = "calibration.xml";

    public int renderTextureWidth;
    public int renderTextureHeight;

    private List<GameObject> caveScreens;
    private List<GameObject> screenCameras;
    private List<GameObject> CompositingQuads;
    private List<RenderTexture> renderTextures;
    private int numberScreens;
    private RenderTexture screenRenderTexture;
    private float lastCompositingCameraOrthoSize;
    
    

    // Start is called before the first frame update
    void Start()
    {
        caveScreens = new List<GameObject>();
        screenCameras = new List<GameObject>();
        renderTextures = new List<RenderTexture>();

        CompositingQuads = new List<GameObject>();

        lastCompositingCameraOrthoSize = -1;

        caveScreens = ReadCalibration();

        foreach (var s in caveScreens)
        {
            var fts = s.GetComponent<FishTankSurface>();
            fts.Recalculate();
            spawnNewQuad(this.transform, s.name + " Quad", fts.topLeft, fts.topRight, fts.bottomRight, fts.bottomLeft);
        }

        numberScreens = caveScreens.Count;

        for (int i = 0; i < numberScreens; i++)
        {
            RenderTexture thisRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            //thisRenderTexture.antiAliasing = 2;  //what do?
            thisRenderTexture.Create();
            renderTextures.Add(thisRenderTexture);


            //GameObject thisCamera = Instantiate(ViewerCameraPrefab, new Vector3(0, 0, 0), Quaternion.identity, viewer.transform);
            GameObject thisCamera = Instantiate(ViewerCameraPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            thisCamera.name = "Screen Camera " + i;
            thisCamera.tag = "Point Cloud Render Camera";
            thisCamera.GetComponent<Camera>().nearClipPlane = 0.1f;
            thisCamera.GetComponent<Camera>().farClipPlane = 10000;
            thisCamera.GetComponent<Camera>().targetTexture = renderTextures[i];
            thisCamera.GetComponent<Camera>().cullingMask = ~(1 << LayerMask.NameToLayer("CompCamera"));
            screenCameras.Add(thisCamera);

            GameObject thisCompositingQuad = Instantiate(CompositingQuadPrefab, new Vector3(0 + (2 * i), 0, 1), Quaternion.identity, compositingCamera.transform);
            thisCompositingQuad.name = "CompositingQuad " + i;
            thisCompositingQuad.GetComponent<MeshRenderer>().material.mainTexture = renderTextures[i];
            //reverse quad vertices' UVs
            //Vector2[] uvs = new Vector2[thisCompositingQuad.GetComponent<MeshFilter>().mesh.vertices.Length];
            //for (int vert=0;vert<thisCompositingQuad.GetComponent<MeshFilter>().mesh.vertices.Length;vert++)
            //{
            //    //Debug.Log("Before: " + thisCompositingQuad.GetComponent<MeshFilter>().mesh.uv[vert]);
            //    uvs[vert] = new Vector2(thisCompositingQuad.GetComponent<MeshFilter>().mesh.uv[vert].x, 1.0f - thisCompositingQuad.GetComponent<MeshFilter>().mesh.uv[vert].y);
            //    //Debug.Log("After: " + thisCompositingQuad.GetComponent<MeshFilter>().mesh.uv[vert]);
            //}
            //thisCompositingQuad.GetComponent<MeshFilter>().mesh.uv = uvs;

            CompositingQuads.Add(thisCompositingQuad);

        }//for each screen

        resizeCompositingQuads();

        //figure out screen rectangles for final compositing

        screenRenderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
    }

    private void OnDrawGizmos()
    {
        if (caveScreens == null)
            return;

        foreach (var s in caveScreens)
        {
            ///draw debug screen indicators
            var fts = s.GetComponent<FishTankSurface>();

            Gizmos.color = Color.red;
            Gizmos.DrawLine(fts.bottomLeft, fts.bottomRight);
            Gizmos.DrawLine(fts.bottomLeft, fts.topLeft);
            Gizmos.DrawLine(fts.topRight, fts.bottomRight);
            Gizmos.DrawLine(fts.topLeft, fts.topRight);
            //Draw direction towards eye
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(fts.center, fts.center + fts.normal);
        }

        foreach (var c in screenCameras)
        {
            DrawFrustum(c.GetComponent<Camera>());
        }
    }//end onDrawGizmos

    void DrawFrustum(Camera cam)
    {
        Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
        Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes(cam); //get planes from matrix
        Plane temp = camPlanes[1]; camPlanes[1] = camPlanes[2]; camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop

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

    // Update is called once per frame
    void Update()
    {        
        resizeCompositingQuads();
        
        for (int i = 0; i < numberScreens; ++i)
        {
            var fts = caveScreens[i].GetComponent<FishTankSurface>();

            Vector3 pa = fts.bottomLeft;
            Vector3 pb = fts.bottomRight;
            Vector3 pc = fts.topLeft;
            Vector3 pd = fts.topRight;

            Vector3 vr = fts.right;
            Vector3 vu = fts.up;
            Vector3 vn = fts.normal;
            Matrix4x4 M = fts.m;

            Vector3 eyePos = viewer.transform.position;

            //From eye to projection screen corners
            Vector3 va = pa - eyePos;
            Vector3 vb = pb - eyePos;
            Vector3 vc = pc - eyePos;
            Vector3 vd = pd - eyePos;

            Vector3 viewDir = eyePos + va + vb + vc + vd;

            //distance from eye to projection screen plane
            float d = -Vector3.Dot(va, vn);
            bool ClampNearPlane = true; //?
            if (ClampNearPlane)
                screenCameras[i].GetComponent<Camera>().nearClipPlane = d;
            float n = screenCameras[i].GetComponent<Camera>().nearClipPlane;
            float f = screenCameras[i].GetComponent<Camera>().farClipPlane;

            float nearOverDist = n / d;
            float l = Vector3.Dot(vr, va) * nearOverDist;
            float r = Vector3.Dot(vr, vb) * nearOverDist;
            float b = Vector3.Dot(vu, va) * nearOverDist;
            float t = Vector3.Dot(vu, vc) * nearOverDist;
            Matrix4x4 P = Matrix4x4.Frustum(l, r, b, t, n, f);

            //Translation to eye position
            Matrix4x4 T = Matrix4x4.Translate(-eyePos);

            //Matrix4x4 R = Matrix4x4.Rotate( Quaternion.Inverse(transform.rotation) * fts.transform.rotation); //may need to replace with center of screen location?

            screenCameras[i].GetComponent<Camera>().worldToCameraMatrix = M * T;// R * T;

            screenCameras[i].GetComponent<Camera>().projectionMatrix = P;
            screenCameras[i].GetComponent<Camera>().nonJitteredProjectionMatrix = P;
        }//end for i (each screenCamera)

    }//end Update()

    void spawnNewQuad(Transform parentScreenTransform, string name, Vector3 UL, Vector3 UR, Vector3 LR, Vector3 LL)
    {
        GameObject newQuad = new GameObject(name);
        newQuad.transform.parent = parentScreenTransform;

        MeshRenderer meshRenderer = newQuad.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        MeshFilter meshFilter = newQuad.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = LL;
        vertices[1] = LR;
        vertices[2] = UL;
        vertices[3] = UR;
        mesh.vertices = vertices;

        //Debug.Log("Mesh vert 1 " + mesh.vertices[0]);
        //Debug.Log("Mesh vert 2 " + mesh.vertices[1]);
        //Debug.Log("Mesh vert 3 " + mesh.vertices[2]);
        //Debug.Log("Mesh vert 4 " + mesh.vertices[3]);

        //int[] tris = new int[6]
        //{
        //    1, 2, 0,
        //    1, 3, 2
        //};
        int[] tris = new int[12]
        {
            1, 2, 0,
            1, 3, 2,
            0, 2, 1,
            2, 3, 1
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;

        newQuad.SetActive(false);
    }//end spawnNewQuad()

    void resizeCompositingQuads()
    {
        float orthoSize = compositingCamera.GetComponent<Camera>().orthographicSize * 2.0f;
        float[] widths = new float[numberScreens];
        if (orthoSize != lastCompositingCameraOrthoSize)
        {
            for (int i = 0; i < numberScreens; i++)
            {
                var fts = caveScreens[i].GetComponent<FishTankSurface>();
                //set size
                CompositingQuads[i].transform.localScale = new Vector3(orthoSize * fts.aspectRatio, orthoSize, 1);
                widths[i] = orthoSize * fts.aspectRatio;
                //set position
                //CompositingQuads[i].transform.localPosition = new Vector3(orthoSize * caveScreens[i].GetComponent<CaveScreen>().aspectRatio, orthoSize, 1);
            }

            //figure out positioning
            float totalWidth = 0;
            for (int i = 0; i < numberScreens; i++)
            {
                totalWidth  += widths[i];
            }
            float halfTotalWidth = totalWidth / 2.0f;
            float xPos = -halfTotalWidth;
            for (int i = 0; i < numberScreens; i++)
            {
                //set size
                Vector3 newPos = CompositingQuads[i].transform.position;
                newPos.x = xPos + (widths[i] / 2.0f);
                CompositingQuads[i].transform.position = newPos;
                xPos += widths[i];
                //set position
                //CompositingQuads[i].transform.localPosition = new Vector3(orthoSize * caveScreens[i].GetComponent<CaveScreen>().aspectRatio, orthoSize, 1);
            }

            lastCompositingCameraOrthoSize = orthoSize;
        }//end if orthosize changed

    }


    List<GameObject> ReadCalibration()
    {
        var screens = new List<GameObject>();

        Debug.Log("Reading Calibration File " + calibrationFilename);
        XmlReader reader = XmlReader.Create(Application.dataPath + "//" + calibrationFilename);

        reader.MoveToContent();

        if (reader.NodeType == XmlNodeType.Element && reader.Name != "FishTank")
        {
            Debug.Log("File " + calibrationFilename + " does not appear to be a valid FishTank calibration file!");
        }
        else
        {
            while (reader.ReadToFollowing("screen"))
            {
                float x, y, z;
                Vector3 topleft, topright, bottomright, bottomleft;


                int screenNum = int.Parse(reader.GetAttribute("number"));
                Debug.Log("Reading Screen " + screenNum);

                reader.ReadToDescendant("topleft"); // move to topLeft corner
                x = Single.Parse(reader.GetAttribute("x"));
                y = Single.Parse(reader.GetAttribute("y"));
                z = Single.Parse(reader.GetAttribute("z"));
                topleft = new Vector3(x, y, z);

                reader.ReadToNextSibling("topright"); // move to topright corner
                x = Single.Parse(reader.GetAttribute("x"));
                y = Single.Parse(reader.GetAttribute("y"));
                z = Single.Parse(reader.GetAttribute("z"));
                topright = new Vector3(x, y, z);

                reader.ReadToNextSibling("bottomright"); // move to bottomright corner
                x = Single.Parse(reader.GetAttribute("x"));
                y = Single.Parse(reader.GetAttribute("y"));
                z = Single.Parse(reader.GetAttribute("z"));
                bottomright = new Vector3(x, y, z);

                reader.ReadToNextSibling("bottomleft"); // move to bottomleft corner
                x = Single.Parse(reader.GetAttribute("x"));
                y = Single.Parse(reader.GetAttribute("y"));
                z = Single.Parse(reader.GetAttribute("z"));
                bottomleft = new Vector3(x, y, z);

                GameObject tmp = new GameObject("Screen " + screenNum.ToString());
                var fts = tmp.AddComponent<FishTankSurface>();
                fts.screenNumber = screenNum;
                fts.topLeft = topleft;
                fts.topRight = topright;
                fts.bottomRight = bottomright;
                fts.bottomLeft = bottomleft;

                screens.Add(tmp);
            }
        }

        reader.Close();

        return screens;
    }
}
