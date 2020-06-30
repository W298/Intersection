using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatePathManager : MonoBehaviour
{
    public enum MODE { BUILD, APPEND, REMOVE, NONE };

    private Camera cm;
    public SplineComputer spline;
    public SplineComputer SplinePreFab;
    public GameObject debugobj;
    public int snapsize = 5;
    public MODE current_mode = MODE.NONE;

    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;

    private float last_x;
    private float last_z;
    private int new_index = 0;

    private Vector3 pos;

    // Start is called before the first frame update
    void Start()
    {
        cm = GetComponentInChildren<Camera>();

        // new_index = spline.pointCount;
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

    void AppendPath()
    {
        Ray ray = cm.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitData;

        if (Physics.Raycast(ray, out hitData, 10000))
        {
            pos = hitData.point;

            UnityEngine.Debug.LogWarning(hitData.collider.gameObject.ToString());
        }

        debugobj.GetComponent<Transform>().position = new Vector3(SnapGrid(pos.x, snapsize), 0, SnapGrid(pos.z, snapsize));

        if (last_x != SnapGrid(pos.x, snapsize) || last_z != SnapGrid(pos.z, snapsize))
        {
            UnityEngine.Debug.LogWarning(new_index.ToString() + " Point Created!");

            spline.SetPointNormal(new_index, def_normal);
            spline.SetPointSize(new_index, 1);
            spline.SetPointPosition(new_index, new Vector3(SnapGrid(pos.x, snapsize), def_y, SnapGrid(pos.z, snapsize)));

            last_x = SnapGrid(pos.x, snapsize);
            last_z = SnapGrid(pos.z, snapsize);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            UnityEngine.Debug.LogWarning("Build Mode Enabled!");
            current_mode = MODE.BUILD;
        }

        if (current_mode == MODE.BUILD)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cm.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitData;

                if (Physics.Raycast(ray, out hitData, 10000))
                {
                    pos = hitData.point;
                }

                SpawnPath();

                AppendPath();
                new_index++;

                current_mode = MODE.APPEND;
            }
        }
        else if (current_mode == MODE.APPEND)
        {
            if (Input.GetMouseButton(0))
            {
                AppendPath();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                new_index++;
            }
        }
    }
}
