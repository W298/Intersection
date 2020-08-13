using System.Collections;
using Dreamteck.Splines;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ArrayUtility = Dreamteck.ArrayUtility;

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

    public void AddRoad(SplineComputer road)
    {
        if (!roads.Contains(road))
            roads.Add(road);
    }

    public void RemoveRoad(SplineComputer road)
    {
        roads.Remove(road);
    }

    public void SetRoads(SplineComputer[] list)
    {
        if (list != null)
        {
            roads.Clear();
            roads = list.ToList();
        }
    }

    public void SetRoads(List<SplineComputer> list)
    {
        if (list != null)
        {
            roads.Clear();
            roads = list;
        }
    }

    public void SetPosition(Vector3 pos)
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
    public enum JOINMODE { TO3, TO4, HEAD, TO3_SPLIT, NONE };
    public enum ROADLANE { RL1, RL2, RL3, RL4 };

    public SplineComputer[] roadPrefabs;

    private Camera cm;
    public SplineComputer SplinePrefab;
    public GameObject debugobj;
    public GameObject debugObj_2;

    public GameObject debugObj_3;
    public GameObject debugObj_4;
    public GameObject textObj;

    public int snapsize = 10;
    private Vector3 def_normal = new Vector3(0, 1, 0);
    private float def_y = 0.0f;
    public float divider = 7.2f;

    public SplineComputer current_spline;
    public MODE current_mode = MODE.NONE;
    private JOINMODE joinmode = JOINMODE.NONE;
    public int new_index = 0;
    public ROADLANE currentRoadLane = ROADLANE.RL1;

    public float last_x;
    public float last_z;
    public Vector3 last_pos;
    private Vector3 pos;
    public Vector3 snap_pos;

    public SplineComputer selected_spline;
    public List<SplineComputer> selectedSplines;
    public int selected_index = 0;
    public Crossroad selected_crossroad;
    public SplineComputer cross_old_spline;
    public SplineComputer cross_new_spline;

    public List<Crossroad> crossroads = new List<Crossroad>();

    void debugPoint(Vector3 pos)
    {
        Instantiate(debugObj_2, pos, Quaternion.identity);
    }

    void debugVector(Vector3 start, Vector3 end)
    {
        Instantiate(debugObj_3, start, Quaternion.identity);
        Instantiate(debugObj_4, end, Quaternion.identity);
    }

    public void LogTextOnPos(string text, Vector3 pos)
    {
        var obj = Instantiate(textObj, pos, Quaternion.Euler(90, 0, 0));
        obj.GetComponent<TextMesh>().text = text;
        obj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        StartCoroutine(Stop());

        IEnumerator Stop()
        {
            yield return 0;
            Destroy(obj);
        }
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

    bool CheckAppendVaild(Vector3 lastPoint, Vector3 currentPoint, Vector3 addPoint)
    {
        Vector3 dir = currentPoint - lastPoint;
        Vector3 dirAppend = addPoint - currentPoint;

        switch (currentRoadLane)
        {
            case ROADLANE.RL1:
                return (Vector3.Angle(dir, dirAppend) <= 90);
            case ROADLANE.RL2:
                return (Vector3.Angle(dir, dirAppend) <= 45);
            default:
                return true;
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

        switch (currentRoadLane)
        {
            case ROADLANE.RL1:
                SplinePrefab = roadPrefabs[0];
                break;
            case ROADLANE.RL2:
                SplinePrefab = roadPrefabs[1];
                break;
            case ROADLANE.RL3:
                SplinePrefab = roadPrefabs[2];
                break;
            case ROADLANE.RL4:
                SplinePrefab = roadPrefabs[3];
                break;
        }

        current_spline = Instantiate(SplinePrefab, pos, Quaternion.identity);

        meshReform(current_spline);
    }

    // Spawn SplineComputer independently.
    SplineComputer InsPath(Vector3 pos)
    {
        switch (currentRoadLane)
        {
            case ROADLANE.RL1:
                SplinePrefab = roadPrefabs[0];
                break;
            case ROADLANE.RL2:
                SplinePrefab = roadPrefabs[1];
                break;
            case ROADLANE.RL3:
                SplinePrefab = roadPrefabs[2];
                break;
            case ROADLANE.RL4:
                SplinePrefab = roadPrefabs[3];
                break;
        }

        var spline = Instantiate(SplinePrefab, pos, Quaternion.identity);
        
        meshReform(spline);
        
        return spline;
    }

    void meshReform(SplineComputer spline)
    {
        switch (currentRoadLane)
        {
            case ROADLANE.RL1:
                for (int i = 0; i < 6; i++)
                {
                    spline.GetComponent<SplineMesh>().meshReduce(i, 1);
                }
                break;
            case ROADLANE.RL2:
                spline.GetComponent<SplineMesh>().meshReduce(6, 4);
                spline.GetComponent<SplineMesh>().meshReduce(7, 4);
                break;
        }
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
                    // -------------------------------------------------------------------- HEAD APPEND CODE
                    bool cond = CheckAppendVaild(
                        selected_spline.GetPoint(1).position,
                        selected_spline.GetPoint(0).position,
                        snap_pos);

                    if (cond || current_spline.GetPoints().Length == 1)
                    {
                        SplinePoint[] points = selected_spline.GetPoints();

                        for (int i = 0; i < points.Length; i++)
                        {
                            selected_spline.SetPoint(i + 1, points[i]);
                        }

                        selected_spline.SetPointNormal(0, def_normal);
                        selected_spline.SetPointSize(new_index, 1);
                        selected_spline.SetPointPosition(0, snap_pos);

                        // ----------------------------------------------------- CHECK JOIN DURING APPEND (HEAD)
                        SplineComputer check_spline = null;
                        foreach (SplineComputer spline in GetSplineComputers(snap_pos))
                        {
                            if (spline != selected_spline)
                            {
                                check_spline = spline;
                            }
                        }

                        if (check_spline != null && check_spline != selected_spline)
                        {
                            if ((check_spline.GetPoints().First().position == snap_pos ||
                                check_spline.GetPoints().Last().position == snap_pos))
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
                                crossroad.AddRoad(check_spline);
                                crossroad.AddRoad(new_spline);
                                crossroad.AddRoad(selected_spline);
                                crossroad.SetPosition(new_spline.GetPoint(0).position);

                                crossroads.Add(crossroad);
                            }
                        }
                        
                        // Check Appending Spline is Closed.
                        if (selected_spline.GetPoints().First().position == selected_spline.GetPoints().Last().position)
                        {
                            UnityEngine.Debug.LogWarning("LOOP");
                            selected_spline.Close();
                        }
                    }
                }
            }
            else
            {
                if (AppendPath())
                {
                    // ----------------------------------------------------- CHECK JOIN DURING APPEND (TAIL)
                    SplineComputer check_spline = null;

                    foreach (SplineComputer spline in GetSplineComputers(snap_pos))
                    {
                        if (spline != current_spline)
                        {
                            check_spline = spline;
                        }
                    }

                    if (check_spline != null && check_spline != current_spline)
                    {
                        if ((check_spline.GetPoints().First().position == snap_pos ||
                            check_spline.GetPoints().Last().position == snap_pos))
                        {
                            UnityEngine.Debug.LogWarning("Join 2-roads (APPEND)");

                            var haveSameCrossroad = GetRefCrossroads(current_spline).Any(cros => GetRefCrossroads(check_spline).Contains(cros));

                            if (haveSameCrossroad)
                            {
                                // CROSSROAD LOOP
                                var spline = MergeSplines(check_spline, current_spline);
                            }
                            else
                            {
                                var spline = MergeSplines(check_spline, current_spline);
                            }
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
                            crossroad.AddRoad(new_spline);
                            crossroad.AddRoad(check_spline);
                            crossroad.AddRoad(current_spline);
                            crossroad.SetPosition(new_spline.GetPoint(0).position);

                            crossroads.Add(crossroad);
                        }
                    }

                    if (current_spline.GetPoints().First().position == current_spline.GetPoints().Last().position)
                    {
                        UnityEngine.Debug.LogWarning("LOOP");
                        current_spline.Close();
                    }

                    // ------------------------------------------------------------- CHECK JOIN DURING BUILD
                    if (joinmode != JOINMODE.NONE)
                    {
                        if (joinmode == JOINMODE.TO3)
                        {
                            UnityEngine.Debug.LogWarning("Join 3-crossroad (BUILD)");

                            // Check If selected spline referenced by another crossroad
                            var refCrossroads = GetRefCrossroads(selected_spline);

                            if (refCrossroads.Count != 0)
                            {
                                Vector3 checkLastPos = selected_spline.GetPoints().Last().position;

                                cross_new_spline = SplitSpline(selected_index, selected_spline);
                                cross_old_spline = selected_spline;

                                foreach (var refCros in refCrossroads)
                                {
                                    if (refCros.getPosition() == checkLastPos)
                                    {
                                        refCros.RemoveRoad(selected_spline);
                                        refCros.AddRoad(cross_new_spline);
                                    }
                                }

                                Crossroad crossroad = new Crossroad();
                                crossroad.AddRoad(cross_new_spline);
                                crossroad.AddRoad(cross_old_spline);
                                crossroad.AddRoad(current_spline);

                                crossroad.SetPosition(cross_new_spline.GetPoint(0).position);

                                crossroads.Add(crossroad);
                            }
                            else
                            {
                                cross_new_spline = SplitSpline(selected_index, selected_spline);
                                cross_old_spline = selected_spline;

                                Crossroad crossroad = new Crossroad();
                                crossroad.AddRoad(cross_new_spline);
                                crossroad.AddRoad(cross_old_spline);
                                crossroad.AddRoad(current_spline);
                                crossroad.SetPosition(cross_new_spline.GetPoint(0).position);

                                crossroads.Add(crossroad);
                            }

                            joinmode = JOINMODE.NONE;
                            selected_spline = null;
                            new_index++;
                        }
                        else if (joinmode == JOINMODE.TO4)
                        {
                            UnityEngine.Debug.LogWarning("Join 4-crossroad (BUILD)");

                            selected_crossroad.AddRoad(current_spline);

                            new_index++;
                            joinmode = JOINMODE.NONE;
                        }
                        else if (joinmode == JOINMODE.TO3_SPLIT)
                        {
                            var crossroad = new Crossroad();
                            crossroad.SetRoads(selectedSplines);
                            crossroad.AddRoad(current_spline);
                            crossroad.SetPosition(current_spline.GetPoint(0).position);
                            
                            crossroads.Add(crossroad);

                            joinmode = JOINMODE.NONE;
                            selectedSplines = null;
                            new_index++;
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

    List<SplineComputer> GetSplineComputers(Vector3 pos)
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

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s2));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s2);
                refCrossroad.AddRoad(s1);
            }

            Destroy(s2.gameObject);

            if (s1.GetPoint(0).position == s1.GetPoints().Last().position)
            {
                s1.Close();
            }

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

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s2));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s2);
                refCrossroad.AddRoad(s1);
            }

            Destroy(s2.gameObject);

            if (s1.GetPoint(0).position == s1.GetPoints().Last().position)
            {
                s1.Close();
            }

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

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s2));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s2);
                refCrossroad.AddRoad(s1);
            }

            Destroy(s2.gameObject);

            if (s1.GetPoint(0).position == s1.GetPoints().Last().position)
            {
                s1.Close();
            }

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

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s1));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s1);
                refCrossroad.AddRoad(s2);
            }

            Destroy(s1.gameObject);

            if (s2.GetPoint(0).position == s2.GetPoints().Last().position)
            {
                s2.Close();
            }

            return s2;
        }
        else
        {
            UnityEngine.Debug.LogWarning("NONE");
            return null;
        }
    }

    Vector3 GetSplinePosition(SplineComputer spline)
    {
        // return spline.GetPoint(spline.GetPoints().Length / 2).position;
        return (spline.GetPoints().First().position + spline.GetPoints().Last().position) / 2;
    }

    List<Crossroad> GetRefCrossroads(SplineComputer spline)
    {
        return crossroads.Where(cros => cros.getRoads().Contains(spline)).ToList();
    }

    void Start()
    {
        cm = GetComponentInChildren<Camera>();
    }

    // TODO - LOOP Spline Append Spliting Code (BUILD, TAIL APPEND, HEAD APPEND / contain CROSSROAD or not contain)

    void Update()
    {  
        RayTrace();

        snap_pos = new Vector3(SnapGrid(pos.x, snapsize), 0, SnapGrid(pos.z, snapsize));
        last_pos = snap_pos;
        debugobj.GetComponent<Transform>().position = snap_pos;

        for (var index = 0; index < crossroads.Count; index++)
        {
            Crossroad cros = crossroads[index];

            LogTextOnPos(index + "C ", cros.getPosition()); // DEBUG
            List<SplineComputer> roads = cros.getRoads();

            switch (roads.Count)
            {
                // TODO - Clean-Up Code
                case 2:
                    SplineComputer stRoad, loopRoad;
                    var dirList = new List<Vector3>();
                    var roadList = new List<SplineComputer>();

                    stRoad = roads.FirstOrDefault(spline => !spline.isClosed);
                    loopRoad = roads.FirstOrDefault(spline => spline.isClosed);

                    LogTextOnPos(index + "C - stRoad", GetSplinePosition(stRoad));
                    LogTextOnPos(index + "C - loopRoad", GetSplinePosition(loopRoad));

                    roadList.Add(stRoad);
                    roadList.Add(loopRoad);
                    roadList.Add(loopRoad);

                    if (stRoad.GetPoints().Last().position == cros.getPosition())
                    {
                        var last_index = stRoad.GetPoints().Length - 1;

                        dirList.Add(stRoad.GetPoint(last_index - 1).position - cros.getPosition());
                    }
                    else if (stRoad.GetPoints().First().position == cros.getPosition())
                    {
                        dirList.Add(stRoad.GetPoint(1).position - cros.getPosition());
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("ERROR!");
                    }

                    int loopJoinIndex = 0;
                    for (int i = 0; i < loopRoad.GetPoints().Length; i++)
                    {
                        if (loopRoad.GetPoint(i).position == cros.getPosition())
                        {
                            loopJoinIndex = i;
                            break;
                        }
                    }

                    var d1 = loopRoad.GetPoint(loopJoinIndex + 1).position - loopRoad.GetPoint(loopJoinIndex).position;

                    int loopLastIndex = loopJoinIndex - 1;
                    if (loopLastIndex < 0)
                    {
                        loopLastIndex = loopRoad.GetPoints().Length - 1 + loopLastIndex;
                    }
                    
                    var d2 = loopRoad.GetPoint(loopLastIndex).position - loopRoad.GetPoint(loopJoinIndex).position;

                    dirList.Add(d1);
                    dirList.Add(d2);

                    for (int i = 0; i < roadList.Count; i++)
                    {
                        bool isRight = false;
                        bool isLeft = false;

                        if (!roadList[i].isClosed)
                        {
                            if (roadList[i].GetPoints().Last().position == cros.getPosition())
                            {
                                foreach (Vector3 dir in dirList)
                                {
                                    if (isVectorVertical(dirList[i], dir))
                                    {
                                        if (isVectorGoClockwise(dirList[i], dir))
                                        {
                                            isLeft = true;
                                        }
                                        else
                                        {
                                            isRight = true;
                                        }
                                    }
                                }

                                double per = roadList[i].Project(cros.getPosition() + dirList[i] / divider).percent;

                                roadList[i].GetComponent<SplineMesh>().GetChannel(1).clipTo = per;

                                if (isLeft && !isRight)
                                {
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipTo = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipTo = per;
                                }
                                else if (isRight && !isLeft)
                                {
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipTo = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipTo = per;
                                }
                                else if (isLeft && isRight)
                                {
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipTo = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipTo = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipTo = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipTo = per;
                                }
                            }
                            else if (roadList[i].GetPoints().First().position == cros.getPosition())
                            {
                                foreach (Vector3 dir in dirList)
                                {
                                    if (isVectorVertical(dirList[i], dir))
                                    {
                                        if (isVectorGoClockwise(dirList[i], dir))
                                        {
                                            isRight = true;
                                        }
                                        else
                                        {
                                            isLeft = true;
                                        }
                                    }
                                }

                                double per = roadList[i].Project(cros.getPosition() + dirList[i] / divider).percent;

                                roadList[i].GetComponent<SplineMesh>().GetChannel(1).clipFrom = per;

                                if (isLeft && !isRight)
                                {
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipFrom = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipFrom = per;
                                }
                                else if (isRight && !isLeft)
                                {
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipFrom = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipFrom = per;
                                }
                                else if (isLeft && isRight)
                                {
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipFrom = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipFrom = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipFrom = per;
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipFrom = per;
                                }
                            }  
                        }
                        else
                        {
                            foreach (var dir in dirList)
                            {
                                if (isVectorVertical(dirList[i], dir))
                                {
                                    if (isVectorGoClockwise(dirList[i], dir))
                                    {
                                        switch (i)
                                        {
                                            case 1:
                                                isRight = true;
                                                break;
                                            case 2:
                                                isLeft = true;
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        switch (i)
                                        {
                                            case 1:
                                                isLeft = true;
                                                break;
                                            case 2:
                                                isRight = true;
                                                break;
                                        }
                                    }
                                }
                            }

                            double per = roadList[i].Project(cros.getPosition() + dirList[i] / divider).percent;

                            switch (i)
                            {
                                case 1:
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(1).clipFrom = per;

                                    if (isLeft && !isRight)
                                    {
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipFrom = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipFrom = per;
                                    }
                                    else if (isRight && !isLeft)
                                    {
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipFrom = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipFrom = per;
                                    }
                                    else if (isLeft && isRight)
                                    {
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipFrom = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipFrom = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipFrom = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipFrom = per;
                                    }
                                    break;
                                case 2:
                                    roadList[i].GetComponent<SplineMesh>().GetChannel(1).clipTo = per;

                                    if (isLeft && !isRight)
                                    {
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipTo = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipTo = per;
                                    }
                                    else if (isRight && !isLeft)
                                    {
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipTo = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipTo = per;
                                    }
                                    else if (isLeft && isRight)
                                    {
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(3).clipTo = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(5).clipTo = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(2).clipTo = per;
                                        roadList[i].GetComponent<SplineMesh>().GetChannel(4).clipTo = per;
                                    }
                                    break;
                            }
                        }
                    }

                    break;
                case 3:
                    var dirs = new List<Vector3>();
                    
                    for (int i = 0; i < roads.Count; i++)
                    {
                        LogTextOnPos(index + "C - " + i, GetSplinePosition(roads[i]));

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
                            UnityEngine.Debug.LogWarning("ERROR!");
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
                    break;
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

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentRoadLane = ROADLANE.RL1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentRoadLane = ROADLANE.RL2;
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
                    List<SplineComputer> splines = GetSplineComputers(snap_pos);
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
                    else if (splines.Count == 2)
                    {
                        // Not Crossroad, But Splines are splitted.
                        UnityEngine.Debug.LogWarning("SPLIT TO3");
                        
                        selectedSplines = splines;

                        current_mode = MODE.APPEND;
                        joinmode = JOINMODE.TO3_SPLIT;
                        
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
