using Dreamteck;
using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatePathManager : MonoBehaviour
{
    public enum MODE { BUILD, APPEND, REMOVE, NONE };

    private Camera cm;
    private SplineComputer spline_computer;
    public SplineComputer SplinePrefab;
    public GameObject debugobj;
    public int snapsize = 5;
    public MODE current_mode = MODE.NONE;

    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;

    private float last_x;
    private float last_z;
    private int new_index = 0;

    private Vector3 pos;

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
        if (spline_computer)
        {
            spline_computer = null;
            new_index = 0;
        }

        spline_computer = Instantiate(SplinePrefab, pos, Quaternion.identity);
    }

    void AppendPath()
    {
        if (last_x != SnapGrid(pos.x, snapsize) || last_z != SnapGrid(pos.z, snapsize))
        {
            spline_computer.SetPointNormal(new_index, def_normal);
            spline_computer.SetPointSize(new_index, 1);
            spline_computer.SetPointPosition(new_index, new Vector3(SnapGrid(pos.x, snapsize), def_y, SnapGrid(pos.z, snapsize)));

            last_x = SnapGrid(pos.x, snapsize);
            last_z = SnapGrid(pos.z, snapsize);
        }
    }

    void RemovePoint(int index)
    {
        // WARNING - To make this function work, I changed below thing.
        // CHANGED - Changed `spline` variable in SplineComputer to public (from private)
        // Body of this function refered DeletePointModule.cs

        SplinePoint[] p = spline_computer.spline.points;

        if (index < p.Length && index >= 0)
        {
            ArrayUtility.RemoveAt(ref p, index);
            spline_computer.spline.points = p;
        }
        else
        {
            UnityEngine.Debug.LogError("Out of Index! (RemovePoint)");
        }

        if (spline_computer)
        {
            spline_computer.Rebuild(true);
        }
    }

    void DebugPoints()
    {
        // Show current Points
        if (spline_computer)
        {
            SplinePoint[] points = spline_computer.GetPoints();

            if (points.Length != 0)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Instantiate(debugobj, points[i].position, Quaternion.identity);
                }
            }
        }
    }

    void RayTrace()
    {
        Ray ray = cm.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitData;

        if (Physics.Raycast(ray, out hitData, 10000))
        {
            pos = hitData.point;
        }
    }

    void Start()
    {
        cm = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        RayTrace();

        debugobj.GetComponent<Transform>().position = new Vector3(SnapGrid(pos.x, snapsize), 0, SnapGrid(pos.z, snapsize));

        // Change MODE
        if (Input.GetKeyDown(KeyCode.B))
        {
            UnityEngine.Debug.LogWarning("Build Mode Enabled!");
            current_mode = MODE.BUILD;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.Debug.LogWarning("Remove Mode Enabled!");
            current_mode = MODE.REMOVE;
        }

        // MODE BEHAVIOR
        if (current_mode == MODE.BUILD)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SpawnPath();

                if (spline_computer)
                {
                    spline_computer.Rebuild(true);
                }

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

                if (spline_computer)
                {
                    spline_computer.Rebuild(true);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                new_index++;
            }
            else if (Input.GetMouseButton(1))
            {
                if (Input.GetMouseButtonDown(1))
                {
                    UnityEngine.Debug.LogWarning("Undo Last Point Creation");

                    RemovePoint(new_index - 1);
                    new_index--;
                }

                if (spline_computer)
                {
                    spline_computer.Rebuild(true);
                }
            }
        }
        else if (current_mode == MODE.REMOVE)
        {
            if (Input.GetMouseButtonDown(0))
            {
                DebugPoints();

                SplinePoint[] p = spline_computer.GetPoints();

                for (int i = 0; i < p.Length; i++)
                {
                    UnityEngine.Debug.LogWarning(Vector3.Distance(p[i].position, pos).ToString());
                    if (Vector3.Distance(p[i].position, pos) < 1)
                    {
                        RemovePoint(i);
                    }
                }
            }
        }
    }
}
