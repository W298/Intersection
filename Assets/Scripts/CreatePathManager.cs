using Dreamteck;
using Dreamteck.Splines;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Crossroad
{
    private Vector3 position;
    private List<SplineComputer> roads = new List<SplineComputer>();

    public List<SplineComputer> getRoads()
    {
        return roads;
    }

    public Vector3 getPosition()
    {
        return position;
    }

    public void addRoad(SplineComputer road)
    {
        roads.Add(road);
    }

    public void setPosition(Vector3 pos)
    {
        position = pos;
    }

    public void Update()
    {

    }
}

public class CreatePathManager : MonoBehaviour
{
    public enum MODE { BUILD, APPEND, REMOVE, NONE };
    public enum JOINMODE { TO3, TO4, HEAD, NONE };

    private Camera cm;
    public SplineComputer spline_computer;

    public SplineComputer SplinePrefab;
    public GameObject debugobj;
    public GameObject debugobj2;
    public int snapsize = 10;
    public MODE current_mode = MODE.NONE;

    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;
    public float last_x;
    public float last_z;
    public Vector3 last_pos;
    public int new_index = 0;
    private Vector3 pos;
    public Vector3 snap_pos;
    private bool isJoin = false;
    private JOINMODE joinmode = JOINMODE.NONE;
    private bool needSplit = false;
    public SplineComputer selected_spline;
    public int selected_index = 0;
    public Crossroad selected_crossroad;

    public float clean_value = 0.139f;

    public List<Crossroad> crossroads = new List<Crossroad>();

    public SplineComputer cross_old_spline;
    public SplineComputer cross_new_spline;

    public float divider = 7.2f;

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

    bool CheckAppendVaild(Vector3 lastPoint, Vector3 currentPoint, Vector3 addPoint, bool onlyStraight = false)
    {
        Vector3 dir = currentPoint - lastPoint;
        Vector3 dirAppend = addPoint - currentPoint;

        if (onlyStraight)
        {
            if (isVectorParallel(dir, dirAppend))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (Vector3.Angle(dir, dirAppend) <= 90)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    bool isVectorInXZArea(Vector3 pos, float x_from, float x_to, float z_from, float z_to)
    {
        bool cond_1 = x_from <= pos.x && pos.x <= x_to;
        bool cond_2 = z_from <= pos.z && pos.z <= z_to;

        return cond_1 && cond_2;
    }

    // Spawn SplineComputer and Apply to spline_computer variable.
    void SpawnPath()
    {
        UnityEngine.Debug.LogWarning("Spawn Path!");
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

            int last_index = spline_computer.GetPoints().Length - 1;

            bool cond = CheckAppendVaild(
                       spline_computer.GetPoint(last_index - 1).position,
                       spline_computer.GetPoint(last_index).position,
                       new Vector3(x, 0, z));

            if (cond || spline_computer.GetPoints().Length == 1)
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

    // Return true when snapping event on.
    // Same feature with AppendPath()
    public bool CheckSnap()
    {
        if (last_x != SnapToGridPoint(pos, snapsize).x || last_z != SnapToGridPoint(pos, snapsize).z)
        {
            last_x = SnapToGridPoint(pos, snapsize).x;
            last_z = SnapToGridPoint(pos, snapsize).z;

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
        UnityEngine.Debug.LogWarning("RunBuild!");
        
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

    // Append Point when snapping event on. Also Handle Cleaning Joined Path.
    void runAppendModeGrid()
    {
        if (Input.GetMouseButton(0))
        {
            // Head Join.
            if (joinmode == JOINMODE.HEAD)
            {
                if (CheckSnap())
                {
                    SplinePoint[] points = selected_spline.GetPoints();

                    for (int i = 0; i < points.Length; i++)
                    {
                        selected_spline.SetPoint(i + 1, points[i]);
                    }

                    selected_spline.SetPointNormal(0, def_normal);
                    selected_spline.SetPointSize(new_index, 1);
                    selected_spline.SetPointPosition(0, snap_pos);

                    // Check Joining is needed during APPEND.
                    SplineComputer check_spline = null;
                    foreach (SplineComputer spline in getSplineComputers(snap_pos))
                    {
                        if (spline != selected_spline)
                        {
                            check_spline = spline;
                        }
                    }

                    if (check_spline != null && check_spline != selected_spline)
                    {
                        if ((check_spline.GetPoints().First().position == snap_pos ||
                            check_spline.GetPoints().Last().position == snap_pos) &&
                            !check_spline.Fixed)
                        {
                            UnityEngine.Debug.LogWarning("Join 2-crossroad!");

                            MergeSplines(check_spline, selected_spline);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Join 3-crossroad!");

                            SplinePoint[] po = check_spline.GetPoints();
                            int index = 0;

                            for (int i = 0; i < po.Length; i++)
                            {
                                if (po[i].position == snap_pos)
                                {
                                    index = i;
                                }
                            }

                            SplineComputer new_spline = SplitSpline(index, check_spline);
                        }
                    }
                }
            }
            else
            {
                if (AppendPath())
                {
                    // Check Joining is needed during APPEND.
                    SplineComputer check_spline = null;
                    foreach (SplineComputer spline in getSplineComputers(snap_pos))
                    {
                        if (spline != spline_computer)
                        {
                            check_spline = spline;
                        }
                    }

                    if (check_spline != null && check_spline != spline_computer)
                    {
                        if ((check_spline.GetPoints().First().position == snap_pos ||
                            check_spline.GetPoints().Last().position == snap_pos) &&
                            !check_spline.Fixed)
                        {
                            UnityEngine.Debug.LogWarning("Join 2-crossroad!");

                            MergeSplines(check_spline, spline_computer);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Join 3-crossroad!");

                            SplinePoint[] points = check_spline.GetPoints();
                            int index = 0;

                            for (int i = 0; i < points.Length; i++)
                            {
                                if (points[i].position == snap_pos)
                                {
                                    index = i;
                                }
                            }

                            SplineComputer new_spline = SplitSpline(index, check_spline);
                        }
                    }

                    if (isJoin)
                    {
                        if (joinmode == JOINMODE.TO3)
                        {
                            // Spliting Start
                            SplineComputer temp_spline = SplitSpline(selected_index, selected_spline);

                            if (temp_spline.GetPoints().Length >= 3)
                            {
                                SplitSpline(1, temp_spline);
                            }

                            cross_new_spline = temp_spline;

                            if (selected_spline.GetPoints().Length >= 3)
                            {
                                cross_old_spline = SplitSpline(selected_index - 1, selected_spline);
                            }
                            else
                            {
                                cross_old_spline = selected_spline;
                            }
                            // Spliting End

                            // CleanLines();

                            new_index++;

                            cross_new_spline.Fixed = true;
                            cross_old_spline.Fixed = true;
                            spline_computer.Fixed = true;

                            Crossroad crossroad = new Crossroad();
                            crossroad.addRoad(cross_new_spline);
                            crossroad.addRoad(cross_old_spline);
                            crossroad.addRoad(spline_computer);
                            crossroad.setPosition(cross_new_spline.GetPoint(0).position);

                            crossroads.Add(crossroad);

                            isJoin = false;
                            joinmode = JOINMODE.NONE;
                            needSplit = true;
                        }
                        else if (joinmode == JOINMODE.TO4)
                        {
                            spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                            spline_computer.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                            spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                            spline_computer.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                            spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;

                            List<SplineComputer> splines = selected_crossroad.getRoads();

                            foreach (SplineComputer spline in splines)
                            {
                                Vector3 dir = spline_computer.GetPoint(1).position - spline_computer.GetPoint(0).position;
                                Vector3 dir2 = spline.GetPoint(1).position - spline.GetPoint(0).position;

                                if (isVectorVertical(dir, dir2))
                                {
                                    if (isVectorGoClockwise(dir, dir2))
                                    {
                                        // 시작부븐 왼쪽
                                    }
                                    else
                                    {
                                        // 끝부분 오른쪽
                                    }
                                }
                            }

                            new_index++;

                            spline_computer.Fixed = true;

                            isJoin = false;
                            joinmode = JOINMODE.NONE;
                            needSplit = true;
                        }
                    }
                    else if (needSplit)
                    {
                        spline_computer = SplitSpline(1, spline_computer);

                        needSplit = false;
                    }
                    else
                    {
                        new_index++;
                    }

                    // TODO - Change rebuild update time interval.
                    // Rebuild All Splines at appending update.
                    foreach (SplineComputer com in GameObject.FindObjectsOfType<SplineComputer>())
                    {
                        com.Rebuild(true);
                    }
                }
            }

            if (spline_computer)
            {
                spline_computer.Rebuild(true);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            spline_computer = null;

            selected_spline = null;
            selected_index = 0;
            selected_crossroad = null;

            new_index = 0;
            last_x = 0;
            last_z = 0;

            joinmode = JOINMODE.NONE;
            current_mode = MODE.BUILD;
            isJoin = false;
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

    List<SplineComputer> getSplineComputers(Vector3 pos)
    {
        SplineComputer[] spline_list = GameObject.FindObjectsOfType<SplineComputer>();
        List<SplineComputer> return_list = new List<SplineComputer>();

        foreach (SplineComputer spline in spline_list)
        {
            SplinePoint[] points = spline.GetPoints();

            for (int i = 0; i < points.Length; i++)
            {
                if (pos == points[i].position)
                {
                    return_list.Add(spline);
                    break;
                }
            }
        }

        return return_list;
    }

    int getSplinePointIndex(SplineComputer spline, SplinePoint point)
    {
        SplinePoint[] points = spline.GetPoints();

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].position == point.position)
            {
                return i;
            }
        }

        return -1;
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

    // Check two vector is Parallel.
    bool isVectorParallel(Vector3 v1, Vector3 v2)
    {
        if (Vector3.Angle(v1, v2) == 0 || Vector3.Angle(v1, v2) == 180)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool isVectorVertical(Vector3 v1, Vector3 v2)
    {
        if (Vector3.Dot(v1, v2) == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    SplineComputer MergeSplines(SplineComputer s1, SplineComputer s2)
    {
        if (s1.GetPoints().Last().position == s2.GetPoints().Last().position)
        {
            // Reverse Merge
            UnityEngine.Debug.LogWarning("Reverse Merge");
            int index = s1.GetPoints().Length;

            SplinePoint[] points = s2.GetPoints();
            for (int i = points.Length - 2; i >= 0; i--)
            {
                UnityEngine.Debug.LogWarning(i);
                s1.SetPoint(index, points[i]);
                index++;
            }

            Destroy(s2.gameObject);

            return s1;
        }
        else if (s1.GetPoints().First().position == s2.GetPoints().First().position)
        {
            UnityEngine.Debug.LogWarning("Reverse Merge 2");
            SplinePoint[] points = s1.GetPoints();
            SplinePoint[] points2 = s2.GetPoints();

            int index = 0;

            for (int i = 0; i < points.Length; i++)
            {
                s1.SetPoint(i + points2.Length - 1, points[i]);
            }
            for (int i = points2.Length - 1; i >= 1; i--)
            {
                s1.SetPoint(index, points2[i]);
                index++;
            }

            Destroy(s2.gameObject);

            return s1;
        }
        else if (s1.GetPoints().Last().position == s2.GetPoints().First().position)
        {
            // Straight Merge
            UnityEngine.Debug.LogWarning("Straight Merge");
            int index = s1.GetPoints().Length;

            SplinePoint[] points = s2.GetPoints();
            for (int i = 1; i < points.Length; i++)
            {
                s1.SetPoint(index, points[i]);
                index++;
            }

            Destroy(s2.gameObject);
           
            return s1;
        }
        else if (s1.GetPoints().First().position == s2.GetPoints().Last().position)
        {
            UnityEngine.Debug.LogWarning("Straight Merge 2");
            int index = s2.GetPoints().Length;

            SplinePoint[] points = s1.GetPoints();

            for (int i = 1; i <= points.Length - 1; i++)
            {
                s2.SetPoint(index, points[i]);
                index++;
            }

            Destroy(s1.gameObject);

            return s2;
        }
        else
        {
            UnityEngine.Debug.LogWarning("NONE");
            return null;
        }
    }

    // Clean Joined Path Line.
    void CleanLines()
    {
        Vector3 dir = spline_computer.GetPoint(1).position - spline_computer.GetPoint(0).position;
        Vector3 cross_old_spline_dir = cross_old_spline.GetPoint(1).position - cross_old_spline.GetPoint(0).position;
        Vector3 cross_new_spline_dir = cross_new_spline.GetPoint(1).position - cross_new_spline.GetPoint(0).position;

        if (isVectorParallel(cross_old_spline_dir, cross_new_spline_dir))
        {
            if (isVectorVertical(cross_old_spline_dir, dir))
            {
                if (isVectorGoClockwise(cross_old_spline_dir, dir))
                {
                    UnityEngine.Debug.LogWarning("90 DEG C");
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 1 - clean_value;
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(4).clipTo = 1 - clean_value;

                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                    spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                    spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(1).clipTo = 1 - clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("90 DEG CC");
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 1 - clean_value;
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(5).clipTo = 1 - clean_value;

                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                    spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                    spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(1).clipTo = 1 - clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                }
            }
            else
            {
                // TODO - Clean Crossed Join Line
                UnityEngine.Debug.LogWarning("DIR CROSS JOIN");
            }
        }
        else
        {
            if (isVectorParallel(cross_old_spline_dir, dir))
            {
                if (isVectorGoClockwise(cross_old_spline_dir, cross_new_spline_dir))
                {
                    UnityEngine.Debug.LogWarning("CASE 1");
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 1 - clean_value;
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(4).clipTo = 1 - clean_value;

                    spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(1).clipTo = 1 - clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("CASE 2");
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 1 - clean_value;
                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(5).clipTo = 1 - clean_value;

                    spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                    cross_old_spline.GetComponent<SplineMesh>().GetChannel(1).clipTo = 1 - clean_value;
                    cross_new_spline.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                    spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                }
            }
            else
            {
                if (isVectorParallel(cross_new_spline_dir, dir))
                {
                    if (isVectorGoClockwise(cross_old_spline_dir, dir))
                    {
                        UnityEngine.Debug.LogWarning("CASE 3");
                        cross_new_spline.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                        cross_new_spline.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                        spline_computer.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                        spline_computer.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 1 - clean_value;
                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(4).clipTo = 1 - clean_value;

                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 1 - clean_value;
                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(5).clipTo = 1 - clean_value;

                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(1).clipTo = 1 - clean_value;
                        cross_new_spline.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                        spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("CASE 4");
                        cross_new_spline.GetComponent<SplineMesh>().GetChannel(2).clipFrom = clean_value;
                        cross_new_spline.GetComponent<SplineMesh>().GetChannel(4).clipFrom = clean_value;

                        spline_computer.GetComponent<SplineMesh>().GetChannel(3).clipFrom = clean_value;
                        spline_computer.GetComponent<SplineMesh>().GetChannel(5).clipFrom = clean_value;

                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(2).clipTo = 1 - clean_value;
                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(4).clipTo = 1 - clean_value;

                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(3).clipTo = 1 - clean_value;
                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(5).clipTo = 1 - clean_value;

                        cross_old_spline.GetComponent<SplineMesh>().GetChannel(1).clipTo = 1 - clean_value;
                        cross_new_spline.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                        spline_computer.GetComponent<SplineMesh>().GetChannel(1).clipFrom = clean_value;
                    }
                }
                else
                {
                    // TODO - Clean Crossed Join Line 2
                    UnityEngine.Debug.LogWarning("3 CROSS JOIN");
                }
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
        last_pos = snap_pos;
        debugobj.GetComponent<Transform>().position = snap_pos;

        foreach (Crossroad cros in crossroads)
        {
            List<Vector3> dirs = new List<Vector3>();

            foreach (SplineComputer spl in cros.getRoads())
            {
                if (spl.GetPoints().Last().position == cros.getPosition())
                {
                    int last_index = spl.GetPoints().Length - 1;

                    Vector3 dir = spl.GetPoint(last_index - 1).position - cros.getPosition();
                    dirs.Add(dir);

                    Vector3 proj_loc = cros.getPosition() + dir / divider;

                    Instantiate(debugobj2, proj_loc, Quaternion.identity);
                }
                else if (spl.GetPoints().First().position == cros.getPosition())
                {
                    Vector3 dir = spl.GetPoint(1).position - cros.getPosition();
                    dirs.Add(dir);

                    Vector3 proj_loc = cros.getPosition() + dir / divider;

                    Instantiate(debugobj2, proj_loc, Quaternion.identity);
                }
            }

            List<SplineComputer> roads = cros.getRoads();

            for (int i = 0; i < roads.Count; i++)
            {
                bool isRight = false;
                bool isLeft = false;

                if (roads[i].GetPoints().Last().position == cros.getPosition())
                {
                    foreach (Vector3 dir in dirs)
                    {
                        if (isVectorVertical(dirs[i], dir))
                        {
                            if (isVectorGoClockwise(dirs[i], dir))
                            {
                                isLeft = true;
                            }
                            else
                            {
                                isRight = true;
                            }
                        }
                    }

                    double per = roads[i].Project(cros.getPosition() + dirs[i] / divider).percent;

                    roads[i].GetComponent<SplineMesh>().GetChannel(1).clipTo = per;

                    if (isLeft && !isRight)
                    {
                        roads[i].GetComponent<SplineMesh>().GetChannel(3).clipTo = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(5).clipTo = per;
                    }
                    else if (isRight && !isLeft)
                    {
                        roads[i].GetComponent<SplineMesh>().GetChannel(2).clipTo = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(4).clipTo = per;
                    }
                    else if (isLeft && isRight)
                    {
                        roads[i].GetComponent<SplineMesh>().GetChannel(3).clipTo = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(5).clipTo = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(2).clipTo = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(4).clipTo = per;
                    }
                }
                else if (roads[i].GetPoints().First().position == cros.getPosition())
                {
                    foreach (Vector3 dir in dirs)
                    {
                        if (isVectorVertical(dirs[i], dir))
                        {
                            if (isVectorGoClockwise(dirs[i], dir))
                            {
                                isRight = true;
                            }
                            else
                            {
                                isLeft = true;
                            }
                        }
                    }

                    double per = roads[i].Project(cros.getPosition() + dirs[i] / divider).percent;

                    roads[i].GetComponent<SplineMesh>().GetChannel(1).clipFrom = per;

                    if (isLeft && !isRight)
                    {
                        roads[i].GetComponent<SplineMesh>().GetChannel(3).clipFrom = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(5).clipFrom = per;
                    }
                    else if (isRight && !isLeft)
                    {
                        roads[i].GetComponent<SplineMesh>().GetChannel(2).clipFrom = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(4).clipFrom = per;
                    }
                    else if (isLeft && isRight)
                    {
                        roads[i].GetComponent<SplineMesh>().GetChannel(3).clipFrom = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(5).clipFrom = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(2).clipFrom = per;
                        roads[i].GetComponent<SplineMesh>().GetChannel(4).clipFrom = per;
                    }
                }
            }
        }

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
                        if (/*!spline.Fixed*/ true)
                        {
                            SplinePoint[] points = spline.GetPoints();

                            for (int i = 0; i < points.Length; i++)
                            {
                                if (snap_pos == points[i].position)
                                {
                                    // Append To Tail.
                                    if (snap_pos == points.Last().position)
                                    {
                                        UnityEngine.Debug.LogWarning("Tail Append");
                                        _isJoin = true;
                                        isFound = true;

                                        new_index = spline.GetPoints().Length - 1;
                                        spline_computer = spline;

                                        current_mode = MODE.APPEND;

                                        break;
                                    }
                                    // Append To Head.
                                    else if (snap_pos == points.First().position)
                                    {
                                        UnityEngine.Debug.LogWarning("Head Append");

                                        _isJoin = true;
                                        isFound = true;

                                        selected_spline = spline;
                                        current_mode = MODE.APPEND;

                                        isJoin = true;
                                        joinmode = JOINMODE.HEAD;

                                        last_x = snap_pos.x;
                                        last_z = snap_pos.z;

                                        break;
                                    }

                                    // Split and Join.
                                    else
                                    {
                                        UnityEngine.Debug.LogWarning("Split");

                                        _isJoin = true;
                                        isJoin = true;
                                        joinmode = JOINMODE.TO3;

                                        selected_spline = spline;
                                        selected_index = i;

                                        runBuildMode();
                                        isFound = true;

                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("FIX");
                            selected_spline = spline;

                            foreach (Crossroad crossroad in crossroads)
                            {
                                if (snap_pos == crossroad.getPosition())
                                {
                                    UnityEngine.Debug.LogWarning("Find");
                                    _isJoin = true;
                                    isJoin = true;
                                    joinmode = JOINMODE.TO4;

                                    selected_crossroad = crossroad;

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
