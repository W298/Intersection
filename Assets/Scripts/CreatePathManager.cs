using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatePathManager : MonoBehaviour
{
    private Camera cm;
    public SplineComputer spline;
    public SplineComputer SplinePreFab;
    public GameObject debugobj;
    public int snapsize = 5;

    private float last_x;
    private float last_z;
    private int new_index;

    private Vector3 pos;

    // Start is called before the first frame update
    void Start()
    {
        cm = GetComponentInChildren<Camera>();

        new_index = spline.pointCount;
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

    void SpawnPath()
    {
        spline = Instantiate(SplinePreFab, pos, Quaternion.identity);
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

                UnityEngine.Debug.LogWarning(hitData.collider.gameObject.ToString());
            }

            debugobj.GetComponent<Transform>().position = new Vector3(SnapGrid(pos.x, snapsize), 0, SnapGrid(pos.z, snapsize));
            SpawnPath();

            if (last_x != SnapGrid(pos.x, snapsize) || last_z != SnapGrid(pos.z, snapsize))
            {
                UnityEngine.Debug.LogWarning(new_index.ToString() + " Point Created!");

                spline.SetPointNormal(new_index, spline.GetPointNormal(0));
                spline.SetPointSize(new_index, 1);
                spline.SetPointPosition(new_index, new Vector3(SnapGrid(pos.x, snapsize), spline.GetPointPosition(0).y, SnapGrid(pos.z, snapsize)));

                last_x = SnapGrid(pos.x, snapsize);
                last_z = SnapGrid(pos.z, snapsize);     
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            new_index++;

            UnityEngine.Debug.LogWarning("Up!");
        }
    }
}
