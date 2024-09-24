using UnityEngine;
using UnityEditor;

// Declare type of Custom Editor
[CustomEditor(typeof(CaveManager))] //1
public class CaveManagerEditor : Editor
{
    //float thumbnailWidth = 70;
    //float thumbnailHeight = 70;
    //float labelWidth = 150f;

    // OnInspector GUI
    public override void OnInspectorGUI() //2
    {

        // Call base class method
        base.DrawDefaultInspector();

        // Custom form for Player Preferences
        CaveManager cm = (CaveManager)target;
    }
}