using Dreamteck;
using Dreamteck.Splines;
using System;
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
    public GameObject debugobj2;
    public int snapsize = 10;
    public MODE current_mode = MODE.NONE;

    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;
    private float last_x;
    private float last_z;
    private int new_index = 0;
    private Vector3 pos;
    private Vector3 snap_pos;
    private bool isJoin = false;

    private SplineComputer old_spline;
    private SplineComputer new_spline;

    float SnapGrid(float value, int snapsize)
    {
        // TODO - Make Snapping to cross possible.

        if (value < 0)
        {
            return Mathf.Round(Mathf.Abs(value / snapsize)) * snapsize * -1;
        }
        else
        {
            return Mathf.Round(value / snapsize) * snapsize;
        }
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
        if (last_x != SnapGrid(pos.x, snapsize) || last_z != SnapGrid(pos.z, snapsize))
        {
            spline_computer.SetPointNormal(new_index, def_normal);
            spline_computer.SetPointSize(new_index, 1);
            spline_computer.SetPointPosition(new_index, new Vector3(SnapGrid(pos.x, snapsize), def_y, SnapGrid(pos.z, snapsize)));

            last_x = SnapGrid(pos.x, snapsize);
            last_z = SnapGrid(pos.z, snapsize);

            return true;
        }
        
        return false;
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
                    isJoin = false;
                }

                new_index++;
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

    // Clean Joined Path Line.
    void CleanLines()
    {
        // PROBLEM : Clip from-to Method uses percent, it stretched out.
        // TODO - Recalculate percent or use another method for setting range of line.

        Vector3 dir = spline_computer.GetPoint(1).position - spline_computer.GetPoint(0).position;
        dir.y = 0;

        Vector3 find_pos = spline_computer.GetPoint(0).position - dir;

        int old_length = old_spline.GetPoints().Length;
        Vector3 from = old_spline.GetPoint(old_length - 1).position - old_spline.GetPoint(old_length - 2).position;
        Vector3 to = new_spline.GetPoint(1).position - new_spline.GetPoint(0).position;

        from.y = 0;
        to.y = 0;

        if (getSplineComputer(find_pos) == old_spline)
        {
            // When Old Spline is parallel to Current spline
            if (isVectorGoClockwise(from, to))
            {
                old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 0.8;
                spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.2;
                new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.192;
                new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.192;
            }
            else
            {
                old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 0.8;
                spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.2;
                new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.192;
                new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.192;
            }
            
        }
        else if (getSplineComputer(find_pos) == new_spline)
        {
            // When New Spline is parallel to Current spline
            if (isVectorGoClockwise(from, to))
            {
                new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.2;
                spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.2;
                old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 0.808;
                old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 0.808;
            }
            else
            {
                new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = 0.2;
                spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = 0.2;
                old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 0.808;
                old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 0.808;
            }    
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
                                if (snap_pos == points.Last().position || snap_pos == points.First().position)
                                {
                                    _isJoin = true;

                                    new_index = spline.GetPoints().Length;
                                    spline_computer = spline;

                                    break;
                                }

                                // Split and Join.
                                else
                                {
                                    _isJoin = true;
                                    isJoin = true;

                                    old_spline = spline;
                                    new_spline = SplitSpline(i, spline);

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
