using UnityEngine;
using UnityEngine.InputSystem;
using Valve.VR;

public class SteamVRTrackedDeviceAssigner : MonoBehaviour
{
    public string preferredTrackedDeviceName = "{htc}vr_tracker_vive_1_0";

    public System.Collections.Generic.List<GameObject> gameObjectsToAssignTracker;

    private int trackerID = -1;

    // Start is called before the first frame update
    void Start()
    {
        Update();
    }

    void Update()
    {
        if (GetTrackerID())
            AddTrackerComponentToGameObjects();

        if (Keyboard.current.tKey.wasPressedThisFrame)
            foreach (var g in gameObjectsToAssignTracker)
            {
                var trackedDeviceComponent = g.GetComponent<SteamVR_TrackedObject>();

                if (trackedDeviceComponent)
                {
                    trackedDeviceComponent.enabled = !trackedDeviceComponent.enabled;
                }
            }
    }

    bool GetTrackerID()
    {
        //help user find device number for their tracker by printing device IDs
        if (OpenVR.System != null)
        {
            for (int i = 0; i < 9; i++)
            {

                var error = ETrackedPropertyError.TrackedProp_Success;
                uint capacity = OpenVR.System.GetStringTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0, ref error);
                if (capacity <= 1)
                {
                    continue;
                }

                var buffer = new System.Text.StringBuilder((int)capacity);
                OpenVR.System.GetStringTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_RenderModelName_String, buffer, capacity, ref error);

                //if (buffer.ToString().Contains("{htc}vr_tracker_vive_1_0") || buffer.ToString().Contains("{htc}vr_tracker_vive_3_0"))
                if (buffer.ToString().Contains(preferredTrackedDeviceName))
                {
                    trackerID = i;
                    return true;
                }
            }
        }

        return false;
    }

    void AddTrackerComponentToGameObjects()
    {
        foreach (var g in gameObjectsToAssignTracker)
        {
            var trackedDeviceComponent = g.GetComponent<SteamVR_TrackedObject>();

            if (!trackedDeviceComponent)
            {
                trackedDeviceComponent = g.AddComponent<SteamVR_TrackedObject>();
            }

            trackedDeviceComponent.index = (SteamVR_TrackedObject.EIndex)trackerID;
        }
    }
}
