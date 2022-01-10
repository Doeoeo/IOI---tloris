using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionDrawer : MonoBehaviour {

    [SerializeField] private Text areaText;

    private LineRenderer lineRenderer;
    private Vector3 startingPos, currentPos;
    private float area;
    // Start is called before the first frame update
    void Start() {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        areaText.text = area + "m";
    }
 
}
