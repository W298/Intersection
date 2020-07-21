using Dreamteck;
using Dreamteck.Splines;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatePathManager : MonoBehaviour
{
    public enum MODE { BUILD, APPEND, REMOVE, NONE };

    private Camera cm;
    public SplineComputer spline_computer;

    public SplineComputer SplinePrefab;
    public GameObject debugobj;
    public GameObject debugobj2;
    public int snapsize = 10;
    public MODE current_mode = MODE.NONE;

    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;
    private float last_x;
    private float last_z;
    private Vector3 last_pos;
    private int new_index = 0;
    private Vector3 pos;
    private Vector3 snap_pos;
    private bool isJoin = false;

    public SplineComputer cross_old_spline;
    public SplineComputer cross_new_spline;
    private SplineComputer cross_current_spline;

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

    Vector3 SnapToGridPoint(Vector3 pos, int _snapsize)
    {
        float snapsize = (float)_snapsize;

        if (!isVectorInXZArea(pos, -snapsize + last_pos.x, snapsize + last_pos.x, 
            -snapsize + last_pos.z, snapsize + +last_pos.z))
        {
            UnityEngine.Debug.LogWarning("Out of range!");
        }
        else
        {
            if (isVectorInXZArea(pos, snapsize / 2 + last_pos.x, snapsize + last_pos.x,
            last_pos.z - snapsize / 4, last_pos.z + snapsize / 4))
            {
                last_pos = last_pos + new Vector3(snapsize, 0, 0);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x - snapsize, last_pos.x - snapsize / 2,
                last_pos.z - snapsize / 4, last_pos.z + snapsize / 4))
            {
                last_pos = last_pos - new Vector3(snapsize, 0, 0);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x - snapsize / 4, last_pos.x + snapsize / 4,
                last_pos.z + snapsize / 2, last_pos.z + snapsize))
            {
                last_pos = last_pos + new Vector3(0, 0, snapsize);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x - snapsize / 4, last_pos.x + snapsize / 4,
                last_pos.z - snapsize, last_pos.z - snapsize / 2))
            {
                last_pos = last_pos - new Vector3(0, 0, snapsize);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x + snapsize / 2, last_pos.x + snapsize,
                last_pos.z + snapsize / 2, last_pos.z + snapsize))
            {
                last_pos = last_pos + new Vector3(snapsize, 0, snapsize);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x - snapsize, last_pos.x - snapsize / 2,
                last_pos.z + snapsize / 2, last_pos.z + snapsize))
            {
                last_pos = last_pos + new Vector3(-snapsize, 0, snapsize);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x + snapsize / 2, last_pos.x + snapsize,
                last_pos.z - snapsize, last_pos.z - snapsize / 2))
            {
                last_pos = last_pos + new Vector3(snapsize, 0, -snapsize);
                return last_pos;
            }
            else if (isVectorInXZArea(pos, last_pos.x - snapsize, last_pos.x - snapsize / 2,
                last_pos.z - snapsize, last_pos.z - snapsize / 2))
            {
                last_pos = last_pos - new Vector3(snapsize, 0, snapsize);
                return last_pos;
            }
        }

        return last_pos;
    }


    // Check Append Point is at Valid Position.
    bool CheckAppendValid(Vector3 addPoint)
    {
        int last_id = spline_computer.GetPoints().Length - 1;

        Vector3 dir = spline_computer.GetPoint(last_id).position - 
            spline_computer.GetPoint(last_id - 1).position;

        Vector3 dirAppend = addPoint - spline_computer.GetPoint(last_id).position;

        if (Vector3.Angle(dir, dirAppend) <= 90)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool isVectorInXZArea(Vector3 pos, float x_from, float x_to, float z_from, float z_to)
    {
        bool cond_1 = x_from <= pos.x && pos.x <= x_to;
        bool cond_2 = z_from <= pos.z && pos.z <= z_to;

        return cond_1 && cond_2;
    }

    void debugPoint(Vector3 pos)
    {
        Instantiate(debugobj2, pos, Quaternion.identity);
    }

    // Spawn SplineComputer and Apply to spline_computer variable.
    void SpawnPath()
    {
        if (spline_computer)
        {
            spline_computer = null;
            new_index = 0;
        }

        spline_computer = Instantiate(SplinePrefab, pos, Quaternion.identity);
    }

    // Spawn SplineComputer independently.
    SplineComputer InsPath(Vector3 pos)
    {
        return Instantiate(SplinePrefab, pos, Quaternion.identity);
    }

    // Append path when snapping event on. Return true when snapping event on.
    bool AppendPath()
    {
        if (last_x != SnapToGridPoint(pos, snapsize).x || last_z != SnapToGridPoint(pos, snapsize).z)
        {
            float x = SnapToGridPoint(pos, snapsize).x;
            float z = SnapToGridPoint(pos, snapsize).z;

            if (CheckAppendValid(new Vector3(x, 0, z)) || spline_computer.GetPoints().Length == 1)
            {
                spline_computer.SetPointNormal(new_index, def_normal);
                spline_computer.SetPointSize(new_index, 1);
                spline_computer.SetPointPosition(new_index, new Vector3(x, def_y, z));

                last_x = x;
                last_z = z;

                return true;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Point is out of range!");
            }
        }
        
        return false;
    }

    // Append Point at desire position.
    void AppendPath(Vector3 pos)
    {
        spline_computer.SetPointNormal(new_index, def_normal);
        spline_computer.SetPointSize(new_index, 1);
        spline_computer.SetPointPosition(new_index, pos);
    }

    // WARNING - To make this function work, I changed below thing.
    // CHANGED - Changed `spline` variable in SplineComputer to public (from private)
    // Body of this function refered DeletePointModule.cs
    void RemovePoint(int index)
    {
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

    // Remove point with point ref.
    void RemovePoint(SplinePoint point)
    {
        SplinePoint[] p = spline_computer.spline.points;
        
        ArrayUtility.RemoveAt(ref p, ArrayUtility.IndexOf(p, point));
        spline_computer.spline.points = p;

        if (spline_computer)
        {
            spline_computer.Rebuild(true);
        }
    }

    // Simply Raytrace and Set mouse position.
    void RayTrace()
    {
        Ray ray = cm.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitData;

        if (Physics.Raycast(ray, out hitData, 10000))
        {
            pos = hitData.point;
        }
    }

    // Spawn SplineComputer and Change mode to Append.
    void runBuildMode()
    {
        new_index = 0;

        SpawnPath();

        if (spline_computer)
        {
            spline_computer.Rebuild(true);
        }
        
        AppendPath();
        new_index++;

        current_mode = MODE.APPEND;
    }

    void runAppendMode()
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

    // Append Point when snapping event on. Also Handle Cleaning Joined Path.
    void runAppendModeGrid()
    {
        if (Input.GetMouseButton(0))
        {
            if (AppendPath())
            {
                if (isJoin)
                {
                    CleanLines();

                    new_index = 0;
                    SpawnPath();
                    AppendPath(new Vector3(SnapGrid(pos.x, snapsize), def_y, SnapGrid(pos.z, snapsize)));
                    new_index++;

                    isJoin = false;
                }
                else
                {
                    new_index++;
                }


            }

            if (spline_computer)
            {
                spline_computer.Rebuild(true);
            }


            // TODO - Change rebuild update time interval.
            // Rebuild All Splines at each update.
            foreach (SplineComputer com in GameObject.FindObjectsOfType<SplineComputer>())
            {
                com.Rebuild(true);
            }

        }
        else if (Input.GetMouseButtonUp(0))
        {
            current_mode = MODE.BUILD;
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
    
    // Get Point index with position.
    int GetPointIndex(Vector3 pos)
    {
        SplinePoint[] points = spline_computer.GetPoints();

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].position == snap_pos)
            {
                return i;
            }
        }

        return -1;
    }

    // Get Point Ref with position.
    SplinePoint getSplinePoint(Vector3 pos)
    {
        SplineComputer[] spline_list = GameObject.FindObjectsOfType<SplineComputer>();

        foreach (SplineComputer spline in spline_list)
        {
            SplinePoint[] points = spline.GetPoints();

            for (int i = 0; i < points.Length; i++)
            {
                if (pos == points[i].position)
                {
                    return points[i];
                }
            }
        }

        return new SplinePoint();
    }

    // Get SplineComputer with position.
    SplineComputer getSplineComputer(Vector3 pos)
    {
        SplineComputer[] spline_list = GameObject.FindObjectsOfType<SplineComputer>();

        foreach (SplineComputer spline in spline_list)
        {
            SplinePoint[] points = spline.GetPoints();

            for (int i = 0; i < points.Length; i++)
            {
                if (pos == points[i].position)
                {
                    return spline;
                }
            }
        }

        return new SplineComputer();
    }

    // Split Spline and return newly spawned SplineComputer.
    SplineComputer SplitSpline(int index, SplineComputer spline)
    {
        SplinePoint[] originPoints = spline.GetPoints();

        List<SplinePoint> old_points = new List<SplinePoint>();
        List<SplinePoint> new_points = new List<SplinePoint>();

        for (int j = 0; j <= index; j++)
        {
            old_points.Add(originPoints[j]);
        }

        for (int j = index; j < originPoints.Length; j++)
        {
            new_points.Add(originPoints[j]);
        }

        spline.SetPoints(old_points.ToArray());

        SplineComputer newSpline = InsPath(new_points[0].position);
        newSpline.SetPoints(new_points.ToArray());

        spline.Rebuild(true);
        newSpline.Rebuild(true);

        return newSpline;
    }

    // Return true when snapping event on. Also return Direction.
    // Same feature with AppendPath()
    bool CheckSnap(out Vector3 dir)
    {
        if (last_x != SnapGrid(pos.x, snapsize) || last_z != SnapGrid(pos.z, snapsize))
        {
            dir = new Vector3(SnapGrid(pos.x, snapsize) - last_x, 0, SnapGrid(pos.z, snapsize) - last_z);

            last_x = SnapGrid(pos.x, snapsize);
            last_z = SnapGrid(pos.z, snapsize);

            return true;
        }

        dir = new Vector3();
        return false;
    }

    // Check two vector create Clockwise or Counterclockwise.
    bool isVectorGoClockwise(Vector3 from, Vector3 to)
    {
        if (Vector3.SignedAngle(from, to, new Vector3(0, 1, 0)) <= 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    float CalcPercentLine(SplineComputer spline, float percent, int snapsize)
    {
        if (spline.CalculateLength() != 0)
        {
            return (percent * snapsize) / spline.CalculateLength();
        }

        return percent;
    }

    // Clean Joined Path Line.
    void CleanLines()
    {
        Vector3 dir = spline_computer.GetPoint(1).position - spline_computer.GetPoint(0).position;
        dir.y = 0;

        Vector3 find_pos = spline_computer.GetPoint(0).position - dir;

        int old_length = cross_old_spline.GetPoints().Length;
        Vector3 from = cross_old_spline.GetPoint(old_length - 1).position - cross_old_spline.GetPoint(old_length - 2).position;
        Vector3 to = cross_new_spline.GetPoint(1).position - cross_new_spline.GetPoint(0).position;

        Vector3 cross_old_spline_dir = cross_old_spline.GetPoint(1).position - cross_old_spline.GetPoint(0).position;
        Vector3 cross_new_spline_dir = cross_new_spline.GetPoint(1).position - cross_new_spline.GetPoint(0).position;

        UnityEngine.Debug.LogWarning(dir);
        UnityEngine.Debug.LogWarning(cross_old_spline_dir);
        UnityEngine.Debug.LogWarning(cross_new_spline_dir);

        from.y = 0;
        to.y = 0;

        if (getSplineComputer(find_pos) == cross_old_spline)
        {
            // When Old Spline is parallel to Current spline
            if (isVectorGoClockwise(from, to))
            {
                UnityEngine.Debug.LogWarning("CASE1");
                cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 0.8f;
                spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.2f;
                cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.192f;
                cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.192f;
            }
            else
            {
                UnityEngine.Debug.LogWarning("CASE2");
                cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 0.8;
                spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.2;
                cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.192;
                cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.192;
            }
            
        }
        else if (getSplineComputer(find_pos) == cross_new_spline)
        {
            // When New Spline is parallel to Current spline
            if (isVectorGoClockwise(from, to))
            {
                UnityEngine.Debug.LogWarning("CASE3");
                cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.2;
                spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.2;
                cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 0.808;
                cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 0.808;
            }
            else
            {
                UnityEngine.Debug.LogWarning("CASE4");
                cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.2;
                spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.2;
                cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 0.808;
                cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 0.808;
            }    
        }
        else
        {
            // TODO - 90 degree Joinning Code
            UnityEngine.Debug.LogWarning("90 degree Joinning Code");
        }
    }

    void Start()
    {
        cm = GetComponentInChildren<Camera>();
    }

    void Update()
    {  
        RayTrace();

        snap_pos = new Vector3(SnapGrid(pos.x, snapsize), 0, SnapGrid(pos.z, snapsize));
        last_pos = snap_pos;
        debugobj.GetComponent<Transform>().position = snap_pos;

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


        if (current_mode == MODE.BUILD)
        {
            // BUILD MODE
            if (Input.GetMouseButtonDown(0))
            {
                SplineComputer[] spline_list = GameObject.FindObjectsOfType<SplineComputer>();

                // Check Mouse Position Value
                bool _isJoin = false;

                if (spline_list.Length != 0)
                {
                    bool isFound = false;

                    foreach (SplineComputer spline in spline_list)
                    {           
                        SplinePoint[] points = spline.GetPoints();

                        for (int i = 0; i < points.Length; i++)
                        {
                            if (snap_pos == points[i].position)
                            {
                                // Change to Append Mode.
                                if (snap_pos == points.Last().position)
                                {
                                    _isJoin = true;

                                    new_index = spline.GetPoints().Length;
                                    spline_computer = spline;

                                    current_mode = MODE.APPEND;

                                    break;
                                }
                                else if (snap_pos == points.First().position)
                                {
                                    _isJoin = true;

                                    new_index = 0;
                                    spline_computer = spline;

                                    current_mode = MODE.APPEND;

                                    break;
                                }

                                // Split and Join.
                                else
                                {
                                    UnityEngine.Debug.LogWarning("Split");

                                    _isJoin = true;
                                    isJoin = true;

                                    SplineComputer temp_spline = SplitSpline(i, spline);
                                    SplitSpline(1, temp_spline);

                                    cross_new_spline = temp_spline;
                                    cross_old_spline = SplitSpline(i - 1, spline);

                                    runBuildMode();
                                    isFound = true;

                                    break;
                                }
                            }
                        }

                        if (isFound) { break; }
                    }
                }

                if (!_isJoin)
                {
                    runBuildMode();
                }
            }
        }
        else if (current_mode == MODE.APPEND)
        {
            // APPEND MODE
            runAppendModeGrid();
        }
        else if (current_mode == MODE.REMOVE)
        {
            // REMOVE MODE
            if (Input.GetMouseButton(0))
            {
                int index = GetPointIndex(snap_pos);
                RemovePoint(index - 1);
            }

            // TODO - Remove points in the middle of spline
            // Now We can use checking mouse position value function.
        }
    }
}
