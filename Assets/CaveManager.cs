using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

public class CaveManager : MonoBehaviour
{
    public GameObject fishTankParent;
    public GameObject ViewerCameraPrefab;
    public GameObject CompositingQuadPrefab;
    public GameObject viewer;
    public GameObject compositingCamera;

    public string calibrationFilename = "calibration.xml";

    public int renderTextureWidth;
    public int renderTextureHeight;

    bool clampNearPlane = true;

    private List<GameObject> caveScreens;
    private List<GameObject> screenCameras;
    private List<GameObject> CompositingQuads;
    private List<RenderTexture> renderTextures;
    private int numberScreens;
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
            s.GetComponent<FishTankSurface>().Recalculate();            
        }

        numberScreens = caveScreens.Count;

        for (int i = 0; i < numberScreens; i++)
        {
            RenderTexture thisRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            //thisRenderTexture.antiAliasing = 2;  //what do?
            thisRenderTexture.Create();
            renderTextures.Add(thisRenderTexture);


            GameObject thisCamera = Instantiate(ViewerCameraPrefab, Vector3.zero, Quaternion.identity);
            thisCamera.name = "Screen Camera " + i;
            thisCamera.tag = "Point Cloud Render Camera";
            thisCamera.GetComponent<Camera>().nearClipPlane = 0.1f;
            thisCamera.GetComponent<Camera>().farClipPlane = 10000;
            thisCamera.GetComponent<Camera>().targetTexture = renderTextures[i];
            thisCamera.GetComponent<Camera>().cullingMask = ~(1 << LayerMask.NameToLayer("CompCamera"));
            thisCamera.transform.parent = viewer.transform;
            thisCamera.transform.SetPositionAndRotation(viewer.transform.position, viewer.transform.rotation);

            screenCameras.Add(thisCamera);

            GameObject thisCompositingQuad = Instantiate(CompositingQuadPrefab, new Vector3(0 + (2 * i), 0, 1), Quaternion.identity, compositingCamera.transform);
            thisCompositingQuad.name = "Compositing Quad " + i;
            thisCompositingQuad.GetComponent<MeshRenderer>().material.mainTexture = renderTextures[i];

            CompositingQuads.Add(thisCompositingQuad);
        }//for each screen
    }

    // Update is called once per frame
    void Update()
    {
        ResizeCompositingQuads();

        RecalculateProjectionsFromViewpoint(viewer.transform.localPosition);

    }//end Update()

    void ResizeCompositingQuads()
    {
        float orthoSize = compositingCamera.GetComponent<Camera>().orthographicSize * 2.0f;
        float[] widths = new float[numberScreens];
        if (orthoSize != lastCompositingCameraOrthoSize)
        {
            float totalWidth = 0;
            for (int i = 0; i < numberScreens; i++)
            {
                var fts = caveScreens[i].GetComponent<FishTankSurface>();
                //set size
                CompositingQuads[i].transform.localScale = new Vector3(orthoSize * fts.aspectRatio, orthoSize, 1);
                widths[i] = orthoSize * fts.aspectRatio;
                totalWidth += widths[i];
            }

            //figure out positioning
            float xPos = -(totalWidth * 0.5f);
            for (int i = 0; i < numberScreens; i++)
            {
                //set size
                Vector3 newPos = CompositingQuads[i].transform.localPosition;
                newPos.x = xPos + (widths[i] / 2.0f);
                CompositingQuads[i].transform.localPosition = newPos;
                xPos += widths[i];
            }

            lastCompositingCameraOrthoSize = orthoSize;
        }//end if orthosize changed
    }

    void RecalculateProjectionsFromViewpoint(Vector3 viewpoint)
    {
        for (int i = 0; i < numberScreens; ++i)
        {
            var fts = caveScreens[i].GetComponent<FishTankSurface>();

            Vector3 pe = fishTankParent.transform.TransformPoint(viewpoint);

            Vector3 pa = fishTankParent.transform.TransformPoint(fts.bottomLeft);
            Vector3 pb = fishTankParent.transform.TransformPoint(fts.bottomRight);
            Vector3 pc = fishTankParent.transform.TransformPoint(fts.topLeft);
            Vector3 pd = fishTankParent.transform.TransformPoint(fts.topRight);

            Vector3 vr = fishTankParent.transform.TransformDirection(fts.right);
            Vector3 vu = fishTankParent.transform.TransformDirection(fts.up);
            Vector3 vn = fishTankParent.transform.TransformDirection(fts.normal);

            //From eye to projection screen corners
            Vector3 va = pa - pe;
            Vector3 vb = pb - pe;
            Vector3 vc = pc - pe;
            Vector3 vd = pd - pe;

            Vector3 viewDir = pe + va + vb + vc + vd;

            //distance from eye to projection screen plane
            float d = -Vector3.Dot(va, vn);
            if (clampNearPlane)
                screenCameras[i].GetComponent<Camera>().nearClipPlane = d;
            float n = screenCameras[i].GetComponent<Camera>().nearClipPlane;
            float f = screenCameras[i].GetComponent<Camera>().farClipPlane;

            float nearOverDist = n / d;
            float l = Vector3.Dot(vr, va) * nearOverDist;
            float r = Vector3.Dot(vr, vb) * nearOverDist;
            float b = Vector3.Dot(vu, va) * nearOverDist;
            float t = Vector3.Dot(vu, vc) * nearOverDist;
            Matrix4x4 P = Matrix4x4.Frustum(l, r, b, t, n, f);

            Matrix4x4 M = fts.m;

            //Translation to eye position
            Matrix4x4 T = Matrix4x4.Translate(-pe);
            Matrix4x4 R = Matrix4x4.Rotate(Quaternion.Inverse(fishTankParent.transform.rotation));

            var sc = screenCameras[i].GetComponent<Camera>();
            sc.projectionMatrix = P;
            sc.nonJitteredProjectionMatrix = P;
            sc.worldToCameraMatrix = M * R * T;

        }//end for i (each screenCamera)
    }

    List<GameObject> ReadCalibration()
    {
        var screens = new List<GameObject>();

        //Debug.Log("Reading Calibration File " + calibrationFilename);
        XmlReader reader = XmlReader.Create(Application.dataPath + "//" + calibrationFilename);

        reader.MoveToContent();

        if (reader.NodeType == XmlNodeType.Element && reader.Name != "FishTank")
        {
            ;// Debug.Log("File " + calibrationFilename + " does not appear to be a valid FishTank calibration file!");
        }
        else
        {
            while (reader.ReadToFollowing("screen"))
            {
                float x, y, z;
                Vector3 topleft, topright, bottomright, bottomleft;


                int screenNum = int.Parse(reader.GetAttribute("number"));
                //Debug.Log("Reading Screen " + screenNum);

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
                if (fishTankParent)
                    tmp.transform.parent = fishTankParent.transform;
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

    public List<GameObject> GetCaveScreens()
    {
        return caveScreens;
    }

    public List<GameObject> GetScreenCameras()
    {
        return screenCameras;
    }
}
