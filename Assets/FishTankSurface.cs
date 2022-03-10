using UnityEngine;
using UnityEditor;

public class FishTankSurface : MonoBehaviour
{
    public int screenNumber;

    public Vector3 topLeft;
    public Vector3 topRight;
    public Vector3 bottomRight;
    public Vector3 bottomLeft;
    
    public Vector3 center;
    public Vector3 right;
    public Vector3 up;
    public Vector3 normal;

    public float height;
    public float width;
    public float aspectRatio;

    public Matrix4x4 m;

    public void Recalculate()
    {
        height = Vector3.Distance(topLeft, bottomLeft);
        width = Vector3.Distance(topLeft, topRight);
        aspectRatio = width / height;

        center = (topLeft + bottomRight) / 2;
        
        right = (bottomRight - bottomLeft).normalized;
        up = (topLeft - bottomLeft).normalized;

        normal = Vector3.Cross(up, right).normalized;
        
        m = Matrix4x4.zero;
        m[0, 0] = right.x;
        m[0, 1] = right.y;
        m[0, 2] = right.z;

        m[1, 0] = up.x;
        m[1, 1] = up.y;
        m[1, 2] = up.z;

        m[2, 0] = normal.x;
        m[2, 1] = normal.y;
        m[2, 2] = normal.z;

        m[3, 3] = 1.0f;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(FishTankSurface))]
public class FishTankSurfaceVisualizaer : Editor
{
    // Custom in-scene UI for when ExampleScript
    // component is selected.
    public void OnSceneGUI()
    {
        var t = target as FishTankSurface;
        var tr = t.transform;
        var center = tr.TransformPoint( t.bottomLeft + ((t.topLeft - t.bottomLeft) + (t.bottomRight - t.bottomLeft)) * 0.5f);
        // display an orange disc where the object is
        var color = new Color(1, 0.8f, 0.4f, 1);
        Handles.color = color;
        Handles.DrawPolyLine(
            tr.TransformPoint(t.topLeft),
            tr.TransformPoint(t.topRight),
            tr.TransformPoint(t.bottomRight),
            tr.TransformPoint(t.bottomLeft),
            tr.TransformPoint(t.topLeft)
        );
        // display object "value" in scene
        GUI.color = color;
        Handles.Label(center, "Screen " + t.screenNumber.ToString());
    
        Handles.color = Color.black;
        Handles.Label(tr.TransformPoint(t.topLeft), "Top Left");
        Handles.Label(tr.TransformPoint(t.topRight), "Top Right");
        Handles.Label(tr.TransformPoint(t.bottomLeft), "Bottom Left");
        Handles.Label(tr.TransformPoint(t.bottomRight), "Bottom Right");
    }
}
#endif