using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField]
    Transform billboardTo;

    // Start is called before the first frame update
    void Start()
    {
        if (billboardTo == null)
        {
            billboardTo = Camera.main.transform;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(billboardTo.transform.position, Vector3.up);
        transform.Rotate(Vector3.up * 180);
    }
}
