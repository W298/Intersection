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

    public void logInfo()
    {
        string values = "";

        foreach (var road in roads)
        {
            values += road.position + " ";
        }
        UnityEngine.Debug.LogWarning("Crossroad Info : " + values);
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
    public SplineComputer SplinePrefab;
    public GameObject debugobj;
    public GameObject debugobj2;

    public int snapsize = 10;
    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;
    public float divider = 7.2f;

    public SplineComputer current_spline;
    public MODE current_mode = MODE.NONE;
    private JOINMODE joinmode = JOINMODE.NONE;
    public int new_index = 0;

    public float last_x;
    public float last_z;
    public Vector3 last_pos;
    private Vector3 pos;
    public Vector3 snap_pos;

    public SplineComputer selected_spline;
    public int selected_index = 0;
    public Crossroad selected_crossroad;
    public SplineComputer cross_old_spline;
    public SplineComputer cross_new_spline;

    public List<Crossroad> crossroads = new List<Crossroad>();

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
        if (current_spline)
        {
            current_spline = null;
            new_index = 0;
        }

        current_spline = Instantiate(SplinePrefab, pos, Quaternion.identity);
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

            int last_index = current_spline.GetPoints().Length - 1;

            bool cond = CheckAppendVaild(
                       current_spline.GetPoint(last_index - 1).position,
                       current_spline.GetPoint(last_index).position,
                       new Vector3(x, 0, z));

            if (cond || current_spline.GetPoints().Length == 1)
            {
                current_spline.SetPointNormal(new_index, def_normal);
                current_spline.SetPointSize(new_index, 1);
                current_spline.SetPointPosition(new_index, new Vector3(x, def_y, z));

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
    // Body of this function referred DeletePointModule.cs
    void RemovePoint(int index)
    {
        SplinePoint[] p = current_spline.spline.points;

        if (index < p.Length && index >= 0)
        {
            ArrayUtility.RemoveAt(ref p, index);
            current_spline.spline.points = p;
        }
        else
        {
            UnityEngine.Debug.LogError("Out of Index! (RemovePoint)");
        }

        if (current_spline)
        {
            current_spline.Rebuild(true);
        }
    }

    // Remove point with point ref.
    void RemovePoint(SplinePoint point)
    {
        SplinePoint[] p = current_spline.spline.points;
        
        ArrayUtility.RemoveAt(ref p, ArrayUtility.IndexOf(p, point));
        current_spline.spline.points = p;

        if (current_spline)
        {
            current_spline.Rebuild(true);
        }
    }

    // Simply Ray-trace and Set mouse position.
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

        if (current_spline)
        {
            current_spline.Rebuild(true);
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
                            UnityEngine.Debug.LogWarning("Join 2-crossroad (HEAD)");

                            MergeSplines(check_spline, selected_spline);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Join 3-crossroad (HEAD)");

                            int index = getSplinePointIndex(check_spline, getSplinePoint(snap_pos, check_spline));

                            SplineComputer new_spline = SplitSpline(index, check_spline);

                            Crossroad crossroad = new Crossroad();
                            crossroad.addRoad(check_spline);
                            crossroad.addRoad(new_spline);
                            crossroad.addRoad(selected_spline);
                            crossroad.setPosition(new_spline.GetPoint(0).position);

                            crossroads.Add(crossroad);
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
                        if (spline != current_spline)
                        {
                            check_spline = spline;
                        }
                    }

                    if (check_spline != null && check_spline != current_spline)
                    {
                        if ((check_spline.GetPoints().First().position == snap_pos ||
                            check_spline.GetPoints().Last().position == snap_pos) &&
                            !check_spline.Fixed)
                        {
                            UnityEngine.Debug.LogWarning("Join 2-roads (APPEND)");

                            MergeSplines(check_spline, current_spline);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Join 3-crossroad (APPEND)");

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

                            Crossroad crossroad = new Crossroad();
                            crossroad.addRoad(new_spline);
                            crossroad.addRoad(check_spline);
                            crossroad.addRoad(current_spline);
                            crossroad.setPosition(new_spline.GetPoint(0).position);

                            crossroads.Add(crossroad);
                        }
                    }

                    if (joinmode != JOINMODE.NONE)
                    {
                        if (joinmode == JOINMODE.TO3)
                        {
                            UnityEngine.Debug.LogWarning("Join 3-crossroad (BUILD)");

                            // Check If selected spline referenced by another crossroad
                            Crossroad refCrossroad = null;

                            foreach (var cros in crossroads)
                            {
                                if (cros.getRoads().Contains(selected_spline))
                                {
                                    refCrossroad = cros;
                                    break;
                                }
                            }

                            if (refCrossroad != null)
                            {
                                if (refCrossroad.getPosition() == selected_spline.GetPoints().Last().position)
                                {
                                    UnityEngine.Debug.LogWarning("Last");

                                    cross_new_spline = SplitSpline(selected_index, selected_spline, true);
                                    cross_old_spline = selected_spline;
                                }
                                else if (refCrossroad.getPosition() == selected_spline.GetPoints().First().position)
                                {
                                    UnityEngine.Debug.LogWarning("First");

                                    cross_new_spline = SplitSpline(selected_index, selected_spline);
                                    cross_old_spline = selected_spline;
                                }
                            }
                            else
                            {
                                cross_new_spline = SplitSpline(selected_index, selected_spline);
                                cross_old_spline = selected_spline;
                            }

                            Crossroad crossroad = new Crossroad();
                            crossroad.addRoad(cross_new_spline);
                            crossroad.addRoad(cross_old_spline);
                            crossroad.addRoad(current_spline);
                            crossroad.setPosition(cross_new_spline.GetPoint(0).position);

                            crossroads.Add(crossroad);

                            joinmode = JOINMODE.NONE;
                            selected_spline = null;
                            new_index++;
                        }
                        else if (joinmode == JOINMODE.TO4)
                        {
                            UnityEngine.Debug.LogWarning("Join 4-crossroad (BUILD)");

                            selected_crossroad.addRoad(current_spline);

                            new_index++;
                            joinmode = JOINMODE.NONE;
                        }
                    }
                    else
                    {
                        new_index++;
                    }
                }
            }

            if (current_spline)
            {
                current_spline.Rebuild(true);
            }

            if (cross_new_spline)
            {
                cross_new_spline.Rebuild(true);
            }

            if (cross_old_spline)
            {
                cross_old_spline.Rebuild(true);
            }

            if (selected_spline)
            {
                selected_spline.Rebuild(true);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            current_spline = null;
            cross_old_spline = null;
            cross_new_spline = null;

            selected_spline = null;
            selected_index = 0;
            selected_crossroad = null;

            new_index = 0;
            last_x = 0;
            last_z = 0;

            joinmode = JOINMODE.NONE;
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
        }
    }
    
    // Get Point count with position.
    int GetPointIndex(Vector3 pos)
    {
        SplinePoint[] points = current_spline.GetPoints();

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

    SplinePoint getSplinePoint(Vector3 pos, SplineComputer spline)
    {
        foreach (SplinePoint point in spline.GetPoints())
        {
            if (point.position == pos)
            {
                return point;
            }
        }

        return new SplinePoint();
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
    SplineComputer SplitSpline(int index, SplineComputer spline, bool reverse = false)
    {
        var oldPoints = new List<SplinePoint>();
        var newPoints = new List<SplinePoint>();

        var originPoints = spline.GetPoints();

        if (!reverse)
        {
            for (int j = 0; j <= index; j++)
            {
                oldPoints.Add(originPoints[j]);
            }

            for (int j = index; j < originPoints.Length; j++)
            {
                newPoints.Add(originPoints[j]);
            }
        }
        else
        {
            for (int i = 0; i <= index; i++)
            {
                newPoints.Add(originPoints[i]);
            }

            for (int i = index; i < originPoints.Length; i++)
            {
                oldPoints.Add(originPoints[i]);
            }
        }

        spline.SetPoints(oldPoints.ToArray());

        var newSpline = InsPath(newPoints[0].position);
        newSpline.SetPoints(newPoints.ToArray());

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

    void Start()
    {
        cm = GetComponentInChildren<Camera>();
    }

    public bool enable = false;

    void Update()
    {  
        RayTrace();

        snap_pos = new Vector3(SnapGrid(pos.x, snapsize), 0, SnapGrid(pos.z, snapsize));
        last_pos = snap_pos;
        debugobj.GetComponent<Transform>().position = snap_pos;

        foreach (var cros in crossroads)
        {
            cros.logInfo();

            List<SplineComputer> roads = cros.getRoads();

            foreach (var road in roads)
            {
                if (road.GetPoints().Last().position == cros.getPosition())
                {
                    if (enable) 
                        UnityEngine.Debug.LogWarning("1- " + road.position);
                }
                    
                else if (road.GetPoints().First().position == cros.getPosition())
                {
                    if (enable)
                        UnityEngine.Debug.LogWarning("2- " + road.position);
                }
                else
                {
                    if (enable)
                        UnityEngine.Debug.LogWarning("E- " + road.position);
                }
            }
        }

        foreach (Crossroad cros in crossroads)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<SplineComputer> roads = cros.getRoads();

            for (int i = 0; i < roads.Count; i++)
            {
                if (roads[i].GetPoints().Last().position == cros.getPosition())
                {
                    int last_index = roads[i].GetPoints().Length - 1;

                    Vector3 dir = roads[i].GetPoint(last_index - 1).position - cros.getPosition();
                    dirs.Add(dir);
                }
                else if (roads[i].GetPoints().First().position == cros.getPosition())
                {
                    Vector3 dir = roads[i].GetPoint(1).position - cros.getPosition();
                    dirs.Add(dir);
                }
                else
                {

                }
            }

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

                Crossroad crossroad = null;

                foreach (Crossroad cros in crossroads)
                {
                    if (snap_pos == cros.getPosition())
                    {
                        crossroad = cros;
                        break;
                    }
                }

                if (crossroad != null)
                {
                    if (crossroad.getRoads().Count == 3)
                    {
                        joinmode = JOINMODE.TO4;

                        selected_crossroad = crossroad;

                        runBuildMode();
                    }
                }
                else
                {
                    List<SplineComputer> splines = getSplineComputers(snap_pos);
                    SplineComputer spline = null;

                    if (splines.Count == 1)
                    {
                        spline = splines[0];

                        SplinePoint point = getSplinePoint(snap_pos, spline);
                        int point_index = getSplinePointIndex(spline, point);

                        if (point_index != -1)
                        {
                            if (point_index == spline.GetPoints().Count() - 1)
                            {
                                UnityEngine.Debug.LogWarning("Tail Append");

                                new_index = point_index;
                                current_spline = spline;

                                current_mode = MODE.APPEND;
                            }
                            else if (point_index == 0)
                            {
                                UnityEngine.Debug.LogWarning("Head Append");

                                selected_spline = spline;

                                current_mode = MODE.APPEND;
                                joinmode = JOINMODE.HEAD;

                                last_x = snap_pos.x;
                                last_z = snap_pos.z;
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("Split for Join");

                                selected_spline = spline;
                                selected_index = point_index;

                                joinmode = JOINMODE.TO3;

                                runBuildMode();
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("ERROR - Can't find Point in Spline");
                        }
                    }
                    else if (splines.Count == 0)
                    {
                        runBuildMode();
                    }
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
