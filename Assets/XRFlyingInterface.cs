///////////////////////////////////////////////////////////////////////////////
//  XR 3D Flying Interface Script for Unity                                  //
//  Copyright 2021 CCOM Data Visualization Research Lab                      //
//  URL: https://ccom.unh.edu/vislab                                         //
//                                                                           //
//  Redistribution and use in source and binary forms, with or without       //
//  modification, are permitted provided that the following conditions       //
//  are met:                                                                 //
//                                                                           //
//  1. Redistributions of source code must retain the above copyright        //
//     notice, this list of conditions and the following disclaimer.         //
//                                                                           //
//  2. Redistributions in binary form must reproduce the above copyright     //
//     notice, this list of conditions and the following disclaimer in the   //
//     documentation and/or other materials provided with the distribution.  //
//                                                                           //
//  3. Neither the name of the copyright holder nor the names of its         //
//     contributors may be used to endorse or promote products derived from  //
//     this software without specific prior written permission.              //
//                                                                           //
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS      //
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT        //
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A  //
//  PARTICULAR PURPOSE ARE DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER //
//  OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, //
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,      //
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR       //
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF   //
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING     //
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS       //
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.             //
///////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using Valve.VR;

public class XRFlyingInterface : MonoBehaviour
{
    public bool desktopFlyingMode;

    public float movementThreshold = 0.01f;
    public float translationMultiplier = 100f;
    public float rotationMultiplier = 0.05f;

    public GameObject flyingVehicle;
    
    public GameObject bat;

    public SteamVR_ActionSet actionSet;
    public SteamVR_Action_Boolean setReference;
    public SteamVR_Action_Boolean fly;

    private bool flying;
    private bool beyondThreshold;
    private GameObject trackingReference;
    private GameObject flyingOrigin;


    // Start is called before the first frame update
    void Start()
    {
        if (flyingVehicle == null)
            flyingVehicle = this.gameObject;

        actionSet.Activate();

        flying = false;
        beyondThreshold = false;

        // In VR, tracking reference can just be the scene origin
        // In Desktop mode, the tracking reference is relative to the display.
        if (!desktopFlyingMode)
        {
            trackingReference = new GameObject("Tracking Reference");
            trackingReference.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (setReference.GetState(SteamVR_Input_Sources.Any) && desktopFlyingMode)
            OnSetReference();

        if (fly.GetState(SteamVR_Input_Sources.Any) && !flying)
            OnBeginFly();

        if (!fly.GetState(SteamVR_Input_Sources.Any) && flying)
            OnEndFly();


        if (flying)
        {
            //Debug.Log("Flying...");
            Vector3 relativeTranslation = bat.transform.position - flyingOrigin.transform.position;
            float relativeTranslationMag = relativeTranslation.magnitude;

            if (relativeTranslationMag >= movementThreshold)
            {
                if (!beyondThreshold)
                {
                    beyondThreshold = true;
                    //Debug.Log("Moving...");
                }


                float displacementCubed = Mathf.Pow(relativeTranslationMag, 3);

                Vector3 cameraOffset = trackingReference.transform.InverseTransformDirection(relativeTranslation);
                cameraOffset = flyingVehicle.transform.TransformDirection(cameraOffset).normalized;

                flyingVehicle.transform.position = flyingVehicle.transform.position + cameraOffset * displacementCubed * translationMultiplier;
            }
            else
            {
                if (beyondThreshold)
                {
                    beyondThreshold = false;
                    //Debug.Log("Stopped moving...");
                }
            }

            Quaternion relativeRotation = Quaternion.Inverse(flyingOrigin.transform.rotation) * bat.transform.rotation;

            flyingVehicle.transform.rotation *= Quaternion.Slerp(Quaternion.identity, relativeRotation, rotationMultiplier);
        }
    }

    void OnSetReference()
    {
        if (trackingReference == null)
            trackingReference = new GameObject("Tracking Reference");

        trackingReference.transform.SetPositionAndRotation(bat.transform.position, bat.transform.rotation);
        
        Debug.Log("Tracking Reference Set!");
    }

    void OnBeginFly()
    {
        if (trackingReference == null)
        {
            Debug.Log("Tracking Reference needs to be set! Flying disabled.");
            return;
        }

        flyingOrigin = new GameObject("Flying Origin");
        flyingOrigin.transform.SetPositionAndRotation(bat.transform.position, bat.transform.rotation);

        flying = true;
        Debug.Log("Flying started");
    }

    void OnEndFly()
    {
        if (flying)
        {
            Destroy(flyingOrigin);

            flying = false;
            beyondThreshold = false;
            Debug.Log("Flying ended");
        }
    }
}
