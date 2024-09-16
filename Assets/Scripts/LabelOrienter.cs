using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelOrienter : MonoBehaviour
{
    [SerializeField]
    Transform labelCamera;

    public float DegreesVisualAngle = 65;

    float aspectRatio;

    // Start is called before the first frame update
    void Start()
    {
        if (labelCamera == null)
        {
            labelCamera = Camera.main.transform;
        }

        aspectRatio = transform.localScale.y / transform.localScale.x;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float visualAngle = DegreesVisualAngle * 0.01745329252f; //5 degrees to radians

        var dist = Mathf.Abs(Vector3.Distance(gameObject.transform.position, labelCamera.transform.position));
        float newSize = Mathf.Abs(Mathf.Tan(visualAngle) * dist) / 10f;
        transform.localScale = new Vector3(newSize, aspectRatio * newSize, 1);

        transform.rotation = labelCamera.transform.rotation;
    }
}
