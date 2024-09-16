using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleENC : MonoBehaviour
{
    [SerializeField]
    GameObject enc;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame) 
        {
            enc.SetActive(!enc.activeSelf);
        }
    }
}
