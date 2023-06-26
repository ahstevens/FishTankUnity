using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARLabelPositioner : MonoBehaviour
{
    public List<GameObject> labeledObjects = new();

    [SerializeField]
    GameObject labelPrefab;

    [SerializeField]
    Camera arCam;

    [SerializeField]
    Canvas canvas;

    Dictionary<GameObject, GameObject> labelMap = new();

    // Start is called before the first frame update
    void Start()
    {
            
    }

    // Update is called once per frame
    void Update()
    {
        int count = 0;
        foreach (GameObject go in labeledObjects)
        {
            //check for new targets and create their labels
            if (!labelMap.ContainsKey(go))
            {
                var lab = Instantiate(labelPrefab, canvas.transform, false);
                labelMap[go] = lab;
                lab.name = go.name + " Label";
                var txt = lab.GetComponent<TextMeshProUGUI>();
                txt.text = go.name;
            }

            var label = labelMap[go];

            var screenPos = arCam.WorldToScreenPoint(go.transform.position + Vector3.up * 40);

            float h = arCam.pixelHeight;
            float w = arCam.pixelWidth;

            if (screenPos.x > w || screenPos.x < 0 ||
                screenPos.y > h || screenPos.y < 0)
            {
                label.SetActive(false);
                return;
            }

            label.SetActive(true);

            float x = screenPos.x - (w / 2);
            float y = screenPos.y - (h / 2);
            float s = canvas.scaleFactor;
            label.GetComponent<TextMeshProUGUI>().rectTransform.anchoredPosition = new Vector2(x, y) / s;
            var currentPos = label.GetComponent<TextMeshProUGUI>().rectTransform.localPosition;
            label.GetComponent<TextMeshProUGUI>().rectTransform.localPosition = new Vector3(currentPos.x, currentPos.y, -0.1f * count);
            count++;
        }
    }
}
