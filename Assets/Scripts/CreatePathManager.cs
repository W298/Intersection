using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatePathManager : MonoBehaviour
{
    private Camera cm;
    public SplineComputer spline;
    public GameObject debugobj;

    public float last_x;
    public float last_z;

    private Vector3 pos;

    // Start is called before the first frame update
    void Start()
    {
        cm = GetComponentInChildren<Camera>();
    }

    float SnapGrid(float value, int snapsize)
    {
        if (value < 0)
        {
            return Mathf.Round(Mathf.Abs(value / snapsize)) * snapsize * -1;
        }
        else
        {
            return Mathf.Round(value / snapsize) * snapsize;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = cm.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitData;

            if (Physics.Raycast(ray, out hitData, 10000))
            {
                pos = hitData.point;
            }

            debugobj.GetComponent<Transform>().position = new Vector3(SnapGrid(pos.x, 5), 0, SnapGrid(pos.z, 5));

            if (last_x != SnapGrid(pos.x, 5) || last_z != SnapGrid(pos.z, 5))
            {
                UnityEngine.Debug.LogWarning("Detect!");

                spline.SetPointSize(6, 1);
                spline.SetPointPosition(6, new Vector3(SnapGrid(pos.x, 5), spline.GetPointPosition(0).y, SnapGrid(pos.z, 5)));

                last_x = SnapGrid(pos.x, 5);
                last_z = SnapGrid(pos.z, 5);

                
            }
        }
    }
}
